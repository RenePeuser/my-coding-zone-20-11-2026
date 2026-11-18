using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MCZ_INLINE_0003_InlineReturnCollectionExpressionCodeFix))]
    [Shared]
    public sealed class MCZ_INLINE_0003_InlineReturnCollectionExpressionCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MCZ_INLINE_0003_InlineReturnCollectionExpressionAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                    .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var span = diagnostic.Location.SourceSpan;

            context.RegisterCodeFix(CodeAction.Create("Extract returned collection into variable",
                                                      ct => ApplyFixAsync(context.Document, span, ct),
                                                      nameof(MCZ_INLINE_0003_InlineReturnCollectionExpressionCodeFix)),
                                    diagnostic);
        }

        private async Task<Document> ApplyFixAsync(Document document,
                                                   TextSpan span,
                                                   CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);

            if (root is null)
            {
                return document;
            }

            // Find the return statement from the diagnostic span.
            // The analyzer reports the diagnostic on the CollectionExpressionSyntax location.
            var returnStmt = root.FindToken(span.Start)
                                 .Parent?
                                 .FirstAncestorOrSelf<ReturnStatementSyntax>();

            if (returnStmt?.Expression is not CollectionExpressionSyntax collectionExpr)
            {
                return document;
            }

            var variableName = "result";

            // var result = [ ... ];
            var equalsValueClauseSyntax = SyntaxFactory.EqualsValueClause(collectionExpr);

            var variableDeclaratorSyntax = SyntaxFactory.VariableDeclarator(variableName)
                                                        .WithInitializer(equalsValueClauseSyntax);

            var identifierNameSyntax = SyntaxFactory.IdentifierName("var");

            var variableDeclarationSyntax = SyntaxFactory.VariableDeclaration(identifierNameSyntax)
                                                         .AddVariables(variableDeclaratorSyntax);

            var declaration = SyntaxFactory.LocalDeclarationStatement(variableDeclarationSyntax);

            // return result;
            var newReturn = returnStmt.WithExpression(SyntaxFactory.IdentifierName(variableName));

            var trackedRoot = root.TrackNodes(returnStmt);

            if (trackedRoot.GetCurrentNode(returnStmt) is not ReturnStatementSyntax currentReturnStmt)
            {
                return document;
            }

            // Insert declaration before the return, then replace the (current) return statement.
            var newRoot = trackedRoot.InsertNodesBefore(currentReturnStmt, new[]
            {
                declaration
            });

            if (newRoot.GetCurrentNode(returnStmt) is not ReturnStatementSyntax insertedReturnStmt)
            {
                return document;
            }

            newRoot = newRoot.ReplaceNode(insertedReturnStmt, newReturn);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}