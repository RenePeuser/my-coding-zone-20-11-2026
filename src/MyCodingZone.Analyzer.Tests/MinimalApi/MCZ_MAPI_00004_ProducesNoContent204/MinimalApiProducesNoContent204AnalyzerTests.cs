using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Tests.TestHarness;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.Analyzer.Tests.MinimalApi.MCZ_MAPI_00004_ProducesNoContent204
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("MinimalApi")]
    public sealed class MinimalApiProducesNoContent204AnalyzerTests
    {
        [TestMethod]
        public async Task Diagnostic_ForTaskHandlerMissingProduces204()
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
                                          endpoints.MapDelete("edges", HandleAsync);

                                          static Task HandleAsync() => Task.CompletedTask;
                                      }
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0004_ProducesNoContent204Analyzer()).ConfigureAwait(false);
            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.AreEqual(MCZ_MAPI_0004_ProducesNoContent204Analyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_MAPI_0004_ProducesNoContent204Analyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_MAPI_0004_ProducesNoContent204Analyzer.DiagnosticId' and check what produced the different value");
        }

        [TestMethod]
        public async Task NoDiagnostic_WhenProduces204Exists()
        {
            //language=csharp
            const string source = """
                                  using System.Threading.Tasks;
                                  using Microsoft.AspNetCore.Builder;
                                  using Microsoft.AspNetCore.Routing;
                                  using Microsoft.AspNetCore.Http;

                                  public sealed class E
                                  {
                                      public void Map(IEndpointRouteBuilder endpoints)
                                      {
                                          endpoints.MapDelete("edges", HandleAsync)
                                                   .Produces(StatusCodes.Status204NoContent);

                                          static Task HandleAsync() => Task.CompletedTask;
                                      }
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0004_ProducesNoContent204Analyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }

        [TestMethod]
        public async Task NoDiagnostic_ForFileEndpoint_WhenProducesFileStreamResultExists()
        {
            //language=csharp
            const string source = """
                                  using System.Threading.Tasks;
                                  using Microsoft.AspNetCore.Builder;
                                  using Microsoft.AspNetCore.Routing;
                                  using Microsoft.AspNetCore.Http;
                                  using Microsoft.AspNetCore.Mvc;

                                  public sealed class E
                                  {
                                      public void Map(IEndpointRouteBuilder endpoints)
                                      {
                                          endpoints.MapGet("photo", HandleAsync)
                                                   .Produces<FileStreamResult>();

                                          static Task<IResult> HandleAsync(HttpContext ctx)
                                              => Task.FromResult(Results.File(new System.IO.MemoryStream(), "image/png") as IResult);
                                      }
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0004_ProducesNoContent204Analyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }

        [TestMethod]
        [DataRow("", true, DisplayName = "no Produces at all")]
        [DataRow(".Produces<string>(StatusCodes.Status200OK, \"text/plain\")", true, DisplayName = "Produces of a non-binary type")]
        [DataRow(".Produces<Stream>(StatusCodes.Status200OK, \"image/jpeg\")", false, DisplayName = "Produces Stream")]
        [DataRow(".Produces<byte[]>(StatusCodes.Status200OK, \"image/jpeg\")", false, DisplayName = "Produces byte array")]
        public async Task BinaryEndpoint_IsOnlyExemptWhenItDeclaresAStreamOrByteArray(string producesCall,
                                                                                      bool expectDiagnostic)
        {
            // Same endpoint in every case, so the Produces call is the only thing that can silence the rule;
            // the first two cases prove the source is flagged at all, which keeps the exemption cases from passing vacuously.
            var source = $$"""
                           using System.IO;
                           using System.Threading.Tasks;
                           using Microsoft.AspNetCore.Builder;
                           using Microsoft.AspNetCore.Routing;
                           using Microsoft.AspNetCore.Http;

                           public sealed class E
                           {
                               public void Map(IEndpointRouteBuilder endpoints)
                               {
                                   endpoints.MapGet("photo", HandleAsync){{producesCall}};

                                   // A `Task` handler: the harness has no ASP.NET refs, so only this shape is flagged without the exemption.
                                   static Task HandleAsync() => Task.CompletedTask;
                               }
                           }
                           """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0004_ProducesNoContent204Analyzer()).ConfigureAwait(false);

            Assert.That.AreEqual(expectDiagnostic ? 1 : 0, diagnostics.Length,
                                 because: $"The rule must report exactly when the binary endpoint declares no Stream or byte[] response.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check the Stream and byte[] exemption in MCZ_MAPI_0004_ProducesNoContent204Analyzer");
        }

        // NOTE: `Task<IResult>` scenarios require ASP.NET Core references for return-type inference.
        // This is validated via code-fix tests / integration usage.

        // NOTE: `Task<IResult>` scenarios require ASP.NET Core types to be available for reliable return-type inference.
        // Analyzer coverage for this case is validated via code-fix tests.
    }
}