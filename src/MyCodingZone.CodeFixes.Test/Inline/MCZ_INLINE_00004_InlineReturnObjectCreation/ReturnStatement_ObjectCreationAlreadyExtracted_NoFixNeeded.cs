using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.CodeFixes.Test.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes.Test.Inline.MCZ_INLINE_00004_InlineReturnObjectCreation
{
    [TestCategory("RoslynCodeFix")]
    [TestCategory("Inline")]
    [TestClass]
    public sealed class ReturnStatement_ObjectCreationAlreadyExtracted_NoFixNeeded
    {
        [TestMethod]
        public async Task NoDiagnostic_NoFixNeeded()
        {
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

            var diagnostics = await CodeFixTestHarness.GetDiagnosticsAsync(source, new MCZ_INLINE_0004_InlineReturnObjectCreationAnalyzer()).ConfigureAwait(false);

            Assert.That.IsEmpty(diagnostics,
                                because: $"Expected no diagnostics but got: {AssertMessageHelper.FormatDiagnosticIds(diagnostics)}{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                fix: "Check which unexpected items 'diagnostics' contains");
        }
    }
}