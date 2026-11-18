using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzerCodeFix))]
    [Shared]
    public sealed class MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzerCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzer.DiagnosticId);

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

            context.RegisterCodeFix(CodeAction.Create("Remove .Accepts<...>()",
                                                      ct => RemoveAcceptsAsync(context.Document, invocation, ct),
                                                      nameof(MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzerCodeFix)),
                                    diagnostic);
        }

        private static async Task<Document> RemoveAcceptsAsync(Document document,
                                                               InvocationExpressionSyntax acceptsInvocation,
                                                               CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);

            if (root is null)
            {
                return document;
            }

            if (acceptsInvocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
                memberAccess.Expression is not InvocationExpressionSyntax previousInvocation)
            {
                return document;
            }

            var newRoot = root.ReplaceNode(acceptsInvocation,
                                           previousInvocation.WithTriviaFrom(acceptsInvocation));

            return document.WithSyntaxRoot(newRoot);
        }
    }
}