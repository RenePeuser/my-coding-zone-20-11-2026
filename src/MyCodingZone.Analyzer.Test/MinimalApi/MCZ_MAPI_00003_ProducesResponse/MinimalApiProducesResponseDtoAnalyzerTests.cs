using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Test.TestHarness;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.Analyzer.Test.MinimalApi.MCZ_MAPI_00003_ProducesResponse
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("MinimalApi")]
    public sealed class MinimalApiProducesResponseDtoAnalyzerTests
    {
        [TestMethod]
        public async Task NoDiagnostic_WhenProducesDtoExists()
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
                                                   .Produces<CreateEdgeResponse>();

                                          static Task<CreateEdgeResponse> HandleAsync() => Task.FromResult(new CreateEdgeResponse());
                                      }
                                  }

                                  public sealed class CreateEdgeResponse { }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0003_ProducesResponseDtoAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }

        [TestMethod]
        public async Task Diagnostic_WhenMissingProducesDto()
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

                                          static Task<CreateEdgeResponse> HandleAsync() => Task.FromResult(new CreateEdgeResponse());
                                      }
                                  }

                                  public sealed class CreateEdgeResponse { }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0003_ProducesResponseDtoAnalyzer()).ConfigureAwait(false);
            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.AreEqual(MCZ_MAPI_0003_ProducesResponseDtoAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_MAPI_0003_ProducesResponseDtoAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_MAPI_0003_ProducesResponseDtoAnalyzer.DiagnosticId' and check what produced the different value");
        }

        [TestMethod]
        public async Task NoDiagnostic_ForTaskIResult_WhenProduces204Exists()
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
                                          endpoints.MapPost("admin/health-check", HandleAsync)
                                                   .Produces(StatusCodes.Status204NoContent);

                                          static Task HandleAsync() => Task.CompletedTask;
                                      }
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0003_ProducesResponseDtoAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }
    }
}