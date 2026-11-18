using MyCodingZone.CodeRules.Services;

namespace MyCodingZone.CodeRules.Test.Services
{
    [TestClass]
    [TestCategory("CodeRules.Tests")]
    [TestCategory("CodeRules.Tests.Services")]
    public sealed class InjectMultipleImplementationsAsIEnumerable_Tests
    {
        [TestMethod]
        [DataRow("List<IHandler> handlers")]
        [DataRow("IReadOnlyList<IHandler> handlers")]
        [DataRow("ImmutableArray<IHandler> handlers")]
        [DataRow("IHandler[] handlers")]
        public async Task Should_Report_Constructor_Parameter_When_It_Is_No_IEnumerable(string parameter)
        {
            await Assert.That.CodeRule.ViolatesAsync<InjectMultipleImplementationsAsIEnumerable>($$"""
                                                                                                   public interface IHandler { }

                                                                                                   public sealed class Dispatcher([|{{parameter}}|]) : IDispatcher
                                                                                                   {
                                                                                                       public void Dispatch() { }
                                                                                                   }
                                                                                                   """);
        }

        [TestMethod]
        [DataRow("IEnumerable<IHandler> handlers")]
        [DataRow("IHandler handler")]
        [DataRow("ILogger<Dispatcher> logger")]
        public async Task Should_Accept_Constructor_Parameter_When_DI_Can_Resolve_It(string parameter)
        {
            await Assert.That.CodeRule.CompliesAsync<InjectMultipleImplementationsAsIEnumerable>($$"""
                                                                                                   public interface IHandler { }

                                                                                                   public sealed class Dispatcher({{parameter}}) : IDispatcher
                                                                                                   {
                                                                                                       public void Dispatch() { }
                                                                                                   }
                                                                                                   """);
        }

        [TestMethod]
        public async Task Should_Suggest_IEnumerable_Of_The_Element_Type()
        {
            var finding = await Assert.That.CodeRule.ViolatesOnceAsync<InjectMultipleImplementationsAsIEnumerable>("""
                                                                                                                   public interface IHandler { }

                                                                                                                   public sealed class Dispatcher([|List<IHandler> handlers|]) : IDispatcher
                                                                                                                   {
                                                                                                                       public void Dispatch() { }
                                                                                                                   }
                                                                                                                   """);

            Assert.That.AreEqual("IEnumerable<IHandler> handlers",
                                 finding.ShouldBe,
                                 because: "The finding replaces the collection type with IEnumerable<T> and keeps the parameter name",
                                 fix: "Check ShouldBe of SERVICES_RULE_003 (InjectMultipleImplementationsAsIEnumerable)");
        }
    }
}
