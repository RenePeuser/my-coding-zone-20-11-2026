// ReSharper disable All
// We are using .NET Standard; keep logic explicit and deterministic.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MyCodingZone.Analyzer.MinimalApi
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MCZ_MAPI_0001";

        private static readonly DiagnosticDescriptor Rule =
            new(id: DiagnosticId,
                title: "Minimal API endpoint must declare request body media type",
                messageFormat: "Add '.Accepts<{0}>({1})' to the Minimal API endpoint",
                description:
                "POST/PUT/PATCH Minimal API endpoints with a request body must explicitly declare the request body and media type.",
                category: "MinimalApi.Design",
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            Throw.IfNull(context, nameof(context));

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeInvocation,
                                             SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var chain = MinimalApiChainWalker.TryCreate(invocation,
                                                        context.SemanticModel,
                                                        context.CancellationToken);

            if (chain is null)
            {
                return;
            }

            // Analyze each endpoint only once (on the root Map* call)
            if (!ReferenceEquals(chain.Value.RootMapInvocation, invocation))
            {
                return;
            }

            var rootMapMethod = chain.Value.RootMapMethodName;

            if (!MinimalApiSemanticRules.RequiresAccepts(rootMapMethod))
            {
                return;
            }

            var hasAccepts = chain.Value.Invocations.Any(IsAcceptsSyntax);

            // IMPORTANT:
            // Accepts<T>() is only required if a request body actually exists.
            if (!hasAccepts &&
                HasRequestBody(chain.Value.RootMapInvocation,
                               context.SemanticModel,
                               context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule,
                                                           chain.Value.RootMapInvocation.GetLocation(),
                                                           "TRequest",
                                                           "MediaTypeNames.Application.Json"));
            }
        }

        // ------------------------------------------------------------
        // Accepts detection (syntax-only, deterministic, fast)
        // ------------------------------------------------------------

        private static bool IsAcceptsSyntax(InvocationExpressionSyntax invocation)
        {
            return invocation.Expression switch
            {
                MemberAccessExpressionSyntax { Name: GenericNameSyntax g }
                    when g.Identifier.ValueText == "Accepts"
                         && g.TypeArgumentList.Arguments.Count == 1
                    => true,

                MemberAccessExpressionSyntax { Name: IdentifierNameSyntax i }
                    when i.Identifier.ValueText == "Accepts"
                    => true,

                MemberBindingExpressionSyntax { Name: GenericNameSyntax g }
                    when g.Identifier.ValueText == "Accepts"
                         && g.TypeArgumentList.Arguments.Count == 1
                    => true,

                MemberBindingExpressionSyntax { Name: IdentifierNameSyntax i }
                    when i.Identifier.ValueText == "Accepts"
                    => true,

                _ => false
            };
        }

        // ------------------------------------------------------------
        // Request body inference (policy-level, no guessing)
        // ------------------------------------------------------------

        private static bool HasRequestBody(InvocationExpressionSyntax mapInvocation,
                                           SemanticModel semanticModel,
                                           CancellationToken ct)
        {
            if (mapInvocation.ArgumentList.Arguments.Count < 2)
            {
                return false;
            }

            var handlerExpression = mapInvocation.ArgumentList.Arguments[1].Expression;

            if (TryGetHandlerParameters(mapInvocation, handlerExpression, semanticModel, out var parameters, ct))
            {
                // Exactly one valid body candidate => request body exists
                var bodyCandidates = parameters.Where(IsPotentialRequestDto).ToList();
                return bodyCandidates.Count == 1;
            }

            // If we can't bind the handler symbol (or delegate signature) we can't reliably infer a request body.
            // To avoid false positives (e.g. POST endpoints without a request body), assume 'no body'.
            return false;
        }

        private static bool TryGetHandlerParameters(InvocationExpressionSyntax mapInvocation,
                                                    ExpressionSyntax handlerExpression,
                                                    SemanticModel semanticModel,
                                                    out ImmutableArray<IParameterSymbol> parameters,
                                                    CancellationToken ct)
        {
            // 1) Preferred: bind method symbol directly (works for lambdas/local functions when Roslyn can resolve)
            var symbolInfo = semanticModel.GetSymbolInfo(handlerExpression, ct);

            if (symbolInfo.Symbol is IMethodSymbol method)
            {
                parameters = method.Parameters;
                return true;
            }

            // CandidateSymbols happens for overload sets.
            var candidateMethod = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

            if (candidateMethod is not null)
            {
                parameters = candidateMethod.Parameters;
                return true;
            }

            // 2) Fallback: resolve delegate signature from the Map* method's handler parameter type.
            // For method groups (incl. static local functions), the handler expression's ConvertedType can be null,
            // but the MapPost/MapPut/... overload has a concrete delegate type for the handler parameter.
            var mapSymbol = semanticModel.GetSymbolInfo(mapInvocation, ct).Symbol as IMethodSymbol;

            if (mapSymbol is not null)
            {
                var handlerParameter = mapSymbol.Parameters.Length >= 3 ? mapSymbol.Parameters[2] : null;
                var delegateType = handlerParameter?.Type as INamedTypeSymbol;
                var invoke = delegateType?.DelegateInvokeMethod;

                if (invoke is not null)
                {
                    parameters = invoke.Parameters;
                    return true;
                }
            }

            // 3) Fallback: use the delegate type of the handler expression and read its Invoke(...) signature.
            var typeInfo = semanticModel.GetTypeInfo(handlerExpression, ct);
            var exprDelegateType = typeInfo.ConvertedType as INamedTypeSymbol;
            var exprInvoke = exprDelegateType?.DelegateInvokeMethod;

            if (exprInvoke is not null)
            {
                parameters = exprInvoke.Parameters;
                return true;
            }

            parameters = default;
            return false;
        }

        private static bool IsPotentialRequestDto(IParameterSymbol parameter)
        {
            var type = parameter.Type;

            // Never interfaces (DI contracts)
            if (type.TypeKind == TypeKind.Interface)
            {
                return false;
            }

            // Exclude primitives / special types
            if (type.SpecialType != SpecialType.None)
            {
                return false;
            }

            // Exclude known framework / infrastructure types
            if (type.Name is
                "HttpContext" or
                "HttpRequest" or
                "HttpResponse" or
                "ClaimsPrincipal" or
                "CancellationToken" or
                "IFormFile" or
                "IFormFileCollection")
            {
                return false;
            }

            // Exclude common DI namespaces
            var ns = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;

            if (ns.StartsWith("Microsoft.Extensions") ||
                ns.StartsWith("System"))
            {
                return false;
            }

            return type.TypeKind is TypeKind.Class or TypeKind.Struct;
        }
    }
}