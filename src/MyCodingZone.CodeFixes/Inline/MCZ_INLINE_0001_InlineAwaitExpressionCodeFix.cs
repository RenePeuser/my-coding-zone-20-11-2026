// ReSharper disable All
// We are using .NetStandard all optimizations are disabled to keep compatibility with projects using older .Net versions

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MCZ_INLINE_0001_InlineAwaitExpressionCodeFix))]
    [Shared]
    public sealed class MCZ_INLINE_0001_InlineAwaitExpressionCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var span = diagnostic.Location.SourceSpan;

            var awaitExpr = root?.FindToken(span.Start).Parent?.FirstAncestorOrSelf<AwaitExpressionSyntax>();

            if (awaitExpr is null)
            {
                return;
            }

            context.RegisterCodeFix(CodeAction.Create("Extract awaited call into variable",
                                                      ct => ApplyFixAsync(context.Document, awaitExpr, ct),
                                                      nameof(MCZ_INLINE_0001_InlineAwaitExpressionCodeFix)), diagnostic);
        }

        private async Task<Document> ApplyFixAsync(Document document,
                                                   AwaitExpressionSyntax awaitExpr,
                                                   CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            var semantic = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);

            if (root is null || semantic is null)
            {
                return document;
            }

            var type = semantic.GetTypeInfo(awaitExpr, ct).ConvertedType;
            var variableName = "result";

            // Create: var result = await FooAsync(...);
            var newDeclaration =
                SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(type?.ToDisplayString() ?? "var"))
                                                                     .AddVariables(SyntaxFactory.VariableDeclarator(variableName)
                                                                                                .WithInitializer(SyntaxFactory.EqualsValueClause(awaitExpr))));

            var statement = awaitExpr.FirstAncestorOrSelf<StatementSyntax>();

            if (statement is null)
            {
                return document;
            }

            // Replace inline await with variable name
            var newRoot = root.TrackNodes(awaitExpr, statement);
            var currentAwait = newRoot.GetCurrentNode(awaitExpr);

            if (currentAwait is null)
            {
                return document;
            }

            var identifierNameSyntax = SyntaxFactory.IdentifierName(variableName);

            newRoot = newRoot.ReplaceNode(currentAwait, identifierNameSyntax);

            var currentStatement = newRoot.GetCurrentNode(statement);

            if (currentStatement is null)
            {
                return document;
            }

            newRoot = newRoot.InsertNodesBefore(currentStatement, new[]
            {
                newDeclaration
            });

            return document.WithSyntaxRoot(newRoot);
        }
    }
}