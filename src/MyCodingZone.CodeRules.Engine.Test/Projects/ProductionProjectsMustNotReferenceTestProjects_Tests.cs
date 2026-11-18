using MyCodingZone.CodeRules.Projects;
using Siemens.AspNet.CodeRules.Contracts;
using Solution.Parser.Sln;

namespace MyCodingZone.CodeRules.Engine.Test.Projects
{
    /// <summary>
    /// PROJECTS_RULE_001 evaluates the solution, not C# code. Each test writes a tiny solution (Shop + Shop.Test) to a temp
    /// folder and hands it to the rule as its SolutionContext.
    /// </summary>
    [TestClass]
    [TestCategory("CodeRules.Tests")]
    [TestCategory("CodeRules.Tests.Projects")]
    public sealed class ProductionProjectsMustNotReferenceTestProjects_Tests
    {
        private const string NoCode = "public sealed class Placeholder;";

        [TestMethod]
        public async Task Should_Report_Productive_Project_When_It_References_A_Test_Project()
        {
            using var solution = FixtureSolution.Create(productionReferencesTests: true);

            var result = await RuleTesterFor(solution).RunAsync(NoCode);

            Assert.That.HasCount(1,
                                 result.Findings,
                                 because: "'Shop' references the test project 'Shop.Test', which ships test code into production",
                                 fix: "Check how PROJECTS_RULE_001 detects test projects (IsTestProject / test packages)");

            Assert.That.AreEqual("Shop → Shop.Test",
                                 result.Findings[0].What,
                                 because: "The finding names the productive project and the referenced test project",
                                 fix: "Check What of PROJECTS_RULE_001 (ProductionProjectsMustNotReferenceTestProjects)");
        }

        [TestMethod]
        public async Task Should_Accept_Productive_Project_When_It_References_No_Test_Project()
        {
            using var solution = FixtureSolution.Create(productionReferencesTests: false);

            await Assert.That.CodeRule.CompliesAsync(RuleTesterFor(solution), NoCode);
        }

        private static RuleTester<ProductionProjectsMustNotReferenceTestProjects> RuleTesterFor(FixtureSolution solution)
        {
            var solutionContext = new SolutionContext(SolutionFile.Parse(solution.Path));
            var ruleTester = new RuleTester<ProductionProjectsMustNotReferenceTestProjects>(options => options.AddContext(solutionContext));

            return ruleTester;
        }

        /// <summary>Shop.slnx with a productive project 'Shop' and a test project 'Shop.Test', deleted on dispose.</summary>
        private sealed class FixtureSolution : IDisposable
        {
            private readonly DirectoryInfo _root;

            private FixtureSolution(DirectoryInfo root)
            {
                _root = root;
            }

            public string Path => System.IO.Path.Combine(_root.FullName, "Shop.slnx");

            public static FixtureSolution Create(bool productionReferencesTests)
            {
                var root = Directory.CreateTempSubdirectory("coderule-fixture-");
                var reference = productionReferencesTests ? """<ItemGroup><ProjectReference Include="..\Shop.Test\Shop.Test.csproj" /></ItemGroup>""" : "";

                Write(root, "Shop.slnx", """
                                         <Solution>
                                           <Project Path="Shop/Shop.csproj" />
                                           <Project Path="Shop.Test/Shop.Test.csproj" />
                                         </Solution>
                                         """);
                Write(root, "Shop/Shop.csproj", $"""
                                                 <Project Sdk="Microsoft.NET.Sdk">
                                                   <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
                                                   {reference}
                                                 </Project>
                                                 """);
                Write(root, "Shop.Test/Shop.Test.csproj", """
                                                          <Project Sdk="Microsoft.NET.Sdk">
                                                            <PropertyGroup>
                                                              <TargetFramework>net10.0</TargetFramework>
                                                              <IsTestProject>true</IsTestProject>
                                                            </PropertyGroup>
                                                            <ItemGroup>
                                                              <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.12.0" />
                                                            </ItemGroup>
                                                          </Project>
                                                          """);

                var fixture = new FixtureSolution(root);

                return fixture;
            }

            public void Dispose()
            {
                _root.Delete(recursive: true);
            }

            private static void Write(DirectoryInfo root,
                                      string relativePath,
                                      string content)
            {
                var file = new FileInfo(System.IO.Path.Combine(root.FullName, relativePath));
                file.Directory!.Create();
                File.WriteAllText(file.FullName, content);
            }
        }
    }
}
