using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes
{
    public abstract class MinimalApiSimpleConventionCodeFixBase : CodeFixProvider
    {
        protected abstract string Title { get; }

        protected abstract string MethodName { get; }

        protected abstract ArgumentListSyntax CreateArguments();

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

            context.RegisterCodeFix(CodeAction.Create(Title, ct => ApplyAsync(context.Document, invocation, ct), GetType().Name),
                                    diagnostic);
        }

        private async Task<Document> ApplyAsync(Document document,
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

            if (chain.Value.Invocations.Any(i => ModelExtensions.GetSymbolInfo(semanticModel, i, ct).Symbol is IMethodSymbol m && m.Name == MethodName))
            {
                return document;
            }

            var outermost = chain.Value.Invocations[0];

            var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                    outermost,
                                                                    SyntaxFactory.IdentifierName(MethodName))
                                            .WithOperatorToken(SyntaxFactory.Token(SyntaxKind.DotToken));

            var inserted = SyntaxFactory.InvocationExpression(memberAccess, CreateArguments());
            var newRoot = root.ReplaceNode(outermost, inserted.WithTriviaFrom(outermost));

            return document.WithSyntaxRoot(newRoot);
        }
    }
}