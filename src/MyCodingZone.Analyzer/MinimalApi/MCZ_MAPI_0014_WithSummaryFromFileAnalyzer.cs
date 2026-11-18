using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MCZ_MAPI_0014_WithSummaryFromFileAnalyzer : MinimalApiSimpleConventionAnalyzerBase
    {
        public const string Id = "MCZ_MAPI_0014";

        protected override string DiagnosticId => Id;

        protected override string RequiredMethodName => "WithSummaryFromFile";

        protected override DiagnosticDescriptor Rule { get; } = new(id: Id,
                                                                    title: "Minimal API endpoint must declare a summary file",
                                                                    messageFormat: "Add '.WithSummaryFromFile(\"Summary.md\")' to the Minimal API endpoint",
                                                                    description: "Summaries are required for API documentation. File-based summaries keep code clean.",
                                                                    category: "MinimalApi.Design",
                                                                    defaultSeverity: DiagnosticSeverity.Info,
                                                                    isEnabledByDefault: false);
    }
}