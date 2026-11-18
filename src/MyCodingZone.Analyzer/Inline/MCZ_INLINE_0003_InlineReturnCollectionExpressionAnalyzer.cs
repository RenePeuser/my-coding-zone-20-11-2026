using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MyCodingZone.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MCZ_INLINE_0003_InlineReturnCollectionExpressionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MCZ_INLINE_0003";

        private static readonly DiagnosticDescriptor Rule = new(id: DiagnosticId,
                                                                title: "Do not inline collection expressions in return statements",
                                                                messageFormat: "Extract the collection expression into a local variable for better debugability",
                                                                category: "Readability",
                                                                defaultSeverity: DiagnosticSeverity.Warning,
                                                                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            Throw.IfNull(context, nameof(context));

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.CollectionExpression);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var collectionExpr = (CollectionExpressionSyntax)context.Node;

            // allow: yield return [...]
            if (collectionExpr.Parent is YieldStatementSyntax)
            {
                return;
            }

            // Only care about: return [...]
            if (collectionExpr.Parent is not ReturnStatementSyntax)
            {
                return;
            }

            // allow: return [];
            if (collectionExpr.Elements.Count == 0)
            {
                return;
            }

            // disallow: return [x], return [..items], return [1,2,3], etc.
            var location = collectionExpr.GetLocation();
            var diagnostic = Diagnostic.Create(Rule, location);
            context.ReportDiagnostic(diagnostic);
        }
    }
}