using MyCodingZone.Analyzer.Test.TestHarness;

namespace MyCodingZone.Analyzer.Test.Async.MCZ_ASYNC_0002_NoTaskWaitAll
{
    [TestClass]
    [TestCategory("RoslynAnalyzer")]
    [TestCategory("Async")]
    public sealed class NoDiagnostic_ForUnrelatedWaitAllMethod
    {
        [TestMethod]
        public async Task NoDiagnostic_ForUnrelatedWaitAllMethodAsync()
        {
            //language=csharp
            const string source = """
                                  public sealed class CustomBarrier
                                  {
                                      public static void WaitAll()
                                      {
                                      }
                                  }

                                  public sealed class Worker
                                  {
                                      public void Run()
                                      {
                                          CustomBarrier.WaitAll();
                                      }
                                  }
                                  """;

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MCZ_ASYNC_0002_NoTaskWaitAllAnalyzer()).ConfigureAwait(false);

            Assert.That.IsEmpty(diagnostics,
                                because: $"A user-defined WaitAll method is not Task.WaitAll{Environment.NewLine}Actual: {AnalyzerTestHarness.Format(diagnostics)}",
                                fix: "Do not report a diagnostic for this case in MCZ_ASYNC_0002_NoTaskWaitAllAnalyzer");
        }
    }
}
