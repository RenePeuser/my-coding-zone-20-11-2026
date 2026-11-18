using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Tests.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.Analyzer.Tests.Inline.MCZ_INLINE_00001_InlineAwaitExpression
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Inline")]
    public sealed class NoDiagnostic_ForAwaitInLocalVariableInitializer
    {
        [TestMethod]
        public async Task NoDiagnostic_ForAwaitInLocalVariableInitializerAsync()
        {
            //language=csharp
            const string source = """
                                  using System.Threading.Tasks;

                                  public sealed class C
                                  {
                                      public async Task<int> M()
                                      {
                                          var x = await FooAsync();
                                          return x;
                                      }

                                      private static Task<int> FooAsync() => Task.FromResult(1);
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_INLINE_0001_InlineAwaitExpressionAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }
    }
}