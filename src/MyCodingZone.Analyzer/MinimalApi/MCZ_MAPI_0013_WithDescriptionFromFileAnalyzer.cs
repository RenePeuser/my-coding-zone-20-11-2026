using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MCZ_MAPI_0013_WithDescriptionFromFileAnalyzer : MinimalApiSimpleConventionAnalyzerBase
    {
        public const string Id = "MCZ_MAPI_0013";

        protected override string DiagnosticId => Id;

        protected override string RequiredMethodName => "WithDescriptionFromFile";

        protected override DiagnosticDescriptor Rule { get; } = new(id: Id,
                                                                    title: "Minimal API endpoint must declare a description file",
                                                                    messageFormat: "Add '.WithDescriptionFromFile(\"Description.md\")' to the Minimal API endpoint",
                                                                    description: "Descriptions are required for API documentation. File-based descriptions keep code clean.",
                                                                    category: "MinimalApi.Design",
                                                                    defaultSeverity: DiagnosticSeverity.Info,
                                                                    isEnabledByDefault: false);
    }
}