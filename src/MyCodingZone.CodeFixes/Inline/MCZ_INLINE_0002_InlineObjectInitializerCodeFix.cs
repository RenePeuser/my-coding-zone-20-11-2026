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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MCZ_INLINE_0002_InlineObjectInitializerCodeFix))]
    [Shared]
    public sealed class MCZ_INLINE_0002_InlineObjectInitializerCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MCZ_INLINE_0002_InlineObjectInitializerAnalyzer.DiagnosticId);

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
            var span = diagnostic.Location.SourceSpan;

            // Prefer resolving from the span node to be resilient to analyzer span choices/trivia.
            var node = root.FindNode(span, getInnermostNodeForTie: true);
            var creation = node.FirstAncestorOrSelf<ObjectCreationExpressionSyntax>()
                           ?? node.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();

            if (creation is null)
            {
                return;
            }

            var codeAction = CodeAction.Create(title: "Extract into variable",
                                               createChangedDocument: ct => ApplyFixAsync(context.Document, creation, ct),
                                               equivalenceKey: "ExtractIntoVariable");

            context.RegisterCodeFix(codeAction, diagnostic);
        }

        private async Task<Document> ApplyFixAsync(Document document,
                                                   ObjectCreationExpressionSyntax creation,
                                                   CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);

            if (root is null)
            {
                return document;
            }

            var semantic = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
            var typeSymbol = semantic?.GetTypeInfo(creation, ct).ConvertedType;

            if (typeSymbol is null ||
                string.IsNullOrEmpty(typeSymbol.Name))
            {
                return document;
            }

            var variableName = char.ToLower(typeSymbol.Name[0]) + typeSymbol.Name.Substring(1);
            var displayString = typeSymbol.ToDisplayString();

            var typeSyntax = SyntaxFactory.ParseTypeName(displayString);

            var equalsValueClauseSyntax = SyntaxFactory.EqualsValueClause(creation);

            var variableDeclaratorSyntax = SyntaxFactory.VariableDeclarator(variableName)
                                                        .WithInitializer(equalsValueClauseSyntax);

            var variableDeclarationSyntax = SyntaxFactory.VariableDeclaration(typeSyntax)
                                                         .AddVariables(variableDeclaratorSyntax);

            var declaration = SyntaxFactory.LocalDeclarationStatement(variableDeclarationSyntax);

            var statement = creation.FirstAncestorOrSelf<StatementSyntax>();

            if (statement is null)
            {
                return document;
            }

            var newRoot = root.TrackNodes(creation, statement);

            var currentCreation = newRoot.GetCurrentNode(creation);

            if (currentCreation is null)
            {
                return document;
            }

            var identifierNameSyntax = SyntaxFactory.IdentifierName(variableName);

            newRoot = newRoot.ReplaceNode(currentCreation, identifierNameSyntax);

            // Avoid collection expressions to keep compatibility with netstandard2.0 compiler toolchains
            var currentStatement = newRoot.GetCurrentNode(statement);

            if (currentStatement is null)
            {
                return document;
            }

            newRoot = newRoot.InsertNodesBefore(currentStatement, new[] { declaration });

            return document.WithSyntaxRoot(newRoot);
        }
    }
}