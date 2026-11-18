using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.CodeFixes.Test.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes.Test.Inline.MCZ_INLINE_00002_InlineObjectInitializer
{
    [TestClass]
    [TestCategory("RoslynCodeFix")]
    [TestCategory("Inline")]
    public sealed class Fix_ExtractsWebApplicationFactoryClientOptionsIntoLocalVariable
    {
        [TestMethod]
        public async Task Fix_ExtractsWebApplicationFactoryClientOptionsIntoLocalVariableAsync()
        {
            const string source = """
                                  using Microsoft.AspNetCore.Mvc.Testing;

                                  public sealed class C
                                  {
                                      private readonly WebApplicationFactory<object> _factory = null!;

                                      public void M()
                                      {
                                          var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
                                          {
                                              AllowAutoRedirect = false
                                          });
                                      }
                                  }
                                  """;

            var (diagnostics, fixedSource) = await CodeFixTestHarness.ApplySingleFixAsync(source,
                                                                                          new MCZ_INLINE_0002_InlineObjectInitializerAnalyzer(),
                                                                                          new MCZ_INLINE_0002_InlineObjectInitializerCodeFix())
                                                                     .ConfigureAwait(false);

            Assert.That.AreEqual(MCZ_INLINE_0002_InlineObjectInitializerAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_INLINE_0002_InlineObjectInitializerAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_INLINE_0002_InlineObjectInitializerAnalyzer.DiagnosticId' and check what produced the different value");
            Assert.That.Contains(fixedSource,
                                 "WebApplicationFactoryClientOptions",
                                 because: $"Expected fixed source to contain 'WebApplicationFactoryClientOptions'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\"WebApplicationFactoryClientOptions\"' is missing in 'fixedSource'");
            Assert.That.Contains(fixedSource,
                                 "CreateClient(webApplicationFactoryClientOptions)",
                                 because: $"Expected fixed source to contain 'CreateClient(webApplicationFactoryClientOptions)'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\"CreateClient(webApplicationFactoryClientOptions)\"' is missing in 'fixedSource'");
            CodeFixTestHarness.AssertContainsLocalDeclaration(fixedSource, "webApplicationFactoryClientOptions");
        }
    }
}