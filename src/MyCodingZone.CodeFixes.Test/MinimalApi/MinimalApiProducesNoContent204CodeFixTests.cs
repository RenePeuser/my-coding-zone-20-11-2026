using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.CodeFixes.Test.TestHarness;
using MyCodingZone.Analyzer.MinimalApi;

namespace MyCodingZone.CodeFixes.Test.MinimalApi
{
    [TestClass]
    [TestCategory("MinimalApi")]
    [TestCategory("Roslyn CodeFixes")]
    public sealed class MinimalApiProducesNoContent204CodeFixTests
    {
        [TestMethod]
        public async Task Fix_AddsProduces204_ForTaskIResultReturningNoContent()
        {
            const string source = """
                                  using System.Threading.Tasks;
                                  using Microsoft.AspNetCore.Builder;
                                  using Microsoft.AspNetCore.Routing;

                                  public sealed class E
                                  {
                                      public void Map(IEndpointRouteBuilder endpoints)
                                      {
                                          endpoints.MapDelete("parameters", HandleAsync);

                                          static Task HandleAsync() => Task.CompletedTask;
                                      }
                                  }
                                  """;

            var (_, fixedSource) = await CodeFixTestHarness.ApplySingleFixAsync(source,
                                                                                new MCZ_MAPI_0004_ProducesNoContent204Analyzer(),
                                                                                new MCZ_MAPI_0004_ProducesNoContent204AnalyzerCodeFix())
                                                           .ConfigureAwait(false);

            Assert.That.Contains(fixedSource,
                                 ".Produces(StatusCodes.Status204NoContent)",
                                 because: $"Expected fixed source to add '.Produces(StatusCodes.Status204NoContent)'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\".Produces(StatusCodes.Status204NoContent)\"' is missing in 'fixedSource'");
        }
    }
}