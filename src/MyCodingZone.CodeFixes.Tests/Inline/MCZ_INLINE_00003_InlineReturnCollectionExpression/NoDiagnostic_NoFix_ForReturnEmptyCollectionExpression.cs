using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer;
using MyCodingZone.CodeFixes.Tests.TestHarness;

namespace MyCodingZone.CodeFixes.Tests.Inline.MCZ_INLINE_00003_InlineReturnCollectionExpression
{
    [TestClass]
    [TestCategory("RoslynCodeFix")]
    [TestCategory("Inline")]
    public sealed class NoDiagnostic_NoFix_ForReturnEmptyCollectionExpression
    {
        [TestMethod]
        public async Task NoDiagnostic_NoFix_ForReturnEmptyCollectionExpressionAsync()
        {
            const string source = """
                                  public sealed class C
                                  {
                                      public int[] M()
                                      {
                                          return [];
                                      }
                                  }
                                  """;

            var diagnostics = await CodeFixTestHarness.GetDiagnosticsAsync(source, new MCZ_INLINE_0003_InlineReturnCollectionExpressionAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: $"Expected no diagnostics but got: {AssertMessageHelper.FormatDiagnosticIds(diagnostics)}{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }
    }
}