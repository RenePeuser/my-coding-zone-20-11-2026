using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Tests.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.Analyzer.Tests.Inline.MCZ_INLINE_00004_InlineReturnObjectCreation
{
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Inline")]
    [TestClass]
    public sealed class Iterator_YieldReturnNewObjectCreation_NoDiagnostic
    {
        [TestMethod]
        public async Task NoDiagnostic()
        {
            //language=csharp
            const string source = """
                                  using System.Collections.Generic;

                                  public sealed class C
                                  {
                                      public IEnumerable<Foo> M()
                                      {
                                          yield return new Foo();
                                      }
                                  }

                                  public sealed class Foo
                                  {
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_INLINE_0004_InlineReturnObjectCreationAnalyzer()).ConfigureAwait(false);

            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }
    }
}