using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MCZ_INLINE_0004_InlineReturnObjectCreationCodeFix))]
    [Shared]
    public sealed class MCZ_INLINE_0004_InlineReturnObjectCreationCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MCZ_INLINE_0004_InlineReturnObjectCreationAnalyzer.DiagnosticId);

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

            if (root is null)
            {
                return;
            }

            // Prefer resolving from the return statement to be resilient against trivia/formatting
            // and diagnostics reported inside the object creation.
            var node = root.FindNode(span, getInnermostNodeForTie: true);
            var returnStatement = node.FirstAncestorOrSelf<ReturnStatementSyntax>();
            var creation = returnStatement?.Expression switch
            {
                ObjectCreationExpressionSyntax occ => occ,
                // `return new Foo { ... };` -> expression is ObjectCreationExpressionSyntax as well,
                // but keep a fallback for unexpected syntactic shapes.
                _ => returnStatement?.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault()
            };

            if (creation is null)
            {
                return;
            }

            context.RegisterCodeFix(CodeAction.Create("Extract return object into variable",
                                                      ct => ApplyFixAsync(context.Document, creation, ct),
                                                      nameof(MCZ_INLINE_0004_InlineReturnObjectCreationCodeFix) + ".Extract"), diagnostic);
        }

        private async Task<Document> ApplyFixAsync(Document document,
                                                   ObjectCreationExpressionSyntax creation,
                                                   CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);

            var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);

            if (root is null)
            {
                return document;
            }

            if (semanticModel is null)
            {
                return document;
            }

            var returnStatement = creation.FirstAncestorOrSelf<ReturnStatementSyntax>();

            if (returnStatement is null)
            {
                return document;
            }

            var createdType = semanticModel.GetTypeInfo(creation, ct).Type;
            var createdTypeName = createdType?.Name;

            // Fallback: best-effort from syntax (may include qualification/generics)
            createdTypeName ??= creation.Type switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                GenericNameSyntax gen => gen.Identifier.ValueText,
                QualifiedNameSyntax q => q.Right.Identifier.ValueText,
                AliasQualifiedNameSyntax a => a.Name.Identifier.ValueText,
                _ => creation.Type.ToString()
            };

            var variableName = createdTypeName.Length == 0
                                   ? "value"
                                   : char.ToLower(createdTypeName[0], CultureInfo.InvariantCulture) + createdTypeName.Substring(1);

            var variableDecl = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                                                                                    .AddVariables(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variableName))
                                                                                                               .WithInitializer(SyntaxFactory.EqualsValueClause(creation))))
                                            .WithLeadingTrivia(returnStatement.GetLeadingTrivia());

            variableDecl = variableDecl.WithAdditionalAnnotations(Formatter.Annotation);

            // Create: return <varName>;
            var newReturn = returnStatement.WithExpression(SyntaxFactory.IdentifierName(variableName));
            newReturn = newReturn.WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(returnStatement, newReturn);

            ReturnStatementSyntax? updatedReturn = null;

            // After replacement the old span might be out of range (e.g., formatting changes or different tree).
            if (newRoot.FullSpan.Contains(returnStatement.Span))
            {
                updatedReturn = newRoot.FindNode(returnStatement.Span, getInnermostNodeForTie: true)
                                       .FirstAncestorOrSelf<ReturnStatementSyntax>();
            }

            updatedReturn ??= newRoot.DescendantNodes().OfType<ReturnStatementSyntax>()
                                     .FirstOrDefault(r => r.SpanStart == returnStatement.SpanStart);

            if (updatedReturn is null)
            {
                // If we cannot locate the replaced node, do not silently no-op.
                var fallbackRoot = root.InsertNodesBefore(returnStatement, new[]
                {
                    variableDecl
                });

                fallbackRoot = fallbackRoot.ReplaceNode(returnStatement, newReturn);

                return document.WithSyntaxRoot(fallbackRoot);
            }

            newRoot = newRoot.InsertNodesBefore(updatedReturn, new[]
            {
                variableDecl
            });

            return document.WithSyntaxRoot(newRoot);
        }
    }
}