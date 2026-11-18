// ReSharper disable All
// We are using .NetStandard all optimizations are disabled to keep compatibility with projects using older .Net versions

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MyCodingZone.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MCZ_INLINE_0004_InlineReturnObjectCreationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MCZ_INLINE_0004";

        private static readonly DiagnosticDescriptor Rule = new(id: DiagnosticId,
                                                                title: "Do not inline object creation in return statements",
                                                                messageFormat: "Extract the returned object into a local variable for better debugability",
                                                                category: "Readability",
                                                                defaultSeverity: DiagnosticSeverity.Warning,
                                                                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            Throw.IfNull(context, nameof(context));

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;

            // Only report when the object creation is directly returned:
            // - `return new Foo(...);`
            // - `return new Foo { ... };`
            // but NOT when it is assigned first:
            // - `var x = new Foo(); return x;`
            var returnStatement = creation.FirstAncestorOrSelf<ReturnStatementSyntax>();
            if (returnStatement is null)
            {
                return;
            }

            // Allowed: yield return new Foo();
            if (creation.FirstAncestorOrSelf<YieldStatementSyntax>() is not null)
            {
                return;
            }

            // Ensure the return statement is returning this exact object creation.
            if (!ReferenceEquals(returnStatement.Expression, creation) &&
                !ReferenceEquals(returnStatement.Expression, creation.Parent))
            {
                return;
            }

            // Disallowed: return new Foo();
            var location = creation.GetLocation();
            var diagnostic = Diagnostic.Create(Rule, location);
            context.ReportDiagnostic(diagnostic);
        }
    }
}