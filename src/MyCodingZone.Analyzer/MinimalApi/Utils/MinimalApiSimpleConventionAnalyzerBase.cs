// ReSharper disable All
// We are using .NetStandard all optimizations are disabled to keep compatibility with projects using older .Net versions

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MyCodingZone.Analyzer.MinimalApi
{
    public abstract class MinimalApiSimpleConventionAnalyzerBase : DiagnosticAnalyzer
    {
        protected abstract string DiagnosticId { get; }

        protected abstract DiagnosticDescriptor Rule { get; }

        protected abstract string RequiredMethodName { get; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            Throw.IfNull(context, nameof(context));

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var chain = MinimalApiChainWalker.TryCreate(invocation, context.SemanticModel, context.CancellationToken);

            if (chain is null)
            {
                return;
            }

            // Analyze each endpoint chain only once (on the root Map* invocation) to avoid duplicate diagnostics.
            if (!ReferenceEquals(chain.Value.RootMapInvocation, invocation))
            {
                return;
            }

            if (chain.Value.Invocations.Any(i => IsMethod(i, context.SemanticModel, RequiredMethodName,
                                                          context.CancellationToken)))
            {
                return;
            }

            if (chain.Value.Invocations.Any(i => IsMethodSyntaxOnly(i, RequiredMethodName)))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, chain.Value.RootMapInvocation.GetLocation()));
        }

        protected static bool IsMethod(InvocationExpressionSyntax invocation,
                                       SemanticModel semanticModel,
                                       string methodName,
                                       CancellationToken ct)
        {
            if (semanticModel.GetSymbolInfo(invocation, ct).Symbol is not IMethodSymbol method)
            {
                return false;
            }

            return string.Equals(method.Name, methodName, StringComparison.Ordinal);
        }

        private static bool IsMethodSyntaxOnly(InvocationExpressionSyntax invocation,
                                               string methodName)
        {
            return invocation.Expression switch
            {
                MemberAccessExpressionSyntax m => string.Equals(m.Name.Identifier.ValueText, methodName, StringComparison.Ordinal),
                MemberBindingExpressionSyntax b => string.Equals(b.Name.Identifier.ValueText, methodName, StringComparison.Ordinal),
                _ => false
            };
        }
    }
}