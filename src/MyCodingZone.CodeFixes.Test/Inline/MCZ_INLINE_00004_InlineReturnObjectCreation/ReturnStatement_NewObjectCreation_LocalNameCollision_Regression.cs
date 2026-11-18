using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.CodeFixes.Test.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes.Test.Inline.MCZ_INLINE_00004_InlineReturnObjectCreation
{
    [TestCategory("RoslynCodeFix")]
    [TestCategory("Inline")]
    [TestClass]
    public sealed class ReturnStatement_NewObjectCreation_LocalNameCollision_Regression
    {
        [TestMethod]
        public async Task Fix_DocumentCurrentNameCollisionBehavior()
        {
            const string source = """
                                  public sealed class C
                                  {
                                      public Foo M()
                                      {
                                          var foo = new Foo();
                                          return new Foo();
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

            // Current (buggy) behavior: the fix introduces a duplicate local name `foo`.
            // This scenario test protects the specifics of that bug so it can be corrected safely later.
            Assert.That.Contains(fixedSource,
                                 "var foo = new Foo();",
                                 because: $"Expected fixed source to contain the duplicated local declaration 'var foo = new Foo();'.{Environment.NewLine}{Environment.NewLine}Actual fixed source:{Environment.NewLine}{fixedSource}",
                                 fix: "Check why '\"var foo = new Foo();\"' is missing in 'fixedSource'");
        }
    }
}