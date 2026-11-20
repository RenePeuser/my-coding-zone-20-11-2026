using AspNetCore.Simple.MsTest.Sdk;
using Solution.Parser.CSharp;
using Solution.Parser.Sln;

namespace MyCodingZone.HowTo
{
    // Stage 1 ends here: Solution.Parser is the abstraction of 04_SimpleAbstraction grown up.
    // MSBuild (solution + projects) and Roslyn (syntax + symbols) behind one model: records, properties, invocations, locations.
    //
    // Two modes:
    //   SolutionFile.Parse(path)                        syntax only, fast, exactly what CSharpSyntaxTree.Parse gives
    //   SolutionFile.Parse(path, ParseMode.WithSymbols) plus symbols, one Roslyn compilation per project, built on first use
    [TestClass]
    [TestCategory("HowTo")]
    public class Solution_Parser_Test
    {
        private const string ClientCode = """
                                          using System.Linq;
                                          using System.Threading.Tasks;

                                          public record User
                                          {
                                              public required string Name { get; init; }
                                              public string? Email { get; set; }
                                          }

                                          public class ApiClient
                                          {
                                              public Task PostAsync(string url, bool writeResponse = true) => Task.CompletedTask;
                                          }

                                          public class Usage
                                          {
                                              public async Task RunAsync(ApiClient client)
                                              {
                                                  await client.PostAsync("/users");
                                                  await client.PostAsync("/orders", false);
                                                  await client.PostAsync(writeResponse: true, url: "/items");

                                                  var count = new[] { 1, 2, 3 }.Count();
                                              }
                                          }
                                          """;

        // ------------------------------------------------------------------ syntax only

        [TestMethod]
        public void How_To_Parse_The_Solution_Syntax_Only()
        {
            var solution = SolutionFile.Parse(RepositoryPaths.Solution);

            // The projects are already classified: production vs. unit test projects.
            Assert.That.Contains(solution.ProductionProjects.Select(p => p.Name), "MyCodingZone.WebApi",
                                 because: "The WebApi is a production project",
                                 fix: "Check that the WebApi project is part of the solution");

            Assert.That.Contains(solution.UnitTestProjects.Select(p => p.Name), "MyCodingZone.HowTo",
                                 because: "The HowTo project is a test project",
                                 fix: "Check that the HowTo project references MSTest");

            // project.Trees: the C# files of the project, parsed once and cached.
            var webApi = solution.ProductionProjects.Single(p => p.Name == "MyCodingZone.WebApi");

            var createUserRequest = webApi.Trees
                                          .SelectMany(tree => tree.Records)
                                          .SingleOrDefault(record => record.Name == "CreateUserRequest");

            Assert.That.IsNotNull(createUserRequest,
                                  because: "The WebApi declares the record CreateUserRequest",
                                  fix: "Check src/MyCodingZone.WebApi/Api/User/V1/Create/Requests/CreateUserRequest.cs");

            Assert.That.IsFalse(solution.HasSymbols,
                                because: "SolutionFile.Parse(path) without a mode gives the syntax only",
                                fix: "Use SolutionFile.Parse(path, ParseMode.WithSymbols) for symbols");
        }

        [TestMethod]
        public void How_To_Parse_Code_In_Memory()
        {
            // Code in memory, the shortest way to try a rule out. Same model as for the solution.
            var codeBase = CodeBase.FromSource(ClientCode);

            var user = codeBase.AllTrees
                               .SelectMany(tree => tree.Records)
                               .Single(record => record.Name == "User");

            var name = user.Properties.Single(p => p.Name == "Name");
            var email = user.Properties.Single(p => p.Name == "Email");

            // Everything we built by hand in MczSyntaxTreeParser comes for free.
            Assert.That.IsTrue(name.IsInitOnly && name.IsRequired,
                               because: "Name is declared as 'required string Name { get; init; }'",
                               fix: "Check Property.IsInitOnly and Property.IsRequired");

            Assert.That.IsTrue(email.IsNullable && email.HasSetter,
                               because: "Email is declared as 'string? Email { get; set; }'",
                               fix: "Check Property.IsNullable and Property.HasSetter");

            // Location and SourceCode: the 'Where' and 'Current' of a finding.
            Assert.That.AreEqual("public string? Email { get; set; }", email.SourceCode,
                                 because: "SourceCode is exactly the source of the declaration",
                                 fix: "Use SourceCode as 'Current' of a finding");

            Assert.That.AreEqual(7, email.Location.StartLine,
                                 because: "Email is declared in line 7 of the code, lines are one-based",
                                 fix: "Use Location as 'Where' of a finding");
        }

        [TestMethod]
        public void How_To_Find_Arguments_With_Syntax_Only()
        {
            var codeBase = CodeBase.FromSource(ClientCode);

            var calls = codeBase.AllTrees
                                .SelectMany(tree => tree.Invocations)
                                .Where(call => call.Name == "PostAsync")
                                .ToList();

            Assert.That.HasCount(3, calls,
                                 because: "RunAsync calls PostAsync three times",
                                 fix: "Check the invocations of the code");

            // Syntax only sees what is written. Only the call with 'writeResponse: true' names the parameter,
            // the call that relies on the default value and the positional 'false' are invisible.
            var writtenAsNamedArgument = calls.Count(call => call.Arguments.Any(argument => argument.Name == "writeResponse"));

            Assert.That.AreEqual(1, writtenAsNamedArgument,
                                 because: "Only one call writes 'writeResponse:' explicitly",
                                 fix: "Use symbols (call.Passes / call.ArgumentFor) to see default and positional arguments");
        }

        [TestMethod]
        public void How_To_Fail_Fast_When_Symbols_Are_Missing()
        {
            var call = CodeBase.FromSource(ClientCode)
                               .AllTrees
                               .SelectMany(tree => tree.Invocations)
                               .First(c => c.Name == "PostAsync");

            // A symbol access without symbols throws instead of guessing. The cost of a rule stays visible.
            Assert.That.ThrowsExactly<InvalidOperationException>(() => _ = call.Method,
                                                                 because: "The code base was opened without symbols",
                                                                 fix: "Open the code with CodeBase.FromSource(code).WithSymbols() or SolutionFile.Parse(path, ParseMode.WithSymbols)");
        }

        // ------------------------------------------------------------------ with symbols

        [TestMethod]
        public void How_To_Resolve_Arguments_With_Symbols()
        {
            // .WithSymbols(): same trees, plus a Roslyn compilation on first symbol access.
            var codeBase = CodeBase.FromSource(ClientCode).WithSymbols();

            var calls = codeBase.AllTrees
                                .SelectMany(tree => tree.Invocations)
                                .Where(call => call.Name == "PostAsync")
                                .ToList();

            // The rule: "find every call that writes the response", no matter how it is written.
            var findings = calls.Where(call => call.Passes("writeResponse", true))
                                .Select(call => $"What: {call.SourceCode}, Where: line {call.Location.StartLine}")
                                .ToList();

            Assert.That.AreEqual(["What: client.PostAsync(\"/users\"), Where: line 19",
                                  "What: client.PostAsync(writeResponse: true, url: \"/items\"), Where: line 21"],
                                 findings,
                                 because: "Passes() asks the compiler: positional, named and left out (default = true) all count",
                                 fix: "Use call.Passes(parameter, value) instead of searching the written arguments");

            // BoundArguments: every parameter of the called method with what the call passes for it.
            var firstCall = calls[0];
            var writeResponse = firstCall.BoundArguments.Single(b => b.Parameter.Name == "writeResponse");

            Assert.That.IsFalse(writeResponse.IsExplicit,
                                because: "The first call leaves 'writeResponse' out, the default value applies",
                                fix: "Check BoundArgument.IsExplicit to tell written from default arguments");

            Assert.That.AreEqual(true, writeResponse.ConstantValue,
                                 because: "The default value of 'writeResponse' is true",
                                 fix: "Check BoundArgument.ConstantValue");

            // ArgumentFor: by position or by name alike.
            Assert.That.AreEqual("false", calls[1].ArgumentFor("writeResponse")?.Expression,
                                 because: "The second call passes 'false' by position",
                                 fix: "Use call.ArgumentFor(parameter) instead of call.Arguments[index]");
        }

        [TestMethod]
        public void How_To_Find_Out_Where_A_Called_Method_Comes_From()
        {
            var codeBase = CodeBase.FromSource(ClientCode).WithSymbols();

            var invocations = codeBase.AllTrees
                                      .SelectMany(tree => tree.Invocations)
                                      .ToList();

            var post = invocations.First(call => call.Name == "PostAsync").Method;
            var count = invocations.Single(call => call.Name == "Count").Method;

            Assert.That.IsNotNull(post,
                                  because: "PostAsync binds to ApiClient.PostAsync",
                                  fix: "Check the compilation diagnostics, an unresolved call has no method");

            Assert.That.IsNotNull(count,
                                  because: "Count binds to System.Linq.Enumerable.Count",
                                  fix: "Check the compilation diagnostics, an unresolved call has no method");

            // Origin: declared in the code base, in a NuGet package or in the framework.
            Assert.That.IsTrue(post.Origin.IsSource,
                               because: "ApiClient.PostAsync is declared in the code itself",
                               fix: "Check Method.Origin.Kind");

            Assert.That.IsTrue(count.Origin.IsFramework,
                               because: "Count() is the LINQ extension method of the framework, the syntax alone cannot tell",
                               fix: "Check Method.Origin.Kind");

            Assert.That.IsTrue(post.ReturnsTask,
                               because: "The resolved method knows its declaration: PostAsync returns Task",
                               fix: "Use call.Method to get the declaration behind a call");
        }

        [TestMethod]
        public void How_To_Parse_The_Solution_With_Symbols()
        {
            var solution = SolutionFile.Parse(RepositoryPaths.Solution, ParseMode.WithSymbols);

            Assert.That.IsTrue(solution.HasSymbols,
                               because: "The solution was parsed with ParseMode.WithSymbols",
                               fix: "Pass ParseMode.WithSymbols to SolutionFile.Parse");

            var webApi = solution.ProductionProjects.Single(p => p.Name == "MyCodingZone.WebApi");

            // Before trusting symbols: are all types and names known? Missing ones usually mean 'not restored'.
            // Note: Solution.Parser 6.1.4 also reports CS8805 (compiles the web app as library) and CS9137 (interceptors
            // of the RequestDelegateGenerator) for the WebApi. Both do not affect the symbols, so we check for missing references only.
            string[] missingReference = ["CS0246", "CS0234", "CS0103", "CS0012"];

            var errors = webApi.CompilationDiagnostics()
                               .Where(d => missingReference.Contains(d.Id))
                               .Select(d => d.ToString())
                               .ToList();

            Assert.That.IsEmpty(errors,
                                because: $"Without the referenced packages calls stay unresolved{Environment.NewLine}{string.Join(Environment.NewLine, errors)}",
                                fix: "Run 'dotnet restore code-rules.slnx' before the CodeRules");

            // Only this project is compiled, on first symbol access.
            // tree.Invocations covers calls inside members, the top-level statements of Program.cs are not part of it.
            var invocations = webApi.Trees
                                    .SelectMany(tree => tree.Invocations)
                                    .ToList();

            // In Api/Startup.cs: services.AddUsers(configuration) and app.MapGroup("api/v1")
            var addUsers = invocations.Single(call => call.Name == "AddUsers").Method;
            var mapGroup = invocations.Single(call => call.Name == "MapGroup").Method;

            Assert.That.IsTrue(addUsers?.Origin.IsSource ?? false,
                               because: "AddUsers is declared in the WebApi itself (Api/User/Startup.cs)",
                               fix: "Check Method.Origin.Kind");

            Assert.That.IsTrue(mapGroup?.Origin.IsFramework ?? false,
                               because: $"MapGroup comes from the ASP.NET Core shared framework, Origin: {mapGroup?.Origin.Name}",
                               fix: "Check Method.Origin.Kind");

            // A typical rule on top: which calls of the WebApi go into which framework or package?
            var external = invocations.Select(call => call.Method)
                                      .Where(method => method is not null && !method.Origin.IsSource)
                                      .Select(method => method!.Origin.Name)
                                      .Distinct()
                                      .ToList();

            Assert.That.IsNotEmpty(external,
                                   because: "The WebApi calls into ASP.NET Core and DependencyInjection",
                                   fix: "Check Method.Origin for the resolved calls");
        }
    }
}
