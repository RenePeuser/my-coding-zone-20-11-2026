using System.Collections.Immutable;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyCodingZone.HowTo
{
    // We use this type so show the Syntax Tree Visualizer in Visual Studio,
    // which is a great tool to explore the structure of C# code.
    public record PersonForSyntaxTreeVisualizer
    {
        // Property Declaration
        public required int Age { get; init; }

        public required string City { get; init; }
    }

    [TestClass]
    [TestCategory("HowTo")]
    public class Better_Usage
    {
        private const string Code = """
                                    public record Person
                                    {
                                        public required int Age { get; init; }
                                        public required string City { get; set; }
                                    }
                                    """;

        private static SyntaxNode _root = null!;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var tree = CSharpSyntaxTree.ParseText(Code);
            _root = tree.GetRoot();

            var nodes = _root.DescendantNodes().ToImmutableList();
        }

        [TestMethod]
        public void How_To_Check_That_Age_Should_Be_Readonly()
        {
            var ageProp = _root.DescendantNodes()
                               .OfType<PropertyDeclarationSyntax>()
                               .FirstOrDefault(p => p.Identifier.Text == nameof(Person.Age));

            Assert.That.IsNotNull(ageProp,
                                  because: "Source code must contain Age property",
                                  fix: "Add Age property to the Person record in the source code");

            Assert.That.IsNotNull(ageProp.AccessorList,
                                  because: "Age property must have accessor list ({ get; init; })",
                                  fix: "Add accessor list to Age property");

            // SyntaxKind instead of comparing strings: typo-safe and the compiler knows every kind.
            var hasSet = ageProp.AccessorList.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
            var hasInit = ageProp.AccessorList.Accessors.Any(a => a.IsKind(SyntaxKind.InitAccessorDeclaration));

            Assert.That.IsTrue(hasInit,
                               because: "Age property is defined with { get; init; }",
                               fix: "Change Age to { get; init; } in the source code");

            Assert.That.IsFalse(hasSet,
                                because: "Age property is defined with { get; init; } accessor, making it readonly",
                                fix: "Change Age to { get; set; } in the source code if it needs to be mutable");
        }

        [TestMethod]
        public void How_To_Check_That_City_Should_Not_Be_Readonly()
        {
            var cityProp = _root.DescendantNodes()
                                .OfType<PropertyDeclarationSyntax>()
                                .FirstOrDefault(p => p.Identifier.Text == "City");

            Assert.That.IsNotNull(cityProp,
                                  because: "Source code must contain City property",
                                  fix: "Add City property to the Person record in the source code");

            Assert.That.IsNotNull(cityProp.AccessorList,
                                  because: "City property must have accessor list ({ get; set; })",
                                  fix: "Add accessor list to City property");

            // IsReadOnly = hat KEINEN 'set' accessor (init zählt nicht als set)
            var hasSet = cityProp.AccessorList.Accessors
                                 .Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));

            Assert.That.IsTrue(hasSet,
                               because: "City property is defined with { get; set; } accessor, making it mutable",
                               fix: "Change City to { get; init; } in the source code to make it readonly");
        }

        [TestMethod]
        public void How_To_Check_Modifiers_Like_Required()
        {
            var properties = _root.DescendantNodes()
                                  .OfType<PropertyDeclarationSyntax>()
                                  .ToList();

            // Modifiers is a SyntaxTokenList, Any(SyntaxKind) checks for a keyword without string compare.
            var notRequired = properties.Where(p => !p.Modifiers.Any(SyntaxKind.RequiredKeyword))
                                        .Select(p => p.Identifier.Text)
                                        .ToList();

            Assert.That.IsEmpty(notRequired,
                                because: "All properties of Person are declared as 'required'",
                                fix: "Add the 'required' modifier to the property");

            var publicProperties = properties.Count(p => p.Modifiers.Any(SyntaxKind.PublicKeyword));

            Assert.That.AreEqual(2, publicProperties,
                                 because: "Age and City are both public",
                                 fix: "Check the modifiers of the properties in the source code");
        }

        [TestMethod]
        public void How_To_Get_File_And_Line_Of_A_Finding()
        {
            // The path is only metadata of the tree, here a virtual file name.
            // For real files: CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)
            var tree = CSharpSyntaxTree.ParseText(Code, path: "Person.cs");

            var ageProp = tree.GetRoot()
                              .DescendantNodes()
                              .OfType<PropertyDeclarationSyntax>()
                              .First(p => p.Identifier.Text == nameof(Person.Age));

            // Location of the identifier, not of the whole declaration: the finding points exactly at 'Age'.
            var lineSpan = ageProp.Identifier.GetLocation().GetLineSpan();

            // Roslyn counts lines and columns zero-based, IDEs and compiler messages one-based.
            var line = lineSpan.StartLinePosition.Line + 1;
            var column = lineSpan.StartLinePosition.Character + 1;
            var where = $"{lineSpan.Path}({line},{column})";

            Assert.That.AreEqual("Person.cs(3,25)", where,
                                 because: "The finding names the exact position in the compiler format file(line,column), IDEs make it clickable",
                                 fix: "Add 1 to Line and Character of the LinePosition, they are zero-based");

            // Exactly the source at the location, the 'Current' of a finding.
            Assert.That.AreEqual("public required int Age { get; init; }", ageProp.ToString(),
                                 because: "ToString() returns the source without leading and trailing trivia",
                                 fix: "Use ToFullString() to include whitespace and comments");
        }

        [TestMethod]
        public void How_To_Parse_Broken_Code()
        {
            // Roslyn is error tolerant: broken code still produces a tree, plus diagnostics.
            var tree = CSharpSyntaxTree.ParseText("public record Person { public int Age { get; init; }");

            var errors = tree.GetDiagnostics()
                             .Where(d => d.Severity == DiagnosticSeverity.Error)
                             .Select(d => d.Id)
                             .ToList();

            Assert.That.Contains(errors, "CS1513",
                                 because: "The closing brace is missing (CS1513: } expected)",
                                 fix: "Check tree.GetDiagnostics() before trusting the tree of unknown code");

            var age = tree.GetRoot()
                          .DescendantNodes()
                          .OfType<PropertyDeclarationSyntax>()
                          .SingleOrDefault();

            Assert.That.IsNotNull(age,
                                  because: "The property is still part of the tree, even if the record is incomplete",
                                  fix: "Roslyn always returns a tree, missing tokens are added as IsMissing");
        }
    }
}
