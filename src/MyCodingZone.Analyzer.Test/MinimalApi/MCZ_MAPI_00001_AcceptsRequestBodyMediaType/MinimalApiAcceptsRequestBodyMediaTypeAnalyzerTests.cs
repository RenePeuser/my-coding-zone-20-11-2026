using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Test.TestHarness;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.Analyzer.Test.MinimalApi.MCZ_MAPI_00001_AcceptsRequestBodyMediaType
{
    [TestClass]
    [TestCategory("MinimalApi")]
    [TestCategory("Roslyn Analyzer")]
    public sealed class MinimalApiAcceptsRequestBodyMediaTypeAnalyzerTests
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

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }

        [TestMethod]
        public async Task Diagnostic_ForMapPostMissingAccepts_WhenBodyExists()
        {
            //language=csharp
            const string source = """
                                  using System.Threading.Tasks;

                                  public interface IEndpointRouteBuilder { }

                                  public static class EndpointRouteBuilderExtensions
                                  {
                                      public static object MapPost(this IEndpointRouteBuilder endpoints, string pattern, System.Func<CreateEdgeRequest, Task> handler)
                                          => new object();
                                  }

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

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer()).ConfigureAwait(false);
            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.AreEqual(MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId' and check what produced the different value");
        }

        [TestMethod]
        public async Task Diagnostic_ForMapPostMissingAccepts_WhenHandlerSymbolCannotBeResolved()
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

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer()).ConfigureAwait(false);
            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.AreEqual(MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId' and check what produced the different value");
        }

        [TestMethod]
        public async Task NoDiagnostic_ForMapGetWithAccepts()
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

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }

        [TestMethod]
        public async Task Diagnostic_ForMapPostMissingAccepts_WhenHandlerIsMethodGroupAndBodyExists()
        {
            //language=csharp
            const string source = """
                                  using System.Threading.Tasks;

                                  public interface IEndpointRouteBuilder { }

                                  public static class EndpointRouteBuilderExtensions
                                  {
                                      public static object MapPost(this IEndpointRouteBuilder endpoints, string pattern, System.Func<CreateEdgeRequest, Task> handler)
                                          => new object();
                                  }

                                  public sealed class E
                                  {
                                      public void Map(IEndpointRouteBuilder endpoints)
                                      {
                                          endpoints.MapPost("edges", HandleAsync);
                                      }

                                      private static Task HandleAsync(CreateEdgeRequest req) => Task.CompletedTask;
                                  }

                                  public sealed class CreateEdgeRequest { }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer()).ConfigureAwait(false);
            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.AreEqual(MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId' and check what produced the different value");
        }

        [TestMethod]
        public async Task Diagnostic_ForMapPostMissingAccepts_WhenHandlerIsStaticLocalFunctionMethodGroupAndBodyExists()
        {
            //language=csharp
            const string source = """
                                  using System.Threading.Tasks;

                                  public interface IEndpointRouteBuilder { }

                                  public static class EndpointRouteBuilderExtensions
                                  {
                                      public static object MapPost(this IEndpointRouteBuilder endpoints, string pattern, System.Func<CreateEdgeRequest, Task> handler)
                                          => new object();
                                  }

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

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer()).ConfigureAwait(false);
            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.AreEqual(MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer.DiagnosticId' and check what produced the different value");
        }
    }
}