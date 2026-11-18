using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Tests.TestHarness;

namespace MyCodingZone.Analyzer.Tests.Inline.MCZ_INLINE_00003_InlineReturnCollectionExpression
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Inline")]
    public sealed class NoDiagnostic_ForCollectionExpressionInVariableInitializer
    {
        [TestMethod]
        public async Task NoDiagnostic_ForCollectionExpressionInVariableInitializerAsync()
        {
            //language=csharp
            const string source = """
                                  public sealed class C
                                  {
                                      public int[] M()
                                      {
                                          var x = [1, 2, 3];
                                          return x;
                                      }
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_INLINE_0003_InlineReturnCollectionExpressionAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }
    }
}