using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyCodingZone.Analyzer;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MCZ_MAPI_0001_AcceptsMediaTypeAnalyzerCodeFix))]
    [Shared]
    public sealed class MCZ_MAPI_0001_AcceptsMediaTypeAnalyzerCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document
                                    .GetSyntaxRootAsync(context.CancellationToken)
                                    .ConfigureAwait(false);

            if (root is null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.First();
            var node = root.FindNode(diagnostic.Location.SourceSpan);

            var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>() ?? node as InvocationExpressionSyntax;

            if (invocation is null)
            {
                return;
            }

            context.RegisterCodeFix(CodeAction.Create("Add .Accepts<TRequest>(MediaTypeNames.Application.Json)",
                                                      ct => AddAcceptsAsync(context.Document, invocation, ct),
                                                      nameof(MCZ_MAPI_0001_AcceptsMediaTypeAnalyzerCodeFix)),
                                    diagnostic);
        }

        private static async Task<Document> AddAcceptsAsync(Document document,
                                                            InvocationExpressionSyntax anyInvocation,
                                                            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);

            if (root is null)
            {
                return document;
            }

            var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);

            if (semanticModel is null)
            {
                return document;
            }

            var chain = MinimalApiChainWalker.TryCreate(anyInvocation, semanticModel, ct);

            if (chain is null)
            {
                return document;
            }

            // Already has Accepts<T>()
            if (chain.Value.Invocations.Any(i =>
                                                ModelExtensions.GetSymbolInfo(semanticModel, i, ct).Symbol is IMethodSymbol m &&
                                                m.Name == "Accepts" &&
                                                m.IsGenericMethod))
            {
                return document;
            }

            var mapInvocation = chain.Value.RootMapInvocation;

            var requestType = TryResolveRequestType(mapInvocation, semanticModel, ct);

            if (requestType is null)
            {
                // Analyzer should already handle ambiguity;
                // code fix must not guess.
                return document;
            }

            var acceptsInvocation =
                SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                                        mapInvocation,
                                                                                        SyntaxFactory.GenericName("Accepts")
                                                                                                     .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.IdentifierName(requestType))))),
                                                   SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.ParseExpression("MediaTypeNames.Application.Json")))));

            var newRoot = root.ReplaceNode(mapInvocation,
                                           acceptsInvocation.WithTriviaFrom(mapInvocation));

            return document.WithSyntaxRoot(newRoot);
        }

        private static string? TryResolveRequestType(InvocationExpressionSyntax mapInvocation,
                                                     SemanticModel semanticModel,
                                                     CancellationToken ct)
        {
            if (mapInvocation.ArgumentList.Arguments.Count < 2)
            {
                return null;
            }

            var handlerArg = mapInvocation.ArgumentList.Arguments[1].Expression;

            // Lambda handler: we only have syntax for parameters, so infer by first typed parameter.
            if (handlerArg is LambdaExpressionSyntax lambda)
            {
                var typeSyntax = GetFirstLambdaParameterTypeSyntax(lambda);

                if (typeSyntax is null)
                {
                    return null;
                }

                var typeInfo = ModelExtensions.GetTypeInfo(semanticModel, typeSyntax, ct).Type;

                return GetFriendlyTypeName(typeInfo);
            }

            // Method group / local function
            var symbol = ModelExtensions.GetSymbolInfo(semanticModel, handlerArg, ct).Symbol;

            if (symbol is IMethodSymbol method)
            {
                return ResolveRequestType(method.Parameters);
            }

            // Fallback for test compilations / partial binding: infer from local function declaration.
            if (handlerArg is IdentifierNameSyntax id)
            {
                var containingMethod = mapInvocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();

                var localFunction = containingMethod?
                                    .DescendantNodes()
                                    .OfType<LocalFunctionStatementSyntax>()
                                    .FirstOrDefault(l => string.Equals(l.Identifier.ValueText, id.Identifier.ValueText, StringComparison.Ordinal));

                if (localFunction is not null)
                {
                    var firstParamType = localFunction.ParameterList.Parameters.FirstOrDefault()?.Type;

                    if (firstParamType is not null)
                    {
                        var type = ModelExtensions.GetTypeInfo(semanticModel, firstParamType, ct).Type;

                        return GetFriendlyTypeName(type);
                    }
                }
            }

            return null;
        }

        private static string? ResolveRequestType(ImmutableArray<IParameterSymbol> parameters)
        {
            // Prefer [FromBody]
            var fromBody = parameters.Where(p => HasAttribute(p, "FromBodyAttribute"))
                                     .ToList();

            if (fromBody.Count == 1)
            {
                return GetFriendlyTypeName(fromBody[0].Type);
            }

            // Fallback: single valid body candidate
            var bodyCandidates = parameters
                                 .Where(IsPotentialRequestDto)
                                 .ToList();

            return bodyCandidates.Count == 1
                       ? GetFriendlyTypeName(bodyCandidates[0].Type)
                       : null;
        }

        private static TypeSyntax? GetFirstLambdaParameterTypeSyntax(LambdaExpressionSyntax lambda)
        {
            return lambda switch
            {
                SimpleLambdaExpressionSyntax s => s.Parameter.Type,
                ParenthesizedLambdaExpressionSyntax p => p.ParameterList.Parameters.FirstOrDefault(param => param.Type is not null)?.Type,
                _ => null
            };
        }

        private static bool IsPotentialRequestDto(IParameterSymbol parameter)
        {
            var type = parameter.Type;

            // Explicit DI
            if (HasAttribute(parameter, "FromServicesAttribute"))
            {
                return false;
            }

            // Never interfaces (DI contracts)
            if (type.TypeKind == TypeKind.Interface)
            {
                return false;
            }

            // Exclude primitives / framework
            if (type.SpecialType != SpecialType.None)
            {
                return false;
            }

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

            var ns = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;

            if (ns.StartsWith("Microsoft.Extensions") ||
                ns.StartsWith("System"))
            {
                return false;
            }

            return type.TypeKind is TypeKind.Class or TypeKind.Struct;
        }

        private static bool HasAttribute(IParameterSymbol parameter,
                                         string attributeName)
        {
            return parameter.GetAttributes()
                            .Any(a => a.AttributeClass?.Name == attributeName);
        }

        private static string? GetFriendlyTypeName(ITypeSymbol? typeSymbol)
        {
            return typeSymbol?.Name;
        }
    }
}