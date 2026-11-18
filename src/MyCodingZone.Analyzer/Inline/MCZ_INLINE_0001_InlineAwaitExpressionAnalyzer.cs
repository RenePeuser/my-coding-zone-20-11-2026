// ReSharper disable All
// We are using .NetStandard all optimizations are disabled to keep compatibility with projects using older .Net versions

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MyCodingZone.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SuppressMessage("ReSharper", "")]
    public sealed class MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MCZ_INLINE_0001";

        private static readonly DiagnosticDescriptor Rule = new(id: DiagnosticId,
                                                                title: "Do not inline awaited invocations",
                                                                messageFormat: "Extract the awaited method call into a local variable for better debugability",
                                                                category: "Readability",
                                                                DiagnosticSeverity.Warning,
                                                                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            Throw.IfNull(context, nameof(context));

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeAwait, SyntaxKind.AwaitExpression);
        }

        private void AnalyzeAwait(SyntaxNodeAnalysisContext context)
        {
            var awaitExpr = (AwaitExpressionSyntax)context.Node;

            // ✔ Allowed: await FooAsync();
            if (awaitExpr.Parent is ExpressionStatementSyntax)
            {
                return;
            }

            // ✔ Allowed: await FooAsync().ConfigureAwait(false);
            if (awaitExpr.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Parent is InvocationExpressionSyntax invocation &&
                invocation.Parent is ExpressionStatementSyntax)
            {
                return;
            }

            // ✔ Allowed: var x = await FooAsync();
            if (awaitExpr.Parent is EqualsValueClauseSyntax &&
                awaitExpr.Parent.Parent is VariableDeclaratorSyntax)
            {
                return;
            }

            // ✔ Allowed: x = await FooAsync();
            if (awaitExpr.Parent is AssignmentExpressionSyntax assignment &&
                assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) &&
                assignment.Parent is ExpressionStatementSyntax)
            {
                return;
            }

            // ❌ Not allowed: inline awaits inside other expressions
            var location = awaitExpr.GetLocation();
            var diagnostic = Diagnostic.Create(Rule, location);
            context.ReportDiagnostic(diagnostic);
        }
    }
}