// ReSharper disable All
// We are using .NetStandard all optimizations are disabled to keep compatibility with projects using older .Net versions

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MyCodingZone.Analyzer.MinimalApi
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MCZ_MAPI_0004_ProducesNoContent204Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MCZ_MAPI_0004";

        private static readonly DiagnosticDescriptor Rule = new(id: DiagnosticId,
                                                                title: "Minimal API endpoint must declare 204 No Content",
                                                                messageFormat: "Add '.Produces(StatusCodes.Status204NoContent)' to the Minimal API endpoint",
                                                                description: "If the handler does not return a response DTO, the endpoint should explicitly declare 204 to reflect no-body semantics.",
                                                                category: "MinimalApi.Design",
                                                                defaultSeverity: DiagnosticSeverity.Info,
                                                                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            Throw.IfNull(context, nameof(context));

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var chain = MinimalApiChainWalker.TryCreate(invocation, context.SemanticModel, context.CancellationToken);

            if (chain is null)
            {
                return;
            }

            // Analyze each endpoint chain only once (on the root Map* invocation) to avoid duplicate/missed diagnostics.
            if (!ReferenceEquals(chain.Value.RootMapInvocation, invocation))
            {
                return;
            }

            var returnType = MinimalApiHandlerInference.TryGetHandlerReturnType(chain.Value, context.SemanticModel, context.CancellationToken);

            // File endpoints commonly return `IResult` via `Results.File(...)`.
            // They are not "no content" endpoints and must not be forced to declare 204.
            // If the endpoint explicitly declares a file result (e.g. `.Produces<FileStreamResult>()`), skip this rule.
            if (chain.Value.Invocations.Any(i => IsProducesFileResultSyntaxOnly(i, context.SemanticModel, context.CancellationToken)))
            {
                return;
            }

            // If we can't infer the handler return type (common in unit tests without ASP.NET refs),
            // fall back to syntax-only detection for common no-content patterns.
            if (returnType is null)
            {
                if (chain.Value.Invocations.Any(i => IsProduces204(i, context.SemanticModel, context.CancellationToken)) ||
                    chain.Value.Invocations.Any(IsProduces204SyntaxOnly))
                {
                    return;
                }

                if (IsTaskOrTaskOfIResultHandlerSyntaxOnly(chain.Value.RootMapInvocation))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, chain.Value.RootMapInvocation.GetLocation()));
                }

                return;
            }

            // Even if an IResult handler returns `Results.NoContent()`, we still require the explicit
            // `.Produces(StatusCodes.Status204NoContent)` for OpenAPI accuracy and consistent conventions.

            if (!MinimalApiSemanticRules.IsNoContentReturnType(returnType) && !IsIResultReturnType(returnType))
            {
                // If Task<T> exists, this isn't the no-content path.
                return;
            }

            if (MinimalApiHandlerInference.HasConcreteResponseDto(returnType))
            {
                return;
            }

            if (chain.Value.Invocations.Any(i => IsProduces204(i, context.SemanticModel, context.CancellationToken)))
            {
                return;
            }

            if (chain.Value.Invocations.Any(IsProduces204SyntaxOnly))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, chain.Value.RootMapInvocation.GetLocation()));
        }

        private static bool IsProducesFileResultSyntaxOnly(InvocationExpressionSyntax invocation,
                                                           SemanticModel semanticModel,
                                                           CancellationToken ct)
        {
            // `.Produces<FileStreamResult>()`, `.Produces<Stream>(StatusCodes.Status200OK, "image/jpeg")`, `.Produces<byte[]>(...)`
            if (invocation.Expression is not MemberAccessExpressionSyntax { Name: GenericNameSyntax g })
            {
                return false;
            }

            if (g.Identifier.ValueText != "Produces" || g.TypeArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            var typeArg = g.TypeArgumentList.Arguments[0];

            var typeName = typeArg switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                QualifiedNameSyntax q => q.Right.Identifier.ValueText,
                ArrayTypeSyntax { ElementType: PredefinedTypeSyntax { Keyword.ValueText: "byte" }, RankSpecifiers.Count: 1 } => "byte[]",
                _ => null
            };

            if (typeName is not ("FileStreamResult" or "Stream" or "byte[]"))
            {
                return false;
            }

            // If we can resolve the symbol, ensure it's actually the expected framework type.
            var resolved = semanticModel.GetTypeInfo(typeArg, ct).Type;

            if (resolved is null || resolved.TypeKind == TypeKind.Error)
            {
                return true;
            }

            return typeName switch
            {
                "FileStreamResult" => resolved.Name == "FileStreamResult" &&
                                      resolved.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Mvc",
                "Stream" => resolved.Name == "Stream" &&
                            resolved.ContainingNamespace?.ToDisplayString() == "System.IO",
                "byte[]" => resolved is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte, Rank: 1 },
                _ => false
            };
        }

        private static bool IsIResultReturnType(ITypeSymbol? returnType)
        {
            var dto = MinimalApiHandlerInference.TryGetResponseDtoType(returnType);

            return dto is not null &&
                   dto.Name == "IResult" &&
                   dto.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Http";
        }

        private static bool IsTaskOrTaskOfIResultHandlerSyntaxOnly(InvocationExpressionSyntax mapInvocation)
        {
            if (mapInvocation.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var handlerExpr = mapInvocation.ArgumentList.Arguments.Last().Expression;

            if (handlerExpr is not IdentifierNameSyntax handlerId)
            {
                return false;
            }

            var containingMethod = mapInvocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            if (containingMethod?.Body is null)
            {
                return false;
            }

            var localFunc = containingMethod.Body.DescendantNodes()
                                            .OfType<LocalFunctionStatementSyntax>()
                                            .FirstOrDefault(l => string.Equals(l.Identifier.ValueText, handlerId.Identifier.ValueText, StringComparison.Ordinal));

            if (localFunc?.ReturnType is null)
            {
                return false;
            }

            // Matches `Task` or `Task<IResult>`
            return localFunc.ReturnType switch
            {
                IdentifierNameSyntax { Identifier.ValueText: "Task" } => true,
                GenericNameSyntax { Identifier.ValueText: "Task", TypeArgumentList.Arguments.Count: 1 } g
                    when IsIResultTypeSyntax(g.TypeArgumentList.Arguments[0]) => true,
                QualifiedNameSyntax { Right: IdentifierNameSyntax { Identifier.ValueText: "Task" } } => true,
                QualifiedNameSyntax { Right: GenericNameSyntax { Identifier.ValueText: "Task", TypeArgumentList.Arguments.Count: 1 } g }
                    when IsIResultTypeSyntax(g.TypeArgumentList.Arguments[0]) => true,
                _ => false
            };
        }

        private static bool IsIResultTypeSyntax(TypeSyntax type)
        {
            return type switch
            {
                IdentifierNameSyntax { Identifier.ValueText: "IResult" } => true,
                QualifiedNameSyntax q when q.Right.Identifier.ValueText == "IResult" => true,
                _ => false
            };
        }

        private static bool IsProduces204(InvocationExpressionSyntax invocation,
                                          SemanticModel semanticModel,
                                          CancellationToken ct)
        {
            if (semanticModel.GetSymbolInfo(invocation, ct).Symbol is not IMethodSymbol method)
            {
                return false;
            }

            if (!string.Equals(method.Name, "Produces", StringComparison.Ordinal))
            {
                return false;
            }

            if (method.IsGenericMethod)
            {
                return false;
            }

            if (invocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            var arg = invocation.ArgumentList.Arguments[0].Expression;

            return arg is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Status204NoContent" };
        }

        private static bool IsProduces204SyntaxOnly(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Produces" })
            {
                return false;
            }

            if (invocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            // `.Produces(StatusCodes.Status204NoContent)`
            return invocation.ArgumentList.Arguments[0].Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Status204NoContent" };
        }
    }
}