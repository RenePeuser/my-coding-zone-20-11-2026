using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.CodeFixes.Tests.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes.Tests.Inline.MCZ_INLINE_00004_InlineReturnObjectCreation
{
    [TestCategory("RoslynCodeFix")]
    [TestCategory("Inline")]
    [TestClass]
    public sealed class ReturnStatement_NewObjectCreation_DiagnosticLocationOnNewExpression_FixStillApplies

    {

        [TestMethod]

        public async Task Fix_AppliesWhenDiagnosticIsOnNewExpression()

        {

            const string source = """

                                  public sealed class C

                                  {

                                      public Foo M()

                                      {

                                          return new Foo();

                                      }

                                  }



                                  public sealed class Foo

                                  {

                                  }

                                  """;



            var analyzer = new MCZ_INLINE_0004_InlineReturnObjectCreationAnalyzer();

            var diagnostics = await CodeFixTestHarness.GetDiagnosticsAsync(source, analyzer).ConfigureAwait(false);



            Assert.That.HasCount(1, diagnostics,
                                 because: $"Expected exactly 1 diagnostic.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Check which items of 'diagnostics' are missing or unexpected");

            Assert.That.AreEqual(MCZ_INLINE_0004_InlineReturnObjectCreationAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_INLINE_0004_InlineReturnObjectCreationAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_INLINE_0004_InlineReturnObjectCreationAnalyzer.DiagnosticId' and check what produced the different value");



            // Sanity: diagnostic is reported on the object-creation expression, not the entire return statement.

            Assert.That.IsGreaterThan(diagnostics[0].Location.SourceSpan.Start, 0,
                                      because: "'diagnostics[0].Location.SourceSpan.Start' must be greater than '0'",
                                      fix: "Check how 'diagnostics[0].Location.SourceSpan.Start' is calculated");

        }

    }

}