using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.CodeFixes.Test.TestHarness;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.CodeFixes.Test.MinimalApi
{
    [TestClass]
    [TestCategory("MinimalApi")]
    [TestCategory("Roslyn CodeFixes")]
    public sealed class MinimalApiProducesResponseDtoCodeFixTests
    {
        [TestMethod]
        public async Task Fix_AddsProducesDto()
        {
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

            var (_, fixedSource) = await CodeFixTestHarness.ApplySingleFixAsync(source,
                                                                                new MCZ_MAPI_0003_ProducesResponseDtoAnalyzer(),
                                                                                new MCZ_MAPI_0003_ProducesResponseDtoAnalyzerCodeFix())
                                                           .ConfigureAwait(false);

            Assert.That.Contains(fixedSource,
                                 ".Produces<CreateEdgeResponse>()",
                                 because: $"Expected fixed source to add '.Produces<CreateEdgeResponse>()'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\".Produces<CreateEdgeResponse>()\"' is missing in 'fixedSource'");
        }
    }
}