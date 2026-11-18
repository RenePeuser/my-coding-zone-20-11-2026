using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Tests.TestHarness;

namespace MyCodingZone.Analyzer.Tests.MinimalApi.MCZ_MAPI_00015_EndpointHandlerHaveNotToBeStatic
{
    [TestClass]
    [TestCategory("MinimalApi")]
    [TestCategory("Roslyn Analyzer")]
    public sealed class MinimalApiEndpointHandlerHaveNotToBeStaticAnalyzerTests
    {
        [TestMethod]
        public async Task Diagnostic_ForStaticLocalFunctionHandler()
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
                                          endpoints.MapPost("comments", HandleAsync);

                                          static Task HandleAsync(CreateCommentRequest req) => Task.CompletedTask;
                                      }
                                  }

                                  public sealed class CreateCommentRequest { }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzer()).ConfigureAwait(false);
            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.AreEqual(MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzer.DiagnosticId, diagnostics[0].Id,
                                 because: "'diagnostics[0].Id' must equal 'MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzer.DiagnosticId'",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzer.DiagnosticId' and check what produced the different value");
        }

        [TestMethod]
        public async Task NoDiagnostic_ForNonStaticLocalFunctionHandler()
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
                                          endpoints.MapPost("comments", HandleAsync);

                                          Task HandleAsync(CreateCommentRequest req) => Task.CompletedTask;
                                      }
                                  }

                                  public sealed class CreateCommentRequest { }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }

        [TestMethod]
        public async Task NoDiagnostic_ForLambdaHandler()
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
                                          endpoints.MapPost("comments", (CreateCommentRequest req) => Task.CompletedTask);
                                      }
                                  }

                                  public sealed class CreateCommentRequest { }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }
    }
}