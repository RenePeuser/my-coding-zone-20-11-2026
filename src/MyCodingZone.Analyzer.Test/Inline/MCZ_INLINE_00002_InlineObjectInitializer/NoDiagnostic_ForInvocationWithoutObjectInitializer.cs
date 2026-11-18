using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Test.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.Analyzer.Test.Inline.MCZ_INLINE_00002_InlineObjectInitializer
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Inline")]
    public sealed class NoDiagnostic_ForInvocationWithoutObjectInitializer
    {
        [TestMethod]
        public async Task NoDiagnostic_ForInvocationWithoutObjectInitializerAsync()
        {
            //language=csharp
            const string source = """
                                  using System;

                                  public sealed class C
                                  {
                                      public void M()
                                      {
                                          Foo(new Request());
                                      }

                                      private static void Foo(Request r) { }
                                  }

                                  public sealed class Request
                                  {
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_INLINE_0002_InlineObjectInitializerAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }
    }
}