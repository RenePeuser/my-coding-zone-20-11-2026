using Extensions.Pack;

namespace MyCodingZone.CodeRules.Test
{
    [TestCategory("CodeRules")]
    [TestCategory("CodeRules Records")]
    [TestClass]
    public class RECORDS_RULE_001 : CodeRuleTestBase
    {
        [TestMethod]
        public void Record_Properties_Have_To_Be_Immutable()
        {
            var findings = from record in Records
                           from property in record.Properties
                           where property.IsReadOnly.IsFalse()
                           select new CodeRuleFinding
                           {
                               What = property.FullyQualifiedName,
                               Where = property.Location,
                               Current = property.SourceCode,
                               ShouldBe = property.SourceCode.Replace("set;", "init;"),
                               Details = [("Property", $"{property.Type} {property.Name}")]
                           };

            Assert.That.CodeRuleHasNoFindings(findings,
                                              id: "RECORDS_RULE_001",
                                              title: "Record properties must be immutable",
                                              why: "A record is a snapshot of a state, e.g. data loaded from the database. It must not change while it is being processed. A setter lets any code modify the same instance, so other parts that hold a reference suddenly see different data. A change must produce a new instance instead.",
                                              howToFix: "Replace 'set;' with 'init;' and create a modified copy with a 'with' expression, e.g. 'order with { Status = OrderStatus.Shipped }'.",
                                              readMoreUrl: "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record#init-only-properties");
        }
    }
}
