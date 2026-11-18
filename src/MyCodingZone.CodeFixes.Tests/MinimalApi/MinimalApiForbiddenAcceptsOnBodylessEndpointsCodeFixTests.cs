using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.CodeFixes.Tests.TestHarness;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.CodeFixes.Tests.MinimalApi
{
    [TestClass]
    [TestCategory("MinimalApi")]
    [TestCategory("Roslyn CodeFixes")]
    public sealed class MinimalApiForbiddenAcceptsOnBodylessEndpointsCodeFixTests
    {
        [TestMethod]
        public async Task Fix_RemovesAccepts_ForMapGet()
        {
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

            var (_, fixedSource) = await CodeFixTestHarness.ApplySingleFixAsync(source,
                                                                                new MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzer(),
                                                                                new MCZ_MAPI_0002_ForbiddenAcceptsOnBodylessEndpointsAnalyzerCodeFix())
                                                           .ConfigureAwait(false);

            Assert.That.DoesNotContain(".Accepts<",
                                       fixedSource,
                                       because: $"Expected code fix to remove '.Accepts<...>' from the endpoint configuration.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                       fix: "Check why '\".Accepts<\"' contains 'fixedSource'");
        }
    }
}