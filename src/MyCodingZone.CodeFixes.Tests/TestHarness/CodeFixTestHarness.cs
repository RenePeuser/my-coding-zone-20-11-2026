using AspNetCore.Simple.MsTest.Sdk;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace MyCodingZone.CodeFixes.Tests.TestHarness
{
    internal static class CodeFixTestHarness
    {
        public static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n", StringComparison.Ordinal)
                       .Replace("\r", "\n", StringComparison.Ordinal);
        }

        public static string NormalizeWhitespaceForComparison(string text)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(text);
            var root = syntaxTree.GetRoot();
            return root.NormalizeWhitespace().ToFullString();
        }

        public static void AssertNoReturnNew(string source)
        {
            source = NormalizeLineEndings(source);
            Assert.That.IsFalse(source.Contains("return new ", StringComparison.Ordinal),
                                because: "Expected fixed source to not contain inline object creation in a return statement.",
                                fix: "Check why 'source.Contains(\"return new \", StringComparison.Ordinal)' is true");
        }

        public static void AssertContainsLocalDeclaration(string source, string variableName)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = tree.GetRoot();

            var found = root.DescendantNodes()
                            .OfType<LocalDeclarationStatementSyntax>()
                            .SelectMany(d => d.Declaration.Variables)
                            .Any(v => string.Equals(v.Identifier.ValueText, variableName, StringComparison.Ordinal));

            Assert.That.IsTrue(found,
                               because: $"Expected local variable declaration '{variableName}' in fixed source.",
                               fix: "Check why 'found' is false");
        }

        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source,
                                                                                 DiagnosticAnalyzer analyzer)
        {
            var project = CreateProject(source, analyzer);
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            Assert.That.IsNotNull(compilation,
                                  because: "'compilation' must be set, the following steps of the test depend on it",
                                  fix: "Check why 'compilation' is null");

            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));

            var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);

            return diagnostics;
        }

        public static async Task<(ImmutableArray<Diagnostic> diagnostics, string fixedSource)> ApplySingleFixAsync(string source,
                                                                                                                   DiagnosticAnalyzer analyzer,
                                                                                                                   CodeFixProvider codeFixProvider)
        {
            var project = CreateProject(source, analyzer);
            var document = project.Documents.Single();

            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            Assert.That.IsNotNull(compilation,
                                  because: "'compilation' must be set, the following steps of the test depend on it",
                                  fix: "Check why 'compilation' is null");

            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);

            Assert.That.IsNotEmpty(diagnostics,
                                   because: "Expected at least one diagnostic.",
                                   fix: "Check why 'diagnostics' is empty");

            var targetId = analyzer.SupportedDiagnostics.FirstOrDefault()?.Id;

            var diagnostic = targetId is null
                                 ? diagnostics[0]
                                 : diagnostics.FirstOrDefault(d => d.Id == targetId);

            Assert.That.IsNotNull(diagnostic,
                                  because: "Expected at least one diagnostic for the provided analyzer.",
                                  fix: "Check why 'diagnostic' is null");

            var actions = new List<CodeAction>();

            var context = new CodeFixContext(document, diagnostic, (a, _) => actions.Add(a), CancellationToken.None);

            await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            Assert.That.IsNotEmpty(actions,
                                   because: "Expected at least one code action.",
                                   fix: "Check why 'actions' is empty");

            var operations = await actions[0].GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
            var operation = operations.OfType<ApplyChangesOperation>()
                                      .Single();

            var newSolution = operation.ChangedSolution;
            var newDocument = newSolution.GetDocument(document.Id);
            Assert.That.IsNotNull(newDocument,
                                  because: "'newDocument' must be set, the following steps of the test depend on it",
                                  fix: "Check why 'newDocument' is null");

            var newText = await newDocument.GetTextAsync().ConfigureAwait(false);

            return (diagnostics, newText.ToString());
        }

        private static Project CreateProject(string source,
                                             DiagnosticAnalyzer analyzer)
        {
            using var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var enabledDiagnostics = analyzer.SupportedDiagnostics
                                             .ToDictionary(d => d.Id, _ => ReportDiagnostic.Info);

            // Some analyzers in this repo are disabled-by-default; make sure their ids are enabled in tests.
            enabledDiagnostics.TryAdd("MCZ_MAPI_0004", ReportDiagnostic.Info);

            var solution = workspace.CurrentSolution
                                    .AddProject(projectId, "TestProject", "TestProject",
                                                LanguageNames.CSharp)
                                    .WithProjectCompilationOptions(projectId,
                                                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                                                       .WithSpecificDiagnosticOptions(enabledDiagnostics))
                                    .WithProjectParseOptions(projectId,
                                                             new CSharpParseOptions(LanguageVersion.Latest))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Task).Assembly.Location))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location))
                                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(CodeFixProvider).Assembly.Location))
                                    .AddDocument(documentId, "Test0.cs", SourceText.From(source));

            return solution.GetProject(projectId)!;
        }
    }
}