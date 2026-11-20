using System.Collections.Immutable;

namespace MyCodingZone.CodeRules.Test
{
    [TestCategory("CodeRules")]
    [TestCategory("CodeRules Projects")]
    [TestClass]
    public class PROJECTS_RULE_001 : CodeRuleTestBase
    {
        [TestMethod]
        public void Productive_Projects_Are_Not_Allowed_To_Reference_Any_Test_Project()
        {
            // ProjectReference.Name is the referenced .csproj file name without extension
            var testProjectNames = Solution.UnitTestProjects
                                           .Select(testProject => testProject.AssemblyName)
                                           .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

            var findings = from project in Solution.ProductionProjects
                           from reference in project.ProjectReferences
                           where testProjectNames.Contains(reference.Name)
                           let projectFile = project.FileInfo
                           select new CodeRuleFinding
                           {
                               What = $"{project.AssemblyName} → {reference.Name}",
                               Where = projectFile,
                               Current = $"<ProjectReference Include=\"{reference.Include}\" />",
                               HowToFix = $"Remove the <ProjectReference> from '{projectFile.Name}'. If the productive code needs something from the test project, move that code into a productive project instead."
                           };


            // Important: Output rendering Human vs AI
            Assert.That.CodeRuleHasNoFindings(findings,
                                              id: "PROJECTS_RULE_001",
                                              title: "Productive projects must not reference test projects",
                                              why: "Test projects are never deployed. A productive project that depends on them ships test code and test packages " +
                                                   "(MSTest, test SDKs, fakes) into production and creates a dependency cycle between production and test code.",
                                              howToFix: "Remove the <ProjectReference> to the test project.");
        }
    }
}
