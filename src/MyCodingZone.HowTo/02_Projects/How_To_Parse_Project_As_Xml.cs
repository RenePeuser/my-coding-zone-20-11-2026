using System.Xml.Linq;
using AspNetCore.Simple.MsTest.Sdk;

namespace MyCodingZone.HowTo
{
    [TestClass]
    [TestCategory("HowTo")]
    public class Parse_Project_As_Xml_Test
    {
        private static readonly string ProjectPath = RepositoryPaths.HowToProject;

        [TestMethod]
        public void How_To_Load_A_Project_File()
        {
            var doc = XDocument.Load(ProjectPath);

            Assert.That.IsNotNull(doc,
                                  because: "XDocument.Load must return a valid XML document",
                                  fix: "Check if project file exists and is valid XML");

            Assert.That.IsNotNull(doc.Root,
                                  because: "Project file must have a root element",
                                  fix: "Ensure project file has <Project> root element");

            Assert.That.AreEqual("Project", doc.Root.Name.LocalName,
                                 because: "Root element must be 'Project'",
                                 fix: "Fix the root element name in the .csproj file");
        }


        // Achtung: Hier gibt es einen Fallstrick :) 
        [TestMethod]
        public void How_To_Find_PackageReferences()
        {
            var doc = XDocument.Load(ProjectPath);

            Assert.That.IsNotNull(doc.Root,
                                  because: "Project must have root element",
                                  fix: "Check project file structure");

            var packages = doc.Root
                              .Descendants("PackageReference")
                              .ToList();

            Assert.That.IsNotEmpty(packages,
                                   because: "Project should have at least one PackageReference",
                                   fix: "Add package references to the project");

            var firstPackage = packages[0];
            var packageName = firstPackage.Attribute("Include")?.Value;

            Assert.That.IsNotNull(packageName,
                                  because: "PackageReference must have Include attribute",
                                  fix: "Add Include attribute with package name");

            Assert.That.IsNotEmpty(packageName,
                                   because: "Package name must not be empty",
                                   fix: "Set a valid package name in Include attribute");
        }

        [TestMethod]
        public void PackageReferences_Should_Not_Have_A_Version()
        {
            // Convention from AGENTS.md: NuGet versions live only in Directory.Packages.props.
            // Checked for every project under src/, not just for this one.
            var sourceDirectory = Path.Combine(RepositoryPaths.Root.FullName, "src");

            var findings = Directory.EnumerateFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories)
                                    .SelectMany(path => XDocument.Load(path, LoadOptions.SetLineInfo)
                                                                 .Descendants("PackageReference")
                                                                 .Where(reference => reference.Attribute("Version") is not null ||
                                                                                     reference.Element("Version") is not null)
                                                                 .Select(reference => Describe(path, reference)))
                                    .ToList();

            Assert.That.IsEmpty(findings,
                                because: $"Versions are managed centrally, a version in the project file overrides it and the projects drift apart{Environment.NewLine}{string.Join(Environment.NewLine, findings)}",
                                fix: "Remove the Version from the PackageReference and add or update <PackageVersion Include=\"...\" Version=\"...\" /> in Directory.Packages.props");
        }

        private static string Describe(string path, XElement reference)
        {
            // LoadOptions.SetLineInfo gives every XElement a line number, that is the 'Where' of the finding.
            var line = ((System.Xml.IXmlLineInfo)reference).LineNumber;
            var relativePath = Path.GetRelativePath(RepositoryPaths.Root.FullName, path);

            return $"What: PackageReference '{reference.Attribute("Include")?.Value}' has a Version, Where: {relativePath}({line})";
        }
    }
}
