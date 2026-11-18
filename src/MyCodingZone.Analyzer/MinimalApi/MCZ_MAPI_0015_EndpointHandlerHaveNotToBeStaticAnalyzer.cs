using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MyCodingZone.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MCZ_MAPI_0015";

        private static readonly DiagnosticDescriptor Rule =
            new(id: DiagnosticId,
                title: "Minimal API endpoint handler must not be static",
                messageFormat: "Make the endpoint handler non-static",
                description:
                "Minimal API endpoint handlers must not be declared as static local functions. Use the endpoint class constructor for DI instead of injecting services into the handler.",
                category: "MinimalApi.Design",
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            Throw.IfNull(context, nameof(context));

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var chain = MinimalApiChainWalker.TryCreate(invocation, context.SemanticModel, context.CancellationToken);

            if (chain is null)
            {
                return;
            }

            if (!ReferenceEquals(chain.Value.RootMapInvocation, invocation))
            {
                return;
            }

            if (chain.Value.RootMapInvocation.ArgumentList.Arguments.Count < 2)
            {
                return;
            }

            var handlerExpression = chain.Value.RootMapInvocation.ArgumentList.Arguments[1].Expression;

            if (handlerExpression is not IdentifierNameSyntax identifier)
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(handlerExpression, context.CancellationToken).Symbol as IMethodSymbol;

            if (symbol is not null)
            {
                if (symbol.IsStatic)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, identifier.GetLocation()));
                }

                return;
            }

            var containingMethod = chain.Value.RootMapInvocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            if (containingMethod?.Body is null)
            {
                return;
            }

            var localFunc = containingMethod.Body.DescendantNodes()
                                       .OfType<LocalFunctionStatementSyntax>()
                                       .FirstOrDefault(l => string.Equals(l.Identifier.ValueText, identifier.Identifier.ValueText, StringComparison.Ordinal));

            if (localFunc is null)
            {
                return;
            }

            if (localFunc.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, identifier.GetLocation()));
            }
        }
    }
}