using MyCodingZone.CodeRules.Records;

namespace MyCodingZone.CodeRules.Test.Records
{
    [TestClass]
    [TestCategory("CodeRules.Tests")]
    [TestCategory("CodeRules.Tests.Records")]
    public sealed class RecordPropertiesMustBeImmutable_Tests
    {
        [TestMethod]
        public async Task Should_Report_Record_Property_When_It_Has_A_Setter()
        {
            await Assert.That.CodeRule.ViolatesAsync<RecordPropertiesMustBeImmutable>("""
                                                                                      public record Order(int Id)
                                                                                      {
                                                                                          [|public string Status { get; set; } = "";|]
                                                                                          [|public int Count { get; set; }|]
                                                                                      }
                                                                                      """);
        }

        [TestMethod]
        [DataRow("public string Status { get; init; } = \"\";")]
        [DataRow("public string Status { get; } = \"\";")]
        [DataRow("public required string Status { get; init; }")]
        public async Task Should_Accept_Record_Property_That_Cannot_Change(string property)
        {
            await Assert.That.CodeRule.CompliesAsync<RecordPropertiesMustBeImmutable>($$"""
                                                                                        public record Order(int Id)
                                                                                        {
                                                                                            {{property}}
                                                                                        }
                                                                                        """);
        }

        [TestMethod]
        public async Task Should_Suggest_An_Init_Accessor()
        {
            var finding = await Assert.That.CodeRule.ViolatesOnceAsync<RecordPropertiesMustBeImmutable>("""
                                                                                                        public record Order(int Id)
                                                                                                        {
                                                                                                            [|public string Status { get; set; } = "";|]
                                                                                                        }
                                                                                                        """);

            Assert.That.AreEqual("public string Status { get; init; } = \"\";",
                                 finding.ShouldBe,
                                 because: "The finding shows the property with 'init' instead of 'set', so the fix can be applied as is",
                                 fix: "Check ShouldBe of RECORDS_RULE_001 (RecordPropertiesMustBeImmutable)");
        }
    }
}
