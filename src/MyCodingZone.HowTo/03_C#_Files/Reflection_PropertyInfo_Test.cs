using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using AspNetCore.Simple.MsTest.Sdk;
using Extensions.Pack;

namespace MyCodingZone.HowTo
{
    public record Person
    {
        public required int Age { get; init; }
    }

    [TestClass]
    [TestCategory("HowTo")]
    public class Reflection_PropertyInfo_Test
    {
        [TestMethod]
        public void How_To_Check_That_Age_Should_Be_Readonly()
        {
            var ageProperty = typeof(Person).GetProperty(nameof(Person.Age));

            Assert.That.IsNotNull(ageProperty,
                                  because: "Person record must have an Age property",
                                  fix: "Add Age property to Person record");

            // Prüfe: Hat die Property einen public set accessor?
            // (init zählt NICHT als set accessor)
            var isReadOnly = ageProperty.SetMethod.IsNull() ||
                             ageProperty.SetMethod.ReturnParameter
                                        .GetRequiredCustomModifiers()
                                        .Any(m => m.Name == "IsExternalInit");

            Assert.That.IsTrue(isReadOnly,
                               because: "Age property has init-only accessor, which is readonly",
                               fix: "Change Age to { get; init; } if it needs to be immutable");
        }

        [TestMethod]
        public void How_To_Load_All_Types_Of_An_Assembly()
        {
            // Every type knows its assembly, no need to know the file name.
            var assembly = typeof(Person).Assembly;

            // GetTypes():         everything, including private, nested and compiler-generated types (<>c, <Main>$ ...).
            // GetExportedTypes(): only public types visible outside the assembly.
            var allTypes = assembly.GetTypes();
            var exportedTypes = assembly.GetExportedTypes();

            Assert.That.IsGreaterThan(allTypes.Length, exportedTypes.Length,
                                      because: "GetTypes also returns internal and compiler-generated types, e.g. RepositoryPaths",
                                      fix: "Use GetExportedTypes for the public surface, GetTypes for everything");

            Assert.That.DoesNotContain(exportedTypes.Select(t => t.Name), "RepositoryPaths",
                                       because: "RepositoryPaths is internal and not part of the public surface",
                                       fix: "Use GetTypes() to see internal types");

            // Typical filter for CodeRules: own, real types, no compiler-generated ones.
            var records = allTypes.Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute)))
                                  .Where(t => t is { IsClass: true, IsAbstract: false })
                                  .Where(IsRecord)
                                  .Select(t => t.Name)
                                  .ToList();

            Assert.That.Contains(records, nameof(Person),
                                 because: "Person is declared as record",
                                 fix: "Check the IsRecord filter: records have a compiler-generated EqualityContract property");

            Assert.That.DoesNotContain(records, nameof(Reflection_PropertyInfo_Test),
                                       because: "A test class is a normal class, not a record",
                                       fix: "Check the IsRecord filter");
        }

        [TestMethod]
        public void How_To_Load_An_Assembly_From_A_File()
        {
            // The path of an assembly on disk, here our own build output.
            // For a real CodeRule this is e.g. the dll of the WebApi in its bin folder.
            var path = typeof(Person).Assembly.Location;

            Assert.That.IsTrue(File.Exists(path),
                               because: "The assembly file must exist on disk",
                               fix: "Build the project before loading its assembly");

            // 1) Assembly.LoadFrom: loads into the default context. An assembly that is already loaded is reused.
            var loadedFrom = Assembly.LoadFrom(path);

            Assert.That.AreSame(typeof(Person).Assembly, loadedFrom,
                                because: "LoadFrom returns the already loaded assembly of the default context",
                                fix: "Use an own AssemblyLoadContext to get a separate copy");

            // 2) Own AssemblyLoadContext: isolated and collectible, can be unloaded after the analysis.
            var context = new AssemblyLoadContext("HowTo isolated", isCollectible: true);

            try
            {
                var isolated = context.LoadFromAssemblyPath(path);
                var isolatedPerson = isolated.GetType(typeof(Person).FullName!);

                Assert.That.IsNotNull(isolatedPerson,
                                      because: "The isolated assembly contains the same types",
                                      fix: "Check the full type name passed to GetType");

                Assert.That.AreEqual(typeof(Person).FullName, isolatedPerson.FullName,
                                     because: "It is the same type name from the same file",
                                     fix: "Check the loaded path");

                // Pitfall: same name, same file, but a different Type. Type identity = name + load context.
                Assert.That.AreNotEqual(typeof(Person), isolatedPerson,
                                        because: "Types from different load contexts are never equal, 'is' and casts fail",
                                        fix: "Compare loaded types by FullName, not by typeof(...)");
            }
            finally
            {
                context.Unload();
            }
        }

        [TestMethod]
        public void Every_TestClass_Should_Have_A_TestCategory()
        {
            // A first CodeRule with reflection: attributes are metadata and visible at runtime.
            var findings = typeof(Person).Assembly
                                         .GetTypes()
                                         .Where(t => t.IsDefined(typeof(TestClassAttribute)))
                                         .Where(t => !t.IsDefined(typeof(TestCategoryAttribute)))
                                         .Select(t => $"What: test class without [TestCategory], Where: {t.FullName}")
                                         .ToList();

            // Note: reflection only knows the type name, not file and line. That is where Roslyn comes in.
            Assert.That.IsEmpty(findings,
                                because: $"Tests are filtered by category in CI, e.g. --filter TestCategory=HowTo{Environment.NewLine}{string.Join(Environment.NewLine, findings)}",
                                fix: "Add [TestCategory(\"HowTo\")] to the test class");
        }

        private static bool IsRecord(Type type)
        {
            // The C# compiler adds a protected 'EqualityContract' property to every record class.
            return type.GetProperty("EqualityContract", BindingFlags.Instance | BindingFlags.NonPublic) is not null;
        }
    }
}
