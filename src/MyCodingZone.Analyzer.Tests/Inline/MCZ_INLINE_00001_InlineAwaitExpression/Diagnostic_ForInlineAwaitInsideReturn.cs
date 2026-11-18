using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Tests.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.Analyzer.Tests.Inline.MCZ_INLINE_00001_InlineAwaitExpression
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Inline")]
    public sealed class Diagnostic_ForInlineAwaitInsideReturn
    {
        [TestMethod]
        public async Task Diagnostic_ForInlineAwaitInsideReturnAsync()
        {
            //language=csharp
            const string source = """
                                  using System.Threading.Tasks;

                                  public sealed class C
                                  {
                                      public Task<int> M()
                                      {
                                          return BarAsync(await FooAsync());
                                      }

                                      private static Task<int> FooAsync() => Task.FromResult(1);
                                      private static Task<int> BarAsync(int x) => Task.FromResult(x);
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer()).ConfigureAwait(false);
            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.AreEqual(MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer.DiagnosticId' and check what produced the different value");
        }
    }
}