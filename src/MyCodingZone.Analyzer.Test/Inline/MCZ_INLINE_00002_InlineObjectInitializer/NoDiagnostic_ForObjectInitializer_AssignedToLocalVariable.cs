using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Test.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.Analyzer.Test.Inline.MCZ_INLINE_00002_InlineObjectInitializer
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Inline")]
    public sealed class NoDiagnostic_ForObjectInitializer_AssignedToLocalVariable
    {
        [TestMethod]
        public async Task NoDiagnostic_ForObjectInitializer_AssignedToLocalVariableAsync()
        {
            //language=csharp
            const string source = """
                                  public sealed class C
                                  {
                                      public void M()
                                      {
                                          var request = new Request { X = 1 };
                                          Foo(request);
                                      }

                                      private static void Foo(Request r) { }
                                  }

                                  public sealed class Request
                                  {
                                      public int X { get; set; }
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_INLINE_0002_InlineObjectInitializerAnalyzer()).ConfigureAwait(false);
            Assert.That.IsEmpty(diagnostics,
                                because: "'diagnostics' must not contain any item",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }
    }
}