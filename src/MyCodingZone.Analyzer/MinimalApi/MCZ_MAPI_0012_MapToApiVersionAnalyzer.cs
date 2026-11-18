using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MCZ_MAPI_0012_MapToApiVersionAnalyzer : MinimalApiSimpleConventionAnalyzerBase
    {
        public const string Id = "MCZ_MAPI_0012";

        protected override string DiagnosticId => Id;

        protected override string RequiredMethodName => "MapToApiVersion";

        protected override DiagnosticDescriptor Rule { get; } = new(id: Id,
                                                                    title: "Minimal API endpoint must declare an API version",
                                                                    messageFormat: "Add '.MapToApiVersion(<version>)' to the Minimal API endpoint",
                                                                    description: "Version mapping is required for endpoint versioning.",
                                                                    category: "MinimalApi.Design",
                                                                    defaultSeverity: DiagnosticSeverity.Info,
                                                                    isEnabledByDefault: false);
    }
}