namespace MyCodingZone.CodeRules.Test
{
    [TestCategory("CodeRules")]
    [TestCategory("CodeRules Records")]
    [TestClass]
    public class RECORDS_RULE_002 : CodeRuleTestBase
    {
        [TestMethod]
        public void Records_Are_Not_Allowed_To_Implement_Methods()
        {
            var findings = from record in Records
                           from method in record.Methods
                           select new CodeRuleFinding
                           {
                               What = method.FullyQualifiedName,
                               Where = method.Location,
                               Current = method.SourceCode,
                               HowToFix = $"Move the method '{method.Name}' to a separate service class or static helper.",
                               Details = [("Record", record.Name), ("Method", $"{method.ReturnParameter} {method.Name}(...)")]
                           };

            Assert.That.CodeRuleHasNoFindings(findings,
                                              id: "RECORDS_RULE_002",
                                              title: "Records must not implement methods",
                                              why: "Records are designed for data transfer and value objects. " +
                                                   "Mixing data and logic violates the separation of concerns and " +
                                                   "makes the code harder to test and maintain.",
                                              howToFix: "Move methods to a separate service class or static helper class.");
        }
    }
}
