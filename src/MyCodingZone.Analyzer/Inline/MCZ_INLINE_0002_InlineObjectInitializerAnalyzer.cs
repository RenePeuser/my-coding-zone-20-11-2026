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
    public sealed class MCZ_INLINE_0002_InlineObjectInitializerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MCZ_INLINE_0002";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(id: DiagnosticId,
                                                                                     title: "Do not inline complex object initializer",
                                                                                     messageFormat: "Extract object initializer into a separate variable for better debugability",
                                                                                     category: "Readability",
                                                                                     DiagnosticSeverity.Warning,
                                                                                     isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            Throw.IfNull(context, nameof(context));

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;

            // Case: await client.MethodAsync(new SomeRequest { ... })
            if (creation.Parent is ArgumentSyntax argument &&
                argument.Parent?.Parent is InvocationExpressionSyntax)
            {
                // Only warn if initializer exists (= complex)
                if (creation.Initializer?.Expressions.Count > 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation()));
                }
            }
        }
    }
}