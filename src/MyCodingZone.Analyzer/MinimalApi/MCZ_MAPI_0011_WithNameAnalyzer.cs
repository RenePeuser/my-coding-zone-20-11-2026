using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MCZ_MAPI_0011_WithNameAnalyzer : MinimalApiSimpleConventionAnalyzerBase
    {
        public const string Id = "MCZ_MAPI_0011";

        protected override string DiagnosticId => Id;

        protected override string RequiredMethodName => "WithName";

        protected override DiagnosticDescriptor Rule { get; } = new(id: Id,
                                                                    title: "Minimal API endpoint must declare a name",
                                                                    messageFormat: "Add '.WithName(\"...\")' to the Minimal API endpoint",
                                                                    description: "Endpoint names are required for routing, client generation, and consistent monitoring.",
                                                                    category: "MinimalApi.Design",
                                                                    defaultSeverity: DiagnosticSeverity.Info,
                                                                    isEnabledByDefault: false);
    }
}