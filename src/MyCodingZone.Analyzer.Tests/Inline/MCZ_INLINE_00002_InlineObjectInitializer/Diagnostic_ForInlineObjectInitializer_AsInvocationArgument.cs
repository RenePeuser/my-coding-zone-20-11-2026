using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.Analyzer.Tests.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.Analyzer.Tests.Inline.MCZ_INLINE_00002_InlineObjectInitializer
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Inline")]
    public sealed class Diagnostic_ForInlineObjectInitializer_AsInvocationArgument
    {
        [TestMethod]
        public async Task Diagnostic_ForInlineObjectInitializer_AsInvocationArgumentAsync()
        {
            //language=csharp
            const string source = """
                                  using System;

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

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_INLINE_0002_InlineObjectInitializerAnalyzer()).ConfigureAwait(false);
            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");
            Assert.That.AreEqual(MCZ_INLINE_0002_InlineObjectInitializerAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_INLINE_0002_InlineObjectInitializerAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_INLINE_0002_InlineObjectInitializerAnalyzer.DiagnosticId' and check what produced the different value");
        }
    }
}