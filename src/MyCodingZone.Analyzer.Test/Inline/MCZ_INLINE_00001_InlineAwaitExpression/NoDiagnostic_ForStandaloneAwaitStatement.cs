using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Test.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.Analyzer.Test.Inline.MCZ_INLINE_00001_InlineAwaitExpression
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Inline")]
    public sealed class NoDiagnostic_ForStandaloneAwaitStatement
    {
        [TestMethod]
        public async Task NoDiagnostic_ForStandaloneAwaitStatementAsync()
        {
            //language=csharp
            const string source = """
                                  using System.Threading.Tasks;

                                  public sealed class C
                                  {
                                      public async Task M()
                                      {
                                          await FooAsync();
                                      }

                                      private static Task FooAsync() => Task.CompletedTask;
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }
    }
}