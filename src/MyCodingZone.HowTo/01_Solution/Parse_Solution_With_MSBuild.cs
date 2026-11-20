using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;


namespace MyCodingZone.HowTo
{
    [TestClass]
    [TestCategory("HowTo")]
    public class Parse_Solution_With_MSBuild_Test
    {
        private static readonly string SolutionPath = RepositoryPaths.Solution;

        [TestMethod]
        public void How_To_Parse_A_Solution_File()
        {
            var solutionFile = SolutionFile.Parse(SolutionPath);

            Assert.That.IsNotNull(solutionFile,
                                  because: "SolutionFile.Parse must return a valid solution object",
                                  fix: "Check if the solution file exists and is valid");

            Assert.That.IsNotNull(solutionFile.ProjectsInOrder,
                                  because: "Solution must have a ProjectsInOrder collection",
                                  fix: "Ensure solution file is properly formatted");
        }

        [TestMethod]
        public void How_To_List_All_Projects_Of_A_Solution()
        {
            var solutionFile = SolutionFile.Parse(SolutionPath);

            // Filter by ProjectType: only KnownToBeMSBuildFormat entries are real projects (.csproj).
            var projects = solutionFile.ProjectsInOrder
                                       .Where(p => p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                                       .Select(p => new
                                       {
                                           p.ProjectName,
                                           p.RelativePath,
                                           p.AbsolutePath
                                       })
                                       .ToList();

            var folders = solutionFile.ProjectsInOrder
                                      .Where(p => p.ProjectType == SolutionProjectType.SolutionFolder)
                                      .ToList();

            Assert.That.Contains(projects.Select(p => p.ProjectName), "MyCodingZone.HowTo",
                                 because: "The HowTo project is part of the solution",
                                 fix: $"Add src/MyCodingZone.HowTo/MyCodingZone.HowTo.csproj to {RepositoryPaths.Solution}");

            // Pitfall: for a classic .sln the folders show up as 'projects' of type SolutionFolder.
            // For .slnx SolutionFile only reports the real projects, folders and solution items are gone.
            Assert.That.IsEmpty(folders,
                                because: "SolutionFile does not report the folders of a .slnx ('/01 How To/', ...)",
                                fix: "Use Microsoft.VisualStudio.SolutionPersistence for folders and solution items, see How_To_Parse_A_Solution_With_SolutionPersistence");

            Assert.That.IsTrue(projects.TrueForAll(p => File.Exists(p.AbsolutePath)),
                               because: "Every project of the solution must exist on disk",
                               fix: "Remove stale project entries from the solution or restore the missing project files");
        }

        [TestMethod]
        public async Task How_To_Parse_A_Solution_With_SolutionPersistence()
        {
            // Microsoft.VisualStudio.SolutionPersistence is the library Visual Studio and MSBuild use for .sln and .slnx.
            // Unlike SolutionFile it gives the real model: folder hierarchy, solution items, project parents.
            var serializer = SolutionSerializers.GetSerializerByMoniker(SolutionPath);

            Assert.That.IsNotNull(serializer,
                                  because: "There must be a serializer for .sln and .slnx files",
                                  fix: "Use a solution file with the extension .sln or .slnx");

            var solution = await serializer.OpenAsync(SolutionPath, CancellationToken.None).ConfigureAwait(false);

            var howToProject = solution.SolutionProjects
                                       .FirstOrDefault(p => p.FilePath.EndsWith("MyCodingZone.HowTo.csproj", StringComparison.Ordinal));

            Assert.That.IsNotNull(howToProject,
                                  because: "The HowTo project is part of the solution",
                                  fix: $"Add src/MyCodingZone.HowTo/MyCodingZone.HowTo.csproj to {RepositoryPaths.Solution}");

            Assert.That.AreEqual("/01 How To/", howToProject.Parent?.Path,
                                 because: "The HowTo project lives in the solution folder '/01 How To/'",
                                 fix: "Move the project into the folder '/01 How To/' in the solution");

            // Solution items (files without project) are only visible with SolutionPersistence.
            var solutionItems = solution.SolutionFolders
                                        .SelectMany(f => f.Files ?? [])
                                        .ToList();

            Assert.That.Contains(solutionItems, "AGENTS.md",
                                 because: "AGENTS.md is a solution item in the folder '/Docs/'",
                                 fix: "Add AGENTS.md to the folder '/Docs/' of the solution");
        }

        [TestMethod]
        public void Every_Project_Under_Src_Should_Be_Part_Of_The_Solution()
        {
            // First mini CodeRule: file system vs. solution.
            var solutionFile = SolutionFile.Parse(SolutionPath);

            var projectsInSolution = solutionFile.ProjectsInOrder
                                                 .Where(p => p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                                                 .Select(p => Path.GetFullPath(p.AbsolutePath))
                                                 .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var sourceDirectory = Path.Combine(RepositoryPaths.Root.FullName, "src");

            var findings = Directory.EnumerateFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories)
                                    .Select(Path.GetFullPath)
                                    .Where(path => !projectsInSolution.Contains(path))
                                    .Select(path => $"What: project is not part of the solution, Where: {Path.GetRelativePath(RepositoryPaths.Root.FullName, path)}")
                                    .ToList();

            Assert.That.IsEmpty(findings,
                                because: $"A project that is not in the solution is not built, not tested and not checked by the CodeRules{Environment.NewLine}{string.Join(Environment.NewLine, findings)}",
                                fix: "Add the project with 'dotnet sln code-rules.slnx add <project>' or delete it");
        }
    }
}
