using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Test.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.Analyzer.Test.Inline.MCZ_INLINE_00004_InlineReturnObjectCreation
{
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Inline")]
    [TestClass]
    public sealed class ReturnStatement_ObjectCreationAssignedToLocalThenReturned_NoDiagnostic
    {
        [TestMethod]
        public async Task NoDiagnostic()
        {
            //language=csharp
            const string source = """
                                  public sealed class C
                                  {
                                      public Foo M()
                                      {
                                          var foo = new Foo();
                                          return foo;
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