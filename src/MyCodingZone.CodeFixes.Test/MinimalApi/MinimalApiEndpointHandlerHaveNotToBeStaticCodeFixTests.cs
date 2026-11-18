using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer;
using MyCodingZone.CodeFixes.Test.TestHarness;

namespace MyCodingZone.CodeFixes.Test.MinimalApi
{
    [TestClass]
    [TestCategory("MinimalApi")]
    [TestCategory("Roslyn CodeFixes")]
    public sealed class MinimalApiEndpointHandlerHaveNotToBeStaticCodeFixTests
    {
        [TestMethod]
        public async Task Fix_RemovesStaticModifier_FromLocalFunctionHandler()
        {
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

            var (_, fixedSource) = await CodeFixTestHarness.ApplySingleFixAsync(source,
                                                                                new MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzer(),
                                                                                new MCZ_MAPI_0015_EndpointHandlerHaveNotToBeStaticAnalyzerCodeFix())
                                                           .ConfigureAwait(false);

            Assert.That.IsFalse(fixedSource.Contains("static Task HandleAsync", StringComparison.Ordinal),
                                because: $"Expected fixed source to remove 'static' from local function handler.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                fix: "Check why 'fixedSource.Contains(\"static Task HandleAsync\", StringComparison.Ordinal)' is true");
        }
    }
}