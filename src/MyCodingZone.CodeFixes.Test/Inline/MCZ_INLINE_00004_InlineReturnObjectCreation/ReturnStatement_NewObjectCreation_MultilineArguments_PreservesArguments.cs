using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.CodeFixes.Test.TestHarness;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes.Test.Inline.MCZ_INLINE_00004_InlineReturnObjectCreation
{
    [TestCategory("RoslynCodeFix")]
    [TestCategory("Inline")]
    [TestClass]
    public sealed class ReturnStatement_NewObjectCreation_MultilineArguments_PreservesArguments
    {
        [TestMethod]
        public async Task Fix_PreservesArguments()
        {
            const string source = """
                                  public interface IHttpCallHandler
                                  {
                                  }

                                  public sealed class HttpCallHandler(HttpClient client, int a, int b, int c) : IHttpCallHandler
                                  {
                                  }

                                  public sealed class C
                                  {
                                      private readonly int _a = 1;
                                      private readonly int _b = 2;
                                      private readonly int _c = 3;

                                      public IHttpCallHandler CreateFrom(HttpClient client)
                                      {
                                          return new HttpCallHandler(client, _a, _b,
                                                                     _c);
                                      }
                                  }
                                  """;

            const string expected = """
                                    public interface IHttpCallHandler
                                    {
                                    }

                                    public sealed class HttpCallHandler(HttpClient client, int a, int b, int c) : IHttpCallHandler
                                    {
                                    }

                                    public sealed class C
                                    {
                                        private readonly int _a = 1;
                                        private readonly int _b = 2;
                                        private readonly int _c = 3;

                                        public IHttpCallHandler CreateFrom(HttpClient client)
                                        {
                                            var httpCallHandler = new HttpCallHandler(client, _a, _b,
                                                _c);
                                            return httpCallHandler;
                                        }
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