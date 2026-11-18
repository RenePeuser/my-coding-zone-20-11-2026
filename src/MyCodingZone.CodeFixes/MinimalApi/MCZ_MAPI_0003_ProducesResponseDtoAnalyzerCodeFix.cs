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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MCZ_MAPI_0003_ProducesResponseDtoAnalyzerCodeFix))]
    [Shared]
    public sealed class MCZ_MAPI_0003_ProducesResponseDtoAnalyzerCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MCZ_MAPI_0003_ProducesResponseDtoAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

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

            context.RegisterCodeFix(CodeAction.Create("Add .Produces<TResponse>()",
                                                      ct => AddProducesAsync(context.Document, invocation, ct),
                                                      nameof(MCZ_MAPI_0003_ProducesResponseDtoAnalyzerCodeFix)),
                                    diagnostic);
        }

        private static async Task<Document> AddProducesAsync(Document document,
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

            var returnType = MinimalApiHandlerInference.TryGetHandlerReturnType(chain.Value, semanticModel, ct);
            var responseDto = MinimalApiHandlerInference.TryGetResponseDtoType(returnType);

            if (responseDto is null)
            {
                return document;
            }

            if (chain.Value.Invocations.Any(i => ModelExtensions.GetSymbolInfo(semanticModel, i, ct).Symbol is IMethodSymbol m && m.Name == "Produces" && m.IsGenericMethod && i.ArgumentList.Arguments.Count == 0))
            {
                return document;
            }

            // Append at end of chain (before final ';' is fine)
            // We rewrite the outermost invocation: <outer>.Produces<T>()
            var outermost = chain.Value.Invocations[0];

            var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                    outermost,
                                                                    SyntaxFactory.GenericName("Produces")
                                                                                 .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.ParseTypeName(responseDto.ToDisplayString())))))
                                            .WithOperatorToken(SyntaxFactory.Token(SyntaxKind.DotToken));

            var producesInvocation = SyntaxFactory.InvocationExpression(memberAccess, SyntaxFactory.ArgumentList());
            var newRoot = root.ReplaceNode(outermost, producesInvocation.WithTriviaFrom(outermost));

            return document.WithSyntaxRoot(newRoot);
        }
    }
}