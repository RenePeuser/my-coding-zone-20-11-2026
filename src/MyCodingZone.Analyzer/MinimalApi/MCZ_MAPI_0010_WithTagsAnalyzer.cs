using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MCZ_MAPI_0010_WithTagsAnalyzer : MinimalApiSimpleConventionAnalyzerBase
    {
        public const string Id = "MCZ_MAPI_0010";

        protected override string DiagnosticId => Id;

        protected override string RequiredMethodName => "WithTags";

        protected override DiagnosticDescriptor Rule { get; } = new(id: Id,
                                                                    title: "Minimal API endpoint must declare tags",
                                                                    messageFormat: "Add '.WithTags(\"...\")' to the Minimal API endpoint",
                                                                    description: "Tags are required for consistent OpenAPI grouping.",
                                                                    category: "MinimalApi.Design",
                                                                    defaultSeverity: DiagnosticSeverity.Info,
                                                                    isEnabledByDefault: false);
    }
}