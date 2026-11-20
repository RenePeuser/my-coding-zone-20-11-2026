// ReSharper disable All
// We are using .NET Standard; keep logic explicit and deterministic.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MyCodingZone.Analyzer.MinimalApi
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MCZ_MAPI_0002";

        private static readonly DiagnosticDescriptor Rule =
            new(id: DiagnosticId,
                title: "Minimal API endpoint must not declare Accepts<T>",
                messageFormat: "Remove '.Accepts<...>()' from this Minimal API endpoint",
                description:
                "GET/DELETE Minimal API endpoints must not declare request bodies via Accepts<T>.",
                category: "MinimalApi.Design",
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            Throw.IfNull(context, nameof(context));

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeInvocation,
                                             SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var chain = MinimalApiChainWalker.TryCreate(invocation,
                                                        context.SemanticModel,
                                                        context.CancellationToken);

            if (chain is null)
            {
                return;
            }

            // Analyze each endpoint only once (on the root Map* call)
            if (!ReferenceEquals(chain.Value.RootMapInvocation, invocation))
            {
                return;
            }

            var rootMapMethod = chain.Value.RootMapMethodName;

            if (!MinimalApiSemanticRules.ForbidsAccepts(rootMapMethod))
            {
                return;
            }

            var acceptsInvocation = chain.Value.Invocations.FirstOrDefault(IsAcceptsSyntax);

            if (acceptsInvocation is null)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule,
                                                       acceptsInvocation.GetLocation()));
        }

        // ------------------------------------------------------------
        // Accepts detection (syntax-only, deterministic, fast)
        // ------------------------------------------------------------

        private static bool IsAcceptsSyntax(InvocationExpressionSyntax invocation)
        {
            return invocation.Expression switch
            {
                MemberAccessExpressionSyntax { Name: GenericNameSyntax g }
                    when g.Identifier.ValueText == "Accepts"
                         && g.TypeArgumentList.Arguments.Count == 1
                    => true,

                MemberAccessExpressionSyntax { Name: IdentifierNameSyntax i }
                    when i.Identifier.ValueText == "Accepts"
                    => true,

                MemberBindingExpressionSyntax { Name: GenericNameSyntax g }
                    when g.Identifier.ValueText == "Accepts"
                         && g.TypeArgumentList.Arguments.Count == 1
                    => true,

                MemberBindingExpressionSyntax { Name: IdentifierNameSyntax i }
                    when i.Identifier.ValueText == "Accepts"
                    => true,

                _ => false
            };
        }
    }
}