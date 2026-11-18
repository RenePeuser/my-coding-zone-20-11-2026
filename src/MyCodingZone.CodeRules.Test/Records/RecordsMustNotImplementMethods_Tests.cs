using MyCodingZone.CodeRules.Records;

namespace MyCodingZone.CodeRules.Test.Records
{
    [TestClass]
    [TestCategory("CodeRules.Tests")]
    [TestCategory("CodeRules.Tests.Records")]
    public sealed class RecordsMustNotImplementMethods_Tests
    {
        [TestMethod]
        public async Task Should_Report_Method_When_A_Record_Implements_Logic()
        {
            await Assert.That.CodeRule.ViolatesAsync<RecordsMustNotImplementMethods>("""
                                                                                     public record Order(int Id, decimal Net)
                                                                                     {
                                                                                         [|public decimal Gross() => Net * 1.19m;|]
                                                                                     }
                                                                                     """);
        }

        [TestMethod]
        [DataRow("public record Order(int Id, decimal Net);")]
        [DataRow("public record Order { public required int Id { get; init; } }")]
        [DataRow("public sealed class OrderCalculator { public decimal Gross(decimal net) => net * 1.19m; }")]
        public async Task Should_Accept_Record_With_Data_Only_And_Logic_Outside(string code)
        {
            await Assert.That.CodeRule.CompliesAsync<RecordsMustNotImplementMethods>(code);
        }

        [TestMethod]
        public async Task Should_Name_Record_And_Method_In_Details()
        {
            var finding = await Assert.That.CodeRule.ViolatesOnceAsync<RecordsMustNotImplementMethods>("""
                                                                                                       public record Order(int Id, decimal Net)
                                                                                                       {
                                                                                                           [|public decimal Gross() => Net * 1.19m;|]
                                                                                                       }
                                                                                                       """);

            Assert.That.AreEqual("Order",
                                 finding.Details.Single(detail => detail.Key == "Record").Value?.ToString(),
                                 because: "The finding names the record that implements the method",
                                 fix: "Check the Details of RECORDS_RULE_002 (RecordsMustNotImplementMethods)");
        }
    }
}
