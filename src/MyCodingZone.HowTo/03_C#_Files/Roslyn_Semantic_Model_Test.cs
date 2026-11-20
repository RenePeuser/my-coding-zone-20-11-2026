using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyCodingZone.HowTo
{
    // Syntax = what is written. Symbols = what it means.
    // For symbols Roslyn needs a compilation: syntax trees + references to the used assemblies.
    [TestClass]
    [TestCategory("HowTo")]
    public class Roslyn_Semantic_Model_Test
    {
        private const string PersonCode = """
                                          public record Person
                                          {
                                              public required int Age { get; init; }
                                              public required string City { get; set; }
                                              public string? Nickname { get; init; }
                                          }
                                          """;

        private const string ServiceCode = """
                                           using System.Threading.Tasks;
                                           using MyTask = System.Threading.Tasks.Task;

                                           public class Service
                                           {
                                               public MyTask RunAsync() => Task.CompletedTask;

                                               public async Task UseAsync()
                                               {
                                                   var result = await LoadAsync();
                                               }

                                               private Task<int> LoadAsync() => Task.FromResult(42);
                                           }
                                           """;

        private static readonly IReadOnlyList<MetadataReference> References = LoadReferences();

        [TestMethod]
        public void How_To_Create_A_Compilation_Without_Errors()
        {
            var compilation = CreateCompilation(PersonCode, ServiceCode);

            // Parse errors and compile errors are different things. Missing references show up only here (CS0246, CS0518 ...).
            var errors = compilation.GetDiagnostics()
                                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                                    .Select(d => d.ToString())
                                    .ToList();

            Assert.That.IsEmpty(errors,
                                because: $"Without a valid compilation symbols are incomplete (error types){Environment.NewLine}{string.Join(Environment.NewLine, errors)}",
                                fix: "Add the missing MetadataReference, e.g. from TRUSTED_PLATFORM_ASSEMBLIES");
        }

        [TestMethod]
        public void How_To_Read_Property_Symbols()
        {
            var compilation = CreateCompilation(PersonCode);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            // Syntax node -> symbol: GetDeclaredSymbol for declarations.
            var properties = tree.GetRoot()
                                 .DescendantNodes()
                                 .OfType<PropertyDeclarationSyntax>()
                                 .Select(p => model.GetDeclaredSymbol(p))
                                 .OfType<IPropertySymbol>()
                                 .ToDictionary(p => p.Name);

            var age = properties["Age"];
            var city = properties["City"];
            var nickname = properties["Nickname"];

            // No string compare with "init" any more, the symbol knows it.
            Assert.That.IsTrue(age.SetMethod?.IsInitOnly ?? false,
                               because: "Age is declared with { get; init; }",
                               fix: "Use IPropertySymbol.SetMethod.IsInitOnly to detect init accessors");

            Assert.That.IsFalse(city.SetMethod?.IsInitOnly ?? true,
                                because: "City is declared with { get; set; }",
                                fix: "A set accessor is not init-only");

            Assert.That.IsTrue(age.IsRequired,
                               because: "Age has the 'required' modifier",
                               fix: "Use IPropertySymbol.IsRequired instead of checking modifiers");

            // The type, resolved by the compiler: 'int' is System.Int32.
            Assert.That.AreEqual(SpecialType.System_Int32, age.Type.SpecialType,
                                 because: "The keyword 'int' is an alias for System.Int32",
                                 fix: "Compare types via symbols, not via the written text");

            // Nullable reference types are only known with symbols (and NullableContextOptions.Enable).
            Assert.That.AreEqual(NullableAnnotation.NotAnnotated, city.NullableAnnotation,
                                 because: "City is declared as 'string'",
                                 fix: "Enable nullable in the CSharpCompilationOptions");

            Assert.That.AreEqual(NullableAnnotation.Annotated, nickname.NullableAnnotation,
                                 because: "Nickname is declared as 'string?'",
                                 fix: "Enable nullable in the CSharpCompilationOptions");

            Assert.That.IsTrue(age.ContainingType.IsRecord,
                               because: "Person is a record",
                               fix: "Use INamedTypeSymbol.IsRecord instead of searching for EqualityContract");
        }

        [TestMethod]
        public void How_To_See_Through_Aliases_With_Symbols()
        {
            var compilation = CreateCompilation(ServiceCode);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var runAsync = tree.GetRoot()
                               .DescendantNodes()
                               .OfType<MethodDeclarationSyntax>()
                               .Single(m => m.Identifier.Text == "RunAsync");

            // Syntax only: the written text. A rule "returns Task" would miss this method.
            Assert.That.AreEqual("MyTask", runAsync.ReturnType.ToString(),
                                 because: "The syntax tree contains exactly what is written, the alias",
                                 fix: "Use the semantic model when the meaning matters, not the text");

            // With symbols: the real type behind the alias.
            var returnType = model.GetTypeInfo(runAsync.ReturnType).Type;

            Assert.That.AreEqual("System.Threading.Tasks.Task", returnType?.ToDisplayString(),
                                 because: "The semantic model resolves the alias MyTask to System.Threading.Tasks.Task",
                                 fix: "Use model.GetTypeInfo(syntax).Type to get the real type");
        }

        [TestMethod]
        public void How_To_Resolve_Var_With_Symbols()
        {
            var compilation = CreateCompilation(ServiceCode);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var declaration = tree.GetRoot()
                                  .DescendantNodes()
                                  .OfType<VariableDeclarationSyntax>()
                                  .Single();

            // Syntax only: 'var', the type is unknown.
            Assert.That.IsTrue(declaration.Type.IsVar,
                               because: "The variable is declared with 'var'",
                               fix: "The syntax tree cannot know the inferred type");

            // With symbols: the compiler inferred int from 'await LoadAsync()'.
            var type = model.GetTypeInfo(declaration.Type).Type;

            Assert.That.AreEqual(SpecialType.System_Int32, type?.SpecialType,
                                 because: "await Task<int> results in int",
                                 fix: "Use model.GetTypeInfo on the 'var' type syntax");

            // And from the call: which method is invoked?
            var invocation = declaration.DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var method = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            Assert.That.AreEqual("System.Threading.Tasks.Task<int>", method?.ReturnType.ToDisplayString(),
                                 because: "LoadAsync returns Task<int>",
                                 fix: "Use model.GetSymbolInfo(invocation).Symbol to get the called method");
        }

        private static CSharpCompilation CreateCompilation(params string[] sources)
        {
            var trees = sources.Select(source => CSharpSyntaxTree.ParseText(source));

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                       nullableContextOptions: NullableContextOptions.Enable);

            return CSharpCompilation.Create("HowTo.Semantic", trees, References,
                                            options);
        }

        private static List<PortableExecutableReference> LoadReferences()
        {
            // All assemblies of the running .NET (System.Runtime, System.Threading.Tasks, ...).
            // For a real project the references come from the project (MSBuildWorkspace or the build output).
            var trustedAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;

            return trustedAssemblies.Split(Path.PathSeparator)
                                    .Select(path => MetadataReference.CreateFromFile(path))
                                    .ToList();
        }
    }
}
