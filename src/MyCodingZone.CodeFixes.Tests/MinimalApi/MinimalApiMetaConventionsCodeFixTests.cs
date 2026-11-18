using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer;
using MyCodingZone.CodeFixes.Tests.TestHarness;

namespace MyCodingZone.CodeFixes.Tests.MinimalApi
{
    [TestClass]
    [TestCategory("MinimalApi")]
    [TestCategory("Roslyn CodeFixes")]
    public sealed class MinimalApiMetaConventionsCodeFixTests
    {
        [TestMethod]
        public async Task Fix_AddsWithTags()
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

                                          static Task HandleAsync() => Task.CompletedTask;
                                      }
                                  }
                                  """;

            var (_, fixedSource) = await CodeFixTestHarness.ApplySingleFixAsync(source,
                                                                                new MCZ_MAPI_0010_WithTagsAnalyzer(),
                                                                                new MCZ_MAPI_0010_WithTagsAnalyzerCodeFix())
                                                           .ConfigureAwait(false);

            Assert.That.Contains(fixedSource,
                                 ".WithTags(\"TODO\")",
                                 because: $"Expected fixed source to add '.WithTags(\"TODO\")'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\".WithTags(\\\"TODO\\\")\"' is missing in 'fixedSource'");
        }
    }
}