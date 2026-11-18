using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Tests.TestHarness;

namespace MyCodingZone.Analyzer.Tests.Inline.MCZ_INLINE_00003_InlineReturnCollectionExpression
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Inline")]
    public sealed class NoDiagnostic_ForReturnEmptyCollectionExpression
    {
        [TestMethod]
        public async Task NoDiagnostic_ForReturnEmptyCollectionExpressionAsync()
        {
            //language=csharp
            const string source = """
                                  using System.Collections.Generic;

                                  public sealed class C
                                  {
                                      public int[] M()
                                      {
                                          return [];
                                      }
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