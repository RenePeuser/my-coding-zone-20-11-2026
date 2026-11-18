using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace MyCodingZone.Analyzer.Tests.TestHarness
{
    internal static class AnalyzerTestHarness
    {
        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source,
                                                                                 DiagnosticAnalyzer analyzer)
        {
            var project = CreateProject(source, analyzer);
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);

            Assert.That.IsNotNull(compilation,
                                  because: "Expected compilation to be created. If this fails, the test harness likely misses required metadata references.",
                                  fix: "Check why 'compilation' is null");

            var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
            var result = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);

            return result;
        }

        public static string Format(ImmutableArray<Diagnostic> diagnostics)
        {
            return diagnostics.IsEmpty
                       ? "<none>"
                       : string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Id} at {diagnostic.Location.GetLineSpan().StartLinePosition}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}"));
        }

        private static Project CreateProject(string source,
                                             DiagnosticAnalyzer analyzer)
        {
            using var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            // Some analyzers are disabled by default (MCZ_MAPI_*); enable every supported id with its default severity
            // or at least Info, so the tests see them.
            var enabledDiagnostics = analyzer.SupportedDiagnostics
                                             .ToDictionary(descriptor => descriptor.Id,
                                                           descriptor => descriptor.IsEnabledByDefault ? ToReportDiagnostic(descriptor.DefaultSeverity) : ReportDiagnostic.Info);

            var solution = workspace.CurrentSolution
                                    .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                                    .WithProjectCompilationOptions(projectId,
                                                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                                                       .WithSpecificDiagnosticOptions(enabledDiagnostics))
                                    .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Latest))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Task).Assembly.Location))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(ImmutableList<>).Assembly.Location))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(LinkedList<>).Assembly.Location))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(System.Collections.ObjectModel.ObservableCollection<>).Assembly.Location))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(ExtensionAttribute).Assembly.Location))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location))
                                    // Required so generic BCL types (List<T>, IEnumerable<T>, ...) fully resolve on .NET Core:
                                    // System.Private.CoreLib alone does not satisfy the System.Runtime type forwarding
                                    // contract the compiler expects; without this, such types fail with CS0012 and any
                                    // code using them degenerates into unresolved/invalid operations that analyzers
                                    // registered via RegisterOperationAction never see.
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location))
                                    .AddDocument(documentId, "Test0.cs", SourceText.From(source));

            return solution.GetProject(projectId)!;
        }

        private static ReportDiagnostic ToReportDiagnostic(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => ReportDiagnostic.Error,
                DiagnosticSeverity.Warning => ReportDiagnostic.Warn,
                DiagnosticSeverity.Info => ReportDiagnostic.Info,
                _ => ReportDiagnostic.Hidden
            };
        }
    }
}
