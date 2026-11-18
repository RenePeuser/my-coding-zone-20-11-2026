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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MCZ_MAPI_0004_ProducesNoContent204AnalyzerCodeFix))]
    [Shared]
    public sealed class MCZ_MAPI_0004_ProducesNoContent204AnalyzerCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MCZ_MAPI_0004_ProducesNoContent204Analyzer.DiagnosticId);

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

            context.RegisterCodeFix(CodeAction.Create("Add .Produces(StatusCodes.Status204NoContent)",
                                                      ct => AddProduces204Async(context.Document, invocation, ct),
                                                      nameof(MCZ_MAPI_0004_ProducesNoContent204AnalyzerCodeFix)),
                                    diagnostic);
        }

        private static async Task<Document> AddProduces204Async(Document document,
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

            // Idempotent: never append a second `.Produces(StatusCodes.Status204NoContent)`
            if (chain.Value.Invocations.Any(i => ModelExtensions.GetSymbolInfo(semanticModel, i, ct).Symbol is IMethodSymbol { Name: "Produces", IsGenericMethod: false } &&
                                                 i.ArgumentList.Arguments.Count == 1 &&
                                                 i.ArgumentList.Arguments[0].Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Status204NoContent" }))
            {
                return document;
            }

            var outermost = chain.Value.Invocations[0];

            var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                    outermost,
                                                                    SyntaxFactory.IdentifierName("Produces"))
                                            .WithOperatorToken(SyntaxFactory.Token(SyntaxKind.DotToken));

            var producesInvocation = SyntaxFactory.InvocationExpression(memberAccess,
                                                                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.ParseExpression("StatusCodes.Status204NoContent")))));

            var newRoot = root.ReplaceNode(outermost, producesInvocation.WithTriviaFrom(outermost));

            return document.WithSyntaxRoot(newRoot);
        }
    }
}