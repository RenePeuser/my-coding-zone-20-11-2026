using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.CodeFixes.Tests.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes.Tests.Inline.MCZ_INLINE_00004_InlineReturnObjectCreation
{
    [TestCategory("RoslynCodeFix")]
    [TestCategory("Inline")]
    [TestClass]
    public sealed class ReturnStatement_NewObjectCreation_StandardFix_ExtractsLocalVariable
    {
        [TestMethod]
        public async Task Fix_ExtractsLocalVariable()
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

            const string expected = """
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

            var (diagnostics, fixedSource) = await CodeFixTestHarness.ApplySingleFixAsync(source,
                                                                                          new MCZ_INLINE_0004_InlineReturnObjectCreationAnalyzer(),
                                                                                          new MCZ_INLINE_0004_InlineReturnObjectCreationCodeFix())
                                                                     .ConfigureAwait(false);

            Assert.That.AreEqual(MCZ_INLINE_0004_InlineReturnObjectCreationAnalyzer.DiagnosticId,
                                 diagnostics[0].Id,
                                 because: $"Expected diagnostic id '{MCZ_INLINE_0004_InlineReturnObjectCreationAnalyzer.DiagnosticId}' but got '{diagnostics[0].Id}'.{Environment.NewLine}Diagnostics:{Environment.NewLine}{AssertMessageHelper.FormatDiagnostics(diagnostics)}",
                                 fix: "Compare 'diagnostics[0].Id' with 'MCZ_INLINE_0004_InlineReturnObjectCreationAnalyzer.DiagnosticId' and check what produced the different value");

            Assert.That.AreEqual(CodeFixTestHarness.NormalizeWhitespaceForComparison(expected),
                                 CodeFixTestHarness.NormalizeWhitespaceForComparison(fixedSource),
                                 because: $"Fixed source did not match expected output.{Environment.NewLine}{Environment.NewLine}Expected:{Environment.NewLine}{expected}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{fixedSource}",
                                 fix: "Compare 'CodeFixTestHarness.NormalizeWhitespaceForComparison(fixedSource)' with 'CodeFixTestHarness.NormalizeWhitespaceForComparison(expected)' and check what produced the different value");
        }
    }
}