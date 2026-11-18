using MyCodingZone.CodeRules.Services;

namespace MyCodingZone.CodeRules.Test.Services
{
    [TestClass]
    [TestCategory("CodeRules.Tests")]
    [TestCategory("CodeRules.Tests.Services")]
    public sealed class ServicesInjectAtMostFiveDependencies_Tests
    {
        [TestMethod]
        [DataRow("public sealed class OrderService(IA a, IB b, IC c, ID d, IE e, IF f) : IOrderService { public void Place() { } }")]
        [DataRow("public sealed class OrderService : IOrderService { public OrderService(IA a, IB b, IC c, ID d, IE e, IF f) { } public void Place() { } }")]
        public async Task Should_Report_Service_When_It_Injects_More_Than_Five_Dependencies(string service)
        {
            var finding = await Assert.That.CodeRule.ViolatesOnceAsync<ServicesInjectAtMostFiveDependencies>(service);

            Assert.That.AreEqual("6 (max 5)",
                                 finding.Details.Single(detail => detail.Key == "Dependencies").Value?.ToString(),
                                 because: "The finding names how many dependencies the service injects and the allowed maximum",
                                 fix: "Check the Details of SERVICES_RULE_006 (ServicesInjectAtMostFiveDependencies)");
        }

        [TestMethod]
        [DataRow("public sealed class OrderService(IA a, IB b, IC c, ID d, IE e) : IOrderService { public void Place() { } }")]
        [DataRow("public sealed class OrderService : IOrderService { public void Place() { } }")]
        [DataRow("public sealed record OrderSnapshot(int A, int B, int C, int D, int E, int F);")]
        public async Task Should_Accept_Class_When_It_Injects_At_Most_Five_Dependencies_Or_Is_No_Service(string code)
        {
            await Assert.That.CodeRule.CompliesAsync<ServicesInjectAtMostFiveDependencies>(code);
        }
    }
}
