namespace MyCodingZone.CodeRules.Test
{
    [TestCategory("CodeRules")]
    [TestCategory("CodeRules Services")]
    [TestClass]
    public class SERVICES_RULE_005 : CodeRuleTestBase
    {
        [TestMethod]
        public void Service_Registrations_Have_To_Return_Void()
        {
            var findings = from @class in Classes
                           where @class.IsStatic
                           from method in @class.Methods
                           where method.IsStatic &&
                                 method.Name.StartsWith("Add", StringComparison.Ordinal) &&
                                 method.IsServiceRegistration() &&
                                 method.ReturnParameter != "void"
                           select new CodeRuleFinding
                           {
                               What = $"{@class.FullyQualifiedName}.{method.Name}",
                               Where = method.Location,
                               Current = method.SourceCode,
                               HowToFix = $"Change the return type of '{method.Name}' from '{method.ReturnParameter}' to 'void' and remove the 'return'.",
                               Details = [("Returns", method.ReturnParameter)]
                           };

            Assert.That.CodeRuleHasNoFindings(findings,
                                              id: "SERVICES_RULE_005",
                                              title: "Service registrations return void",
                                              why: "One style for all registrations: a registration is a statement, not a builder. " +
                                                   "Returning IServiceCollection invites long fluent chains in which a missing or duplicate registration is hard to spot in a review.",
                                              howToFix: "Return 'void' from the registration and call each registration as its own statement.");
        }
    }
}
