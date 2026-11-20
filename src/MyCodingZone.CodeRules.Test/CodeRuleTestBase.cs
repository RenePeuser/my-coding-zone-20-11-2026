using System.Collections.Immutable;
using Solution.Parser.CSharp;
using Solution.Parser.Sln;
using CSharpSyntaxTree = Solution.Parser.CSharp.CSharpSyntaxTree;
using Enum = Solution.Parser.CSharp.Enum;

// [assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
[assembly: DoNotParallelize]

namespace MyCodingZone.CodeRules.Test
{
    public record JsonFile(FileInfo FileInfo,
                           string Content);

    [TestClass]
    public abstract class CodeRuleTestBase
    {
        // You start activating code rules, you have 1 Mio findings exclude first legacy stuff
        // To monitor just new code. But do step by step cleanups !
        private static readonly string[] IgnoreForCodeRuleAnalyse = [];

        public static ImmutableList<CSharpSyntaxTree> SyntaxTreesToAnalyze { get; set; } = ImmutableList<CSharpSyntaxTree>.Empty;

        public static ImmutableList<CSharpSyntaxTree> ProductiveSyntaxTrees { get; set; } = ImmutableList<CSharpSyntaxTree>.Empty;

        public static ImmutableList<CSharpSyntaxTree> TestSyntaxTrees { get; set; } = ImmutableList<CSharpSyntaxTree>.Empty;

        public static SolutionFile Solution { get; private set; } = null!;

        public static ImmutableList<CSharpSyntaxTree> AllSyntaxTrees { get; set; } = ImmutableList<CSharpSyntaxTree>.Empty;

        public static ImmutableList<Record> Records { get; private set; } = ImmutableList<Record>.Empty;

        public static ImmutableList<Class> Classes { get; private set; } = ImmutableList<Class>.Empty;

        protected static ImmutableList<Interface> Interfaces { get; private set; } = ImmutableList<Interface>.Empty;

        public static ImmutableList<Enum> Enums { get; private set; } = ImmutableList<Enum>.Empty;

        protected static ImmutableList<Struct> Structs { get; private set; } = ImmutableList<Struct>.Empty;

        [AssemblyInitialize]
        public static void Init(TestContext testContext)
        {
            // ParseMode.WithSymbols: syntax trees plus symbols (call.Method, call.Passes(...), method.Origin, ...).
            // Nothing is compiled here. A project is compiled the first time a rule asks for one of its symbols,
            // so rules that only use the syntax stay as fast as before.
            // The trees have to come from project.Trees: a tree parsed on its own (CSharpSyntaxTree.Parse(file)) has no symbols.

            // Solution = SolutionFile.Find("code-rules.slnx", ParseMode.WithSymbols);
            Solution = SolutionFile.Find("code-rules.slnx");

            ProductiveSyntaxTrees = Solution.ProductionProjects
                                            .SelectMany(p => p.Trees)
                                            .Where(tree => !IgnoreForCodeRuleAnalyse.Any(namespaceToIgnore => tree.Namespace.Name.Contains(namespaceToIgnore)))
                                            .ToImmutableList();

            TestSyntaxTrees = Solution.UnitTestProjects
                                      .SelectMany(p => p.Trees)
                                      .Where(tree => !IgnoreForCodeRuleAnalyse.Any(namespaceToIgnore => tree.Namespace.Name.Contains(namespaceToIgnore)))
                                      .ToImmutableList();

            AllSyntaxTrees = ProductiveSyntaxTrees.Concat(TestSyntaxTrees).ToImmutableList();

            SyntaxTreesToAnalyze = AllSyntaxTrees.Where(tree => !IgnoreForCodeRuleAnalyse.Any(namespaceToIgnore => tree.Namespace.Name.Contains(namespaceToIgnore))).ToImmutableList();

            Records = AllSyntaxTrees.SelectMany(tree => tree.Records).ToImmutableList();
            Classes = AllSyntaxTrees.SelectMany(tree => tree.Classes).ToImmutableList();
            Enums = AllSyntaxTrees.SelectMany(tree => tree.Enums).ToImmutableList();
            Interfaces = AllSyntaxTrees.SelectMany(tree => tree.Interfaces).ToImmutableList();
            Structs = AllSyntaxTrees.SelectMany(tree => tree.Structs).ToImmutableList();
        }
    }
}
