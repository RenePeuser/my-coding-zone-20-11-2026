using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Tests.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.Analyzer.Tests.Inline.MCZ_INLINE_00001_InlineAwaitExpression
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Inline")]
    public sealed class Diagnostic_ForInlineAwaitInArgumentList_WithConfigureAwaitOnOuterCall
    {
        [TestMethod]
        public async Task Diagnostic_ForInlineAwaitInArgumentList_WithConfigureAwaitOnOuterCallAsync()
        {
            //language=csharp
            const string source = """
                                  using System.Threading.Tasks;

                                  public sealed class C
                                  {
                                      public async Task<int> M()
                                      {
                                          return await BarAsync(await FooAsync()).ConfigureAwait(false);
                                      }

                                      private static Task<int> FooAsync() => Task.FromResult(1);
                                      private static Task<int> BarAsync(int x) => Task.FromResult(x);
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer()).ConfigureAwait(false);

            Assert.That.HasCount(2, diagnostics,
                                 because: $"Expected 2 diagnostics.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.IsTrue(diagnostics.All(d => d.Id == MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer.DiagnosticId),
                               because: $"Expected all diagnostics to have id '{MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer.DiagnosticId}'. Actual ids: {AssertMessageHelper.FormatDiagnosticIds(diagnostics)}{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                               fix: "Check why 'diagnostics.All(d => d.Id == MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer.DiagnosticId)' is false");
        }
    }
}