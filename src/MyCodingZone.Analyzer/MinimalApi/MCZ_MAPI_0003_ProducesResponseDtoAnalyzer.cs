// ReSharper disable All
// We are using .NetStandard all optimizations are disabled to keep compatibility with projects using older .Net versions

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MyCodingZone.Analyzer.MinimalApi
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MCZ_MAPI_0003_ProducesResponseDtoAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MCZ_MAPI_0003";

        private static readonly DiagnosticDescriptor MissingProducesDtoRule = new(id: DiagnosticId,
                                                                                  title: "Minimal API endpoint must declare success response DTO",
                                                                                  messageFormat: "Add '.Produces<{0}>()' to the Minimal API endpoint",
                                                                                  description: "Minimal API endpoints returning a concrete response DTO should declare the success response type for OpenAPI accuracy.",
                                                                                  category: "MinimalApi.Design",
                                                                                  defaultSeverity: DiagnosticSeverity.Info,
                                                                                  isEnabledByDefault: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MissingProducesDtoRule);

        public override void Initialize(AnalysisContext context)
        {
            Throw.IfNull(context, nameof(context));

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var chain = MinimalApiChainWalker.TryCreate(invocation, context.SemanticModel, context.CancellationToken);

            if (chain is null)
            {
                return;
            }

            // Analyze each endpoint chain only once (on the root Map* invocation) to avoid duplicate/missed diagnostics.
            if (!ReferenceEquals(chain.Value.RootMapInvocation, invocation))
            {
                return;
            }

            var returnType = MinimalApiHandlerInference.TryGetHandlerReturnType(chain.Value, context.SemanticModel, context.CancellationToken);

            if (!MinimalApiHandlerInference.HasConcreteResponseDto(returnType))
            {
                return;
            }

            var responseDto = MinimalApiHandlerInference.TryGetResponseDtoType(returnType);

            if (responseDto is null)
            {
                return;
            }

            if (IsIResult(responseDto))
            {
                return;
            }

            if (chain.Value.Invocations.Any(i => IsProducesDto(i, context.SemanticModel, context.CancellationToken)))
            {
                return;
            }

            if (chain.Value.Invocations.Any(IsProducesDtoSyntaxOnly))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(MissingProducesDtoRule, chain.Value.RootMapInvocation.GetLocation(), responseDto.Name));
        }

        private static bool IsProducesDto(InvocationExpressionSyntax invocation,
                                          SemanticModel semanticModel,
                                          CancellationToken ct)
        {
            if (semanticModel.GetSymbolInfo(invocation, ct).Symbol is not IMethodSymbol method)
            {
                return false;
            }

            if (!string.Equals(method.Name, "Produces", StringComparison.Ordinal))
            {
                return false;
            }

            if (!method.IsGenericMethod)
            {
                return false;
            }

            // Produces<T>() (no args)
            return invocation.ArgumentList.Arguments.Count == 0;
        }

        private static bool IsProducesDtoSyntaxOnly(InvocationExpressionSyntax invocation)
        {
            // `.Produces<T>()`
            return invocation.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax g } &&
                   g.Identifier.ValueText == "Produces" &&
                   g.TypeArgumentList.Arguments.Count == 1 &&
                   invocation.ArgumentList.Arguments.Count == 0;
        }

        private static bool IsIResult(ITypeSymbol typeSymbol)
        {
            return typeSymbol.Name == "IResult" &&
                   typeSymbol.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Http";
        }
    }
}