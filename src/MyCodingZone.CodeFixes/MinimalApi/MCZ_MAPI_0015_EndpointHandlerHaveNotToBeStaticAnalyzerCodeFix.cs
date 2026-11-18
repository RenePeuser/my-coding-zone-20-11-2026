using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzerCodeFix))]
    [Shared]
    public sealed class MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzerCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzer.DiagnosticId);

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

            var localFunction = TryFindLocalFunction(node) ?? TryFindLocalFunctionFromMapInvocation(root);

            if (localFunction is null || !localFunction.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
            {
                return;
            }

            context.RegisterCodeFix(CodeAction.Create("Make handler non-static",
                                                      ct => MakeNonStaticAsync(context.Document, localFunction, ct),
                                                      nameof(MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzerCodeFix)),
                                    diagnostic);
        }

        private static async Task<Document> MakeNonStaticAsync(Document document,
                                                               LocalFunctionStatementSyntax localFunction,
                                                               CancellationToken ct)
        {
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);

            var newModifiers = SyntaxFactory.TokenList(localFunction.Modifiers.Where(m => !m.IsKind(SyntaxKind.StaticKeyword)));
            editor.ReplaceNode(localFunction, localFunction.WithModifiers(newModifiers));

            return editor.GetChangedDocument();
        }

        private static LocalFunctionStatementSyntax? TryFindLocalFunction(SyntaxNode node)
        {
            // Preferred: diagnostic was placed directly on the local function.
            var localFunction = node.FirstAncestorOrSelf<LocalFunctionStatementSyntax>();

            if (localFunction is not null)
            {
                return localFunction;
            }

            // Fallback: diagnostic is on the handler identifier in the Map* call.
            var identifier = node as IdentifierNameSyntax ?? node.FirstAncestorOrSelf<IdentifierNameSyntax>();

            if (identifier is null)
            {
                return null;
            }

            var containingMethod = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            if (containingMethod?.Body is null)
            {
                return null;
            }

            return containingMethod.Body.DescendantNodes()
                                  .OfType<LocalFunctionStatementSyntax>()
                                  .FirstOrDefault(l => string.Equals(l.Identifier.ValueText, identifier.Identifier.ValueText, StringComparison.Ordinal));
        }

        private static LocalFunctionStatementSyntax? TryFindLocalFunctionFromMapInvocation(SyntaxNode root)
        {
            var mapInvocation = root.DescendantNodes()
                                    .OfType<InvocationExpressionSyntax>()
                                    .FirstOrDefault(IsMapInvocationWithIdentifierHandler);

            if (mapInvocation is null)
            {
                return null;
            }

            if (mapInvocation.ArgumentList.Arguments[1].Expression is not IdentifierNameSyntax handlerIdentifier)
            {
                return null;
            }

            var containingMethod = mapInvocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            if (containingMethod?.Body is null)
            {
                return null;
            }

            return containingMethod.Body.DescendantNodes()
                                  .OfType<LocalFunctionStatementSyntax>()
                                  .FirstOrDefault(l => string.Equals(l.Identifier.ValueText, handlerIdentifier.Identifier.ValueText, StringComparison.Ordinal));
        }

        private static bool IsMapInvocationWithIdentifierHandler(InvocationExpressionSyntax invocation)
        {
            if (invocation.ArgumentList.Arguments.Count < 2)
            {
                return false;
            }

            if (invocation.ArgumentList.Arguments[1].Expression is not IdentifierNameSyntax)
            {
                return false;
            }

            return invocation.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText.StartsWith("Map", StringComparison.Ordinal),
                _ => false
            };
        }
    }
}