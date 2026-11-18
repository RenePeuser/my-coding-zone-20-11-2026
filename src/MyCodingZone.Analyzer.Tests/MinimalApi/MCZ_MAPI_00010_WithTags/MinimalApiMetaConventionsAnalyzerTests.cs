using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Tests.TestHarness;

namespace MyCodingZone.Analyzer.Tests.MinimalApi.MCZ_MAPI_00010_WithTags
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("MinimalApi")]
    public sealed class MinimalApiMetaConventionsAnalyzerTests
    {
        [TestMethod]
        public async Task Diagnostic_ForMissingWithTags()
        {
            //language=csharp
            const string source = """
                                  using System.Threading.Tasks;
                                  using Microsoft.AspNetCore.Builder;
                                  using Microsoft.AspNetCore.Routing;

                                  public sealed class E
                                  {
                                      public void Map(IEndpointRouteBuilder endpoints)
                                      {
                                          endpoints.MapGet("edges", HandleAsync);

                                          static Task HandleAsync() => Task.CompletedTask;
                                      }
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0010_WithTagsAnalyzer()).ConfigureAwait(false);
            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.AreEqual(MCZ_MAPI_0010_WithTagsAnalyzer.Id,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_MAPI_0010_WithTagsAnalyzer.Id}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_MAPI_0010_WithTagsAnalyzer.Id' and check what produced the different value");
        }

        [TestMethod]
        public async Task NoDiagnostic_WhenAllMetaPresent()
        {
            //language=csharp
            const string source = """
                                  using System.Threading.Tasks;
                                  using Microsoft.AspNetCore.Builder;
                                  using Microsoft.AspNetCore.Routing;

                                  public sealed class E
                                  {
                                      public void Map(IEndpointRouteBuilder endpoints)
                                      {
                                          endpoints.MapGet("edges", HandleAsync)
                                                   .WithTags("Edges")
                                                   .WithName("getAllEdgesV1")
                                                   .MapToApiVersion(1)
                                                   .WithDescriptionFromFile("Description.md")
                                                   .WithSummaryFromFile("Summary.md");

                                          static Task HandleAsync() => Task.CompletedTask;
                                      }
                                  }
                                  """;

            var diagnostics0010 = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0010_WithTagsAnalyzer()).ConfigureAwait(false);

            Assert.That.IsEmpty(diagnostics0010,
                                because: "MCZ_MAPI_0010 must not report an endpoint that already has all conventions",
                                fix: "Check which unexpected diagnostics MCZ_MAPI_0010_WithTagsAnalyzer reports");

            var diagnostics0011 = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0011_WithNameAnalyzer()).ConfigureAwait(false);

            Assert.That.IsEmpty(diagnostics0011,
                                because: "MCZ_MAPI_0011 must not report an endpoint that already has all conventions",
                                fix: "Check which unexpected diagnostics MCZ_MAPI_0011_WithNameAnalyzer reports");

            var diagnostics0012 = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0012_MapToApiVersionAnalyzer()).ConfigureAwait(false);

            Assert.That.IsEmpty(diagnostics0012,
                                because: "MCZ_MAPI_0012 must not report an endpoint that already has all conventions",
                                fix: "Check which unexpected diagnostics MCZ_MAPI_0012_MapToApiVersionAnalyzer reports");

            var diagnostics0013 = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0013_WithDescriptionFromFileAnalyzer()).ConfigureAwait(false);

            Assert.That.IsEmpty(diagnostics0013,
                                because: "MCZ_MAPI_0013 must not report an endpoint that already has all conventions",
                                fix: "Check which unexpected diagnostics MCZ_MAPI_0013_WithDescriptionFromFileAnalyzer reports");

            var diagnostics0014 = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0014_WithSummaryFromFileAnalyzer()).ConfigureAwait(false);

            Assert.That.IsEmpty(diagnostics0014,
                                because: "MCZ_MAPI_0014 must not report an endpoint that already has all conventions",
                                fix: "Check which unexpected diagnostics MCZ_MAPI_0014_WithSummaryFromFileAnalyzer reports");
        }
    }
}