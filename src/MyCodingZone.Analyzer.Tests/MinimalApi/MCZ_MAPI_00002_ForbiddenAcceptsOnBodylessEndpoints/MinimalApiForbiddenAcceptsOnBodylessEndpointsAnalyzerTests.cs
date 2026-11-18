using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Tests.TestHarness;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.Analyzer.Tests.MinimalApi.MCZ_MAPI_00002_ForbiddenAcceptsOnBodylessEndpoints
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("MinimalApi")]
    public sealed class MinimalApiForbiddenAcceptsOnBodylessEndpointsAnalyzerTests
    {
        [TestMethod]
        public async Task NoDiagnostic_ForMapPostWithAccepts()
        {
            //language=csharp
            const string source = """
                                  using System.Net.Mime;
                                  using System.Threading.Tasks;
                                  using Microsoft.AspNetCore.Builder;
                                  using Microsoft.AspNetCore.Routing;

                                  public sealed class E
                                  {
                                      public void Map(IEndpointRouteBuilder endpoints)
                                      {
                                          endpoints.MapPost("edges", HandleAsync)
                                                   .Accepts<CreateEdgeRequest>(MediaTypeNames.Application.Json);

                                          static Task HandleAsync(CreateEdgeRequest req) => Task.CompletedTask;
                                      }
                                  }

                                  public sealed class CreateEdgeRequest { }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }

        [TestMethod]
        public async Task NoDiagnostic_ForMapPutWithNonGenericAccepts()
        {
            //language=csharp
            const string source = """
                                  using System;
                                  using System.Net.Mime;
                                  using System.Threading.Tasks;
                                  using Microsoft.AspNetCore.Builder;
                                  using Microsoft.AspNetCore.Routing;

                                  public sealed class E
                                  {
                                      public void Map(IEndpointRouteBuilder endpoints)
                                      {
                                          var route = "edges";
                                          var requestTypeToUse = typeof(CreateEdgeRequest);

                                          endpoints.MapPut($"{route}/{{id}}", (CreateEdgeRequest req) => Task.CompletedTask)
                                                   .Accepts(requestTypeToUse, MediaTypeNames.Application.Json);
                                      }
                                  }

                                  public sealed class CreateEdgeRequest { }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }

        [TestMethod]
        public async Task NoDiagnostic_ForMapPostWithAcceptsFullyQualifiedMediaType()
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
                                          endpoints.MapPost("edges", HandleAsync)
                                                   .Accepts<CreateEdgeRequest>(System.Net.Mime.MediaTypeNames.Application.Json);

                                          static Task HandleAsync(CreateEdgeRequest req) => Task.CompletedTask;
                                      }
                                  }

                                  public sealed class CreateEdgeRequest { }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }

        [TestMethod]
        public async Task NoDiagnostic_ForMapPostMissingAccepts_WhenHandlerSymbolCannotBeResolved()
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
                                          endpoints.MapPost("edges", HandleAsync);

                                          static Task HandleAsync(CreateEdgeRequest req) => Task.CompletedTask;
                                      }
                                  }

                                  public sealed class CreateEdgeRequest { }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }

        [TestMethod]
        public async Task Diagnostic_ForMapGetWithAccepts()
        {
            //language=csharp
            const string source = """
                                  using System.Net.Mime;
                                  using System.Threading.Tasks;
                                  using Microsoft.AspNetCore.Builder;
                                  using Microsoft.AspNetCore.Routing;

                                  public sealed class E
                                  {
                                      public void Map(IEndpointRouteBuilder endpoints)
                                      {
                                          endpoints.MapGet("edges", HandleAsync)
                                                   .Accepts<CreateEdgeRequest>(MediaTypeNames.Application.Json);

                                          static Task HandleAsync() => Task.CompletedTask;
                                      }
                                  }

                                  public sealed class CreateEdgeRequest { }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzer()).ConfigureAwait(false);
            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.AreEqual(MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzer.DiagnosticId' and check what produced the different value");
        }
    }
}