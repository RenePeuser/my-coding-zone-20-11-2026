using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.CodeFixes.Test.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes.Test.Inline.MCZ_INLINE_00002_InlineObjectInitializer
{
    [TestClass]
    [TestCategory("RoslynCodeFix")]
    [TestCategory("Inline")]
    public sealed class Fix_ExtractsObjectInitializerIntoLocalVariable
    {
        [TestMethod]
        public async Task Fix_ExtractsObjectInitializerIntoLocalVariableAsync()
        {
            const string source = """
                                  public sealed class C
                                  {
                                      public void M()
                                      {
                                          Foo(new Request { X = 1 });
                                      }

                                      private static void Foo(Request r) { }
                                  }

                                  public sealed class Request
                                  {
                                      public int X { get; set; }
                                  }
                                  """;

            var (diagnostics, fixedSource) = await CodeFixTestHarness.ApplySingleFixAsync(source,
                                                                                          new MCZ_INLINE_0002_InlineObjectInitializerAnalyzer(),
                                                                                          new MCZ_INLINE_0002_InlineObjectInitializerCodeFix())
                                                                     .ConfigureAwait(false);

            Assert.That.AreEqual(MCZ_INLINE_0002_InlineObjectInitializerAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_INLINE_0002_InlineObjectInitializerAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_INLINE_0002_InlineObjectInitializerAnalyzer.DiagnosticId' and check what produced the different value");
            Assert.That.Contains(fixedSource,
                                 "request = new Request",
                                 because: $"Expected fixed source to contain 'request = new Request'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\"request = new Request\"' is missing in 'fixedSource'");
            Assert.That.Contains(fixedSource,
                                 "Foo(request)",
                                 because: $"Expected fixed source to contain 'Foo(request)'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\"Foo(request)\"' is missing in 'fixedSource'");
            CodeFixTestHarness.AssertContainsLocalDeclaration(fixedSource, "request");
        }
    }
}