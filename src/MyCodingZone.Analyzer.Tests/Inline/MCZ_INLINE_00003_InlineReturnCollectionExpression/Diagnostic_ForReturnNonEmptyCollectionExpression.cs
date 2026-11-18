using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Tests.TestHarness;

namespace MyCodingZone.Analyzer.Tests.Inline.MCZ_INLINE_00003_InlineReturnCollectionExpression
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Inline")]
    public sealed class Diagnostic_ForReturnNonEmptyCollectionExpression
    {
        [TestMethod]
        public async Task Diagnostic_ForReturnNonEmptyCollectionExpressionAsync()
        {
            //language=csharp
            const string source = """
                                  using System.Collections.Generic;

                                  public sealed class C
                                  {
                                      public int[] M()
                                      {
                                          return [1, 2, 3];
                                      }
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_INLINE_0003_InlineReturnCollectionExpressionAnalyzer()).ConfigureAwait(false);
            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.AreEqual(MCZ_INLINE_0003_InlineReturnCollectionExpressionAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_INLINE_0003_InlineReturnCollectionExpressionAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_INLINE_0003_InlineReturnCollectionExpressionAnalyzer.DiagnosticId' and check what produced the different value");
        }
    }
}