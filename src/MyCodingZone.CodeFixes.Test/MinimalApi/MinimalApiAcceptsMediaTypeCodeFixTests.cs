using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.CodeFixes.Test.TestHarness;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.CodeFixes.Test.MinimalApi
{
    [TestClass]
    [TestCategory("MinimalApi")]
    [TestCategory("Roslyn CodeFixes")]
    public sealed class MinimalApiAcceptsRequestBodyMediaTypeCodeFixTests
    {
        [TestMethod]
        public async Task Fix_AddsAccepts_ForMapPost()
        {
            const string source = """
                                  using System.Threading.Tasks;
                                  using System.Net.Mime;
                                  using Microsoft.AspNetCore.Builder;
                                  using Microsoft.AspNetCore.Routing;

                                  public sealed class E
                                  {
                                      public void Map(IEndpointRouteBuilder endpoints)
                                      {
                                          endpoints.MapPost("edges", (CreateEdgeRequest req) => Task.CompletedTask);
                                      }
                                  }

                                  public sealed class CreateEdgeRequest { }
                                  """;

            var (_, fixedSource) = await CodeFixTestHarness.ApplySingleFixAsync(source,
                                                                                new MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer(),
                                                                                new MCZ_MAPI_0001_AcceptsMediaTypeAnalyzerCodeFix())
                                                           .ConfigureAwait(false);

            Assert.That.Contains(fixedSource,
                                 ".Accepts<CreateEdgeRequest>(MediaTypeNames.Application.Json)",
                                 because: $"Expected fixed source to add '.Accepts<CreateEdgeRequest>(MediaTypeNames.Application.Json)'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\".Accepts<CreateEdgeRequest>(MediaTypeNames.Application.Json)\"' is missing in 'fixedSource'");
        }

        [TestMethod]
        public async Task NoFix_WhenNonGenericAcceptsAlreadyPresent()
        {
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

            var diagnostics = await CodeFixTestHarness.GetDiagnosticsAsync(source, new MCZ_MAPI_0001_AcceptsRequestBodyMediaTypeAnalyzer())
                                                      .ConfigureAwait(false);

            Assert.That.IsEmpty(diagnostics,
                                because: $"Expected no diagnostics but got: {AssertMessageHelper.FormatDiagnosticIds(diagnostics)}{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }
    }
}
