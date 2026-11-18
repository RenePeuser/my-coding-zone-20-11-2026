using System.Collections.Immutable;

namespace MyCodingZone.CodeRules.Projects
{
    /// <summary>Stage 2 counterpart: src/MyCodingZone.CodeRules.Tests/ProjectReferences/PROJECTS_RULE_001.cs</summary>
    public sealed class ProductionProjectsMustNotReferenceTestProjects : ISolutionRule
    {
        public string Id => "PROJECTS_RULE_001";

        public string Title => "Productive projects must not reference test projects";

        public string Why => "Test projects are never deployed. A productive project that depends on them ships test code and test packages " +
                             "(MSTest, test SDKs, fakes) into production and creates a dependency cycle between production and test code.";

        public string HowToFix => "Remove the <ProjectReference> to the test project.";

        public ImmutableList<string> Tags => ["Architecture"];

        public IEnumerable<Finding> Evaluate(SolutionContext context)
        {
            var solution = context.Solution;

            // ProjectReference.Name is the referenced .csproj file name without extension
            var testProjectNames = solution.UnitTestProjects
                                           .Select(testProject => testProject.AssemblyName)
                                           .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

            return from project in solution.ProductionProjects
                   from reference in project.ProjectReferences
                   where testProjectNames.Contains(reference.Name)
                   select new Finding
                   {
                       What = $"{project.AssemblyName} → {reference.Name}",
                       Where = project.FileInfo,
                       HowToFix = $"Remove the <ProjectReference> from '{project.FileInfo.Name}'. If the productive code needs something from the test project, move that code into a productive project instead.",
                       // Where marks the whole csproj, so the offending line goes into the details instead of Current
                       Details = [("Reference", $"<ProjectReference Include=\"{reference.Include}\" />")]
                   };
        }
    }
}
