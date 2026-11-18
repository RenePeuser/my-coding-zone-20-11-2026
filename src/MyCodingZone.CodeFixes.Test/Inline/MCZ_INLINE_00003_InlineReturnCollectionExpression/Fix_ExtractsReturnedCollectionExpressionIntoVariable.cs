using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer;
using MyCodingZone.CodeFixes.Test.TestHarness;

namespace MyCodingZone.CodeFixes.Test.Inline.MCZ_INLINE_00003_InlineReturnCollectionExpression
{
    [TestClass]
    [TestCategory("RoslynCodeFix")]
    [TestCategory("Inline")]
    public sealed class Fix_ExtractsReturnedCollectionExpressionIntoVariable
    {
        [TestMethod]
        public async Task Fix_ExtractsReturnedCollectionExpressionIntoVariableAsync()
        {
            const string source = """
                                  public sealed class C
                                  {
                                      public int[] M()
                                      {
                                          return [1, 2, 3];
                                      }
                                  }
                                  """;

            var (diagnostics, fixedSource) = await CodeFixTestHarness.ApplySingleFixAsync(source,
                                                                                          new MCZ_INLINE_0003_InlineReturnCollectionExpressionAnalyzer(),
                                                                                          new MCZ_INLINE_0003_InlineReturnCollectionExpressionCodeFix())
                                                                     .ConfigureAwait(false);

            Assert.That.AreEqual(MCZ_INLINE_0003_InlineReturnCollectionExpressionAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_INLINE_0003_InlineReturnCollectionExpressionAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_INLINE_0003_InlineReturnCollectionExpressionAnalyzer.DiagnosticId' and check what produced the different value");
            Assert.That.Contains(fixedSource,
                                 "result = [1, 2, 3]",
                                 because: $"Expected fixed source to contain 'result = [1, 2, 3]'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\"result = [1, 2, 3]\"' is missing in 'fixedSource'");
            Assert.That.Contains(fixedSource,
                                 "return result",
                                 because: $"Expected fixed source to contain 'return result'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\"return result\"' is missing in 'fixedSource'");
        }
    }
}