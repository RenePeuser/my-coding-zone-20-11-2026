using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.CodeFixes;
using MyCodingZone.CodeFixes.Test.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes.Test.Inline.MCZ_INLINE_00001_InlineAwaitExpression
{
    [TestClass]
    [TestCategory("RoslynCodeFix")]
    [TestCategory("Inline")]
    public sealed class Fix_IntroducesLocalVariableForAwaitExpression_WhenOuterCallIsAwaited
    {
        [TestMethod]
        public async Task Fix_IntroducesLocalVariableForAwaitExpression_WhenOuterCallIsAwaitedAsync()
        {
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

            var (diagnostics, fixedSource) = await CodeFixTestHarness.ApplySingleFixAsync(source,
                                                                                          new MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer(),
                                                                                          new MCZ_INLINE_0001_InlineAwaitExpressionCodeFix())
                                                                     .ConfigureAwait(false);

            Assert.That.AreEqual(MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer.DiagnosticId' and check what produced the different value");
            Assert.That.Contains(fixedSource,
                                 "result = await",
                                 because: $"Expected fixed source to contain 'result = await'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\"result = await\"' is missing in 'fixedSource'");
            Assert.That.Contains(fixedSource,
                                 "return result",
                                 because: $"Expected fixed source to contain 'return result'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\"return result\"' is missing in 'fixedSource'");
        }
    }
}