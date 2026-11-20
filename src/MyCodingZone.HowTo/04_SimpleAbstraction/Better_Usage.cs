using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyCodingZone.HowTo
{
    // The same record as real type (for reflection) and as source code (for Roslyn, see Code below).
    public record Employee
    {
        public required int Age { get; init; }

        public required string City { get; init; }

        public string? Nickname { get; init; }
    }

    [TestClass]
    [TestCategory("HowTo")]
    public class Simple_Abstraction
    {
        private const string Code = """
                                    public record Person
                                    {
                                        public required int Age { get; init; }
                                        public required string City { get; set; }
                                    }
                                    """;

        private const string EmployeeCode = """
                                            public record Employee
                                            {
                                                public required int Age { get; init; }

                                                public required string City { get; init; }

                                                public string? Nickname { get; init; }
                                            }

                                            public record Address
                                            {
                                                public required string Street { get; set; }
                                            }
                                            """;

        private static MczRecord _record = null!;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Let us try a wrapper around the native parser :)
            _record = MczSyntaxTreeParser.Parse(Code);
        }

        [TestMethod]
        public void Age_Should_Be_Readonly()
        {
            var age = _record.Properties.First(p => p.Name == nameof(Person.Age));

            Assert.That.IsTrue(age.IsReadonly,
                               because: "Properties with 'init' accessor should be detected as readonly to enforce immutability patterns",
                               fix: "Check the MczSyntaxTreeParser.ParseProperty logic - it should identify 'init' accessors as readonly");
        }

        [TestMethod]
        public void City_Should_Not_Be_Readonly()
        {
            var city = _record.Properties.First(p => p.Name == "City");

            Assert.That.IsFalse(city.IsReadonly,
                                because: "Properties with 'set' accessor should be detected as mutable to allow runtime modifications",
                                fix: "Check the MczSyntaxTreeParser.ParseProperty logic - it should identify 'set' accessors as not readonly");
        }

        [TestMethod]
        public void How_To_Use_The_Richer_Model_For_A_Rule()
        {
            // The rule only talks about records and properties, no SyntaxNode, no PropertyInfo.
            var records = MczSyntaxTreeParser.ParseRecords(EmployeeCode, path: "Employee.cs");

            var findings = FindMutableProperties(records);

            Assert.That.AreEqual([
                                     "What: 'Address.Street' is mutable, Where: Employee.cs(12,28)"
                                 ],
                                 findings,
                                 because: "Only Address.Street has a set accessor, the finding names record, property and file position",
                                 fix: "Check MczSyntaxTreeParser: Name, IsReadonly and Location of every MczProperty");

            var employee = records.Single(r => r.Name == nameof(Employee));

            Assert.That.AreEqual([
                                     "Age",
                                     "City"
                                 ],
                                 employee.Properties.Where(p => p.IsRequired).Select(p => p.Name),
                                 because: "Only Age and City have the 'required' modifier",
                                 fix: "Check how MczSyntaxTreeParser detects the 'required' modifier");
        }

        [TestMethod]
        public void How_To_Fill_The_Same_Model_From_Roslyn_And_Reflection()
        {
            // Two sources, one model, one rule. The rule does not care where the data comes from.
            var fromRoslyn = MczSyntaxTreeParser.ParseRecords(EmployeeCode, path: "Employee.cs")
                                                .Single(r => r.Name == nameof(Employee));

            var fromReflection = MczReflectionParser.Parse(typeof(Employee));

            Assert.That.AreEqual(Describe(fromRoslyn), Describe(fromReflection),
                                 because: "Both parsers must produce the same model for the same record",
                                 fix: "Align MczSyntaxTreeParser and MczReflectionParser (readonly = init or no setter, required = 'required')");

            Assert.That.AreEqual(FindMutableProperties([fromRoslyn]).Count, FindMutableProperties([fromReflection]).Count,
                                 because: "The same rule finds the same violations, no matter the source",
                                 fix: "Check that both parsers set IsReadonly the same way");

            // The difference: reflection has no source, so there is no 'Where'.
            Assert.That.IsTrue(fromReflection.Properties.TrueForAll(p => p.Location is null),
                               because: "A compiled assembly does not know file and line of a property",
                               fix: "Use Roslyn when findings need a file position");

            Assert.That.IsTrue(fromRoslyn.Properties.TrueForAll(p => p.Location is not null),
                               because: "Roslyn knows the exact position of every property",
                               fix: "Set MczProperty.Location in MczSyntaxTreeParser");
        }

        // The rule: written once against the abstraction.
        private static ImmutableList<string> FindMutableProperties(IEnumerable<MczRecord> records)
        {
            return records.SelectMany(record => record.Properties
                                                      .Where(p => !p.IsReadonly)
                                                      .Select(p => $"What: '{record.Name}.{p.Name}' is mutable, Where: {p.Location ?? record.Name}"))
                          .ToImmutableList();
        }

        private static string Describe(MczRecord record)
        {
            // Location left out on purpose, it differs by source.
            var properties = record.Properties
                                   .OrderBy(p => p.Name, StringComparer.Ordinal)
                                   .Select(p => $"{p.Name}(readonly: {p.IsReadonly}, required: {p.IsRequired})");

            return $"{record.Name}: {string.Join(", ", properties)}";
        }
    }

    // MyCodingZoneSyntaxTreeParser :) 
    // Simplifies the Roslyn API to a model of records and properties, with readonly and required flags.
    public static class MczSyntaxTreeParser
    {
        public static MczRecord Parse(string syntax)
        {
            return ParseRecords(syntax).First();
        }

        public static ImmutableList<MczRecord> ParseRecords(string syntax,
                                                            string path = "")
        {
            var tree = CSharpSyntaxTree.ParseText(syntax, path: path);
            var root = tree.GetRoot();

            return root.DescendantNodes()
                       .OfType<RecordDeclarationSyntax>()
                       .Select(ParseRecord)
                       .ToImmutableList();
        }

        private static MczRecord ParseRecord(RecordDeclarationSyntax syntax)
        {
            var props = syntax.Members
                              .OfType<PropertyDeclarationSyntax>()
                              .Select(ParseProperty)
                              .ToImmutableList();

            var record = new MczRecord
            {
                Name = syntax.Identifier.Text,
                Properties = props
            };

            return record;
        }

        private static MczProperty ParseProperty(PropertyDeclarationSyntax syntax)
        {
            var name = syntax.Identifier.Text;
            var isReadonly = syntax.AccessorList?.Accessors.All(a => !a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false;
            var isRequired = syntax.Modifiers.Any(SyntaxKind.RequiredKeyword);

            var lineSpan = syntax.Identifier.GetLocation().GetLineSpan();
            var location = $"{lineSpan.Path}({lineSpan.StartLinePosition.Line + 1},{lineSpan.StartLinePosition.Character + 1})";

            var property = new MczProperty
            {
                Name = name,
                IsReadonly = isReadonly,
                IsRequired = isRequired,
                Location = location
            };

            return property;
        }
    }

    // Parser - dasselbe Modell aus einem kompilierten Typ
    public static class MczReflectionParser
    {
        public static MczRecord Parse(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                            .Select(ParseProperty)
                            .ToImmutableList();

            var record = new MczRecord
            {
                Name = type.Name,
                Properties = props
            };

            return record;
        }

        private static MczProperty ParseProperty(PropertyInfo property)
        {
            // init is a set method with the required custom modifier IsExternalInit.
            var setMethod = property.SetMethod;

            var isReadonly = setMethod is null ||
                             setMethod.ReturnParameter
                                      .GetRequiredCustomModifiers()
                                      .Contains(typeof(IsExternalInit));

            var result = new MczProperty
            {
                Name = property.Name,
                IsReadonly = isReadonly,
                IsRequired = property.IsDefined(typeof(RequiredMemberAttribute)),
                Location = null
            };

            return result;
        }
    }

    // Daten-Typ - reine Daten, keine Parsing-Logik
    public record MczRecord
    {
        public required string Name { get; init; }

        public required ImmutableList<MczProperty> Properties { get; init; }
    }

    // Daten-Typ - reine Daten
    public record MczProperty
    {
        public required string Name { get; init; }

        public required bool IsReadonly { get; init; }

        public required bool IsRequired { get; init; }

        // File and position, e.g. "Employee.cs(5,28)". Null when the source is unknown (reflection).
        public required string? Location { get; init; }
    }
}
