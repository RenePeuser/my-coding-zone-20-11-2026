---
name: code-rule-as-test
description: Stage 2 only - write a CodeRule as a plain MSTest test class deriving from CodeRuleTestBase in src/MyCodingZone.CodeRules.Test, using Solution.Parser and the own CodeRuleFinding / Assert.That.CodeRuleHasNoFindings infrastructure. Does NOT use ICodeRule / ISolutionRule / IRule<T> - for the Siemens Code Rule Engine use /code-rule-with-engine.
---

# CodeRule as test (stage 2)

**Purpose:** Turn a piece of project knowledge ("productive projects must not reference test projects",
"record properties must be immutable", ...) into a CodeRule written as an ordinary MSTest test, with the repository's
own infrastructure. This is the "build it yourself" variant of the talk.

**Package:** `Solution.Parser` (version in `Directory.Packages.props`). API cheat sheet: [API.md](API.md).

## Scope - read first

This repository has several implementations of "a rule" for demo purposes. This skill writes **exactly one kind**:

| Do | Do not |
|---|---|
| `public class <ID> : CodeRuleTestBase` with `[TestClass]` / `[TestMethod]` in `src/MyCodingZone.CodeRules.Test/<Group>/` | implement `ICodeRule`, `ISolutionRule` or `IRule<TContext>` (that is stage 5 → `/code-rule-with-engine`) |
| `CodeRuleFinding` + `Assert.That.CodeRuleHasNoFindings(...)` | use `Finding`, `RuleTester`, `Assert.That.CodeRule.ViolatesAsync` (stage 5) |
| | write a `DiagnosticAnalyzer` (stage 3, `src/MyCodingZone.Analyzer`) or an example in `src/MyCodingZone.HowTo` (stage 1) |

**Counterpart:** stage 2 and stage 5 share the rule IDs (see AGENTS.md). Ask the user once at the start:
*"Only the stage 2 test, or also the stage 5 engine rule?"* Default is both: do this skill first, then
`/code-rule-with-engine` with the **same** `Id`, `Title`, `Why`, `HowToFix`, `ReadMoreUrl`. If the stage 5 rule already
exists, copy the texts from it.

**When to use:**
- The user wants a rule "as a test", "in stage 2", "with CodeRuleTestBase" or "without the engine"
- An existing rule in `src/MyCodingZone.CodeRules.Test` has to be extended
- The stage 2 counterpart of an existing stage 5 rule is missing

---

## Workflow

### 1. Pin down the rule

Before writing code, state in one sentence each:

- **What** is forbidden or required
- **Scope**: productive code, test code or both; solution, project file or C# file level
- **Why** it matters (goes into the finding)
- **Fix**: what the developer has to change (goes into the finding)

If the scope or the fix is unclear, ask the user. Do not guess a rule.

### 2. Pick the level and the data source

All data is loaded once in `CodeRuleTestBase` (`[AssemblyInitialize]`). Use it, do not parse again.

| Level | Rule is about | Start from |
|---|---|---|
| Solution | which projects exist, naming, test vs productive | `Solution.Projects`, `Solution.ProductionProjects`, `Solution.UnitTestProjects` |
| Project | csproj content: references, packages, properties | `project.ProjectReferences`, `project.PackageReferences`, `project.Document` (XDocument) |
| C# file | types, members, attributes, usings, namespaces | `ProductiveSyntaxTrees`, `TestSyntaxTrees`, `AllSyntaxTrees`, `Records`, `Classes`, `Enums`, `Interfaces`, `Structs` |

A derived model several rules of one group share (e.g. "all services") goes into an abstract base class deriving
from `CodeRuleTestBase` with a static `Lazy<T>` - see `Services/ServiceCodeRuleBase.cs`.

Only reach for raw Roslyn when the Solution.Parser model does not carry what you need
(see "Model gaps" in [API.md](API.md)). If you add something to `CodeRuleTestBase`, keep it lazy or cheap.

### 3. Create the file

- Id: `<TOPIC>_RULE_<NNN>`, e.g. `RECORDS_RULE_001`, `PROJECTS_RULE_001`. Reuse the topic of an existing
  rule when it fits and take the next free number - check **both** `src/MyCodingZone.CodeRules.Test/**/*_RULE_*.cs`
  and the `Id` properties in `src/MyCodingZone.CodeRules/**/*.cs`; otherwise start a new topic with `_001`.
- Folder: one per topic, named in plain words, e.g. `Records/`, `ProjectReferences/`. The folder becomes the
  group in the output.
- File name and class name = rule id. One rule per file.
- Namespace: `MyCodingZone.CodeRules.Test`, no sub namespace per folder.
- Categories: `[TestCategory("CodeRules")]` plus one specific, e.g. `[TestCategory("CodeRules Records")]`.
- Method name reads as the rule: `Productive_Projects_Are_Not_Allowed_To_Reference_Any_Test_Project`.

### 4. Write the rule - template

Collect **all** findings as `CodeRuleFinding`, then assert once with `Assert.That.CodeRuleHasNoFindings`.
The rule only states *what* it checks - layout, sorting, documentation link and Human/Ai output come from
`src/MyCodingZone.CodeRules.Test/TestEnvironment/`. Never build the failure message as a string.

The names mirror stage 5 on purpose: the finding has `What`, `Where`, `Current`, `ShouldBe`, `HowToFix` and
`Details`; the assert takes `id`, `title`, `why`, `howToFix` and `readMoreUrl`.

Build every finding with an object initializer only - no constructor arguments, no mix of both.
`What` is `required`, the compiler rejects a finding without it. All other properties are optional.

#### 4a. C# declaration

For anything derived from `Solution.Parser.CSharp.DeclarationBase` (type, record, property, method, field, ...)
take `What`, `Where` and `Current` from the declaration, then add what carries information:
`ShouldBe`, `HowToFix`, `Details`.

```csharp
using Extensions.Pack;

namespace MyCodingZone.CodeRules.Test
{
    [TestCategory("CodeRules")]
    [TestCategory("CodeRules Records")]
    [TestClass]
    public class RECORDS_RULE_001 : CodeRuleTestBase
    {
        [TestMethod]
        public void Record_Properties_Have_To_Be_Immutable()
        {
            var findings = from record in Records
                           from property in record.Properties
                           where property.IsReadOnly.IsFalse()
                           select new CodeRuleFinding
                           {
                               What = property.FullyQualifiedName,
                               Where = property.Location,
                               Current = property.SourceCode,
                               ShouldBe = property.SourceCode.Replace("set;", "init;"),
                               Details = [("Property", $"{property.Type} {property.Name}")]
                           };

            Assert.That.CodeRuleHasNoFindings(findings,
                                              id: "RECORDS_RULE_001",
                                              title: "Record properties must be immutable",
                                              why: "A record is a snapshot of a state, e.g. data loaded from the database. It must not change while it is being processed. A setter lets any code modify the same instance, so other parts that hold a reference suddenly see different data. A change must produce a new instance instead.",
                                              howToFix: "Replace 'set;' with 'init;' and create a modified copy with a 'with' expression, e.g. 'order with { Status = OrderStatus.Shipped }'.",
                                              readMoreUrl: "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record#init-only-properties");
        }
    }
}
```

#### 4b. No declaration - project specific checks

For csproj, slnx, packages or snapshot JSON there is no declaration. Same initializer, `Where` accepts a
`CodeLocation` or a `FileInfo` (e.g. the csproj):

```csharp
select new CodeRuleFinding
{
    What = $"{project.AssemblyName} → {Path.GetFileNameWithoutExtension(include)}",   // required
    Where = projectFile,                                                             // FileInfo of the csproj
    Current = $"<ProjectReference Include=\"{reference.Include}\" />",
    HowToFix = $"Remove the <ProjectReference> from '{projectFile.Name}'."            // optional: overrides the HowToFix of the rule
};
```

Assert the same way with `Assert.That.CodeRuleHasNoFindings(...)` (see `ProjectReferences/PROJECTS_RULE_001.cs`).

If a project specific check yields no list of findings but just one fact (e.g. "the solution contains
exactly one Web API project", "`global.json` pins the SDK"), you may use a plain assert from
`AspNetCore.Simple.MsTest.Sdk` instead, always with `because:` and `fix:`:

```csharp
Assert.That.IsTrue(globalJson.Exists,
                   because: "The SDK version must be pinned so every machine and CI builds with the same SDK.",
                   fix: "Add a global.json with 'rollForward: disable' to the repository root.");
```

As soon as a check can hit more than one element, collect `CodeRuleFinding`s instead.

Finding checklist:

- [ ] **shape**: object initializer only, `What` always set (it is `required`)
- [ ] **what**: `What` names the offending element (type, member, reference, package)
- [ ] **where**: `Where` - pass `declaration.Location` or the csproj `FileInfo`; paths become repository relative
- [ ] **why**: `why:` - the decision behind the convention, once per rule
- [ ] **how to fix**: `howToFix:` for the rule, `HowToFix` on the finding when it depends on the finding; `Current` / `ShouldBe` for a before/after line
- [ ] **details** (optional): `Details = [("Key", value)]`, shown in the order given, keys unique per finding
- [ ] **read more** (optional): `readMoreUrl:` on the assert for background reading about the problem (API docs, pattern); the rule documentation link is added by the assert
- [ ] group (= folder), documentation link and rule source are added by the assert

Output mode: `CodeRuleSettings__OutputMode=ai` (or `TestSdkSettings__OutputMode=ai`) renders JSON for agents,
default is the framed Human output. Fixtures can catch `CodeRuleViolationException` and check `.Violation.Findings`.

### 5. Pitfalls

- **Paths:** never absolute. `ProjectReference.Include` is the raw relative path from the csproj with `\`.
  Resolve it with `Path.GetFullPath(Path.Combine(projectDir, include.Replace('\\', Path.DirectorySeparatorChar)))`
  and compare case-insensitively.
- **Test detection:** a project counts as test when its csproj contains `<IsTestProject>true` or references
  `MSTest.*`, `Microsoft.NET.Test.Sdk`, `NUnit`, `XUnit`. `IsTestProject` set only in `Directory.Build.props` is not seen.
- **Solution scope:** only projects listed in `code-rules.slnx` are parsed. Projects outside the slnx are invisible.
- **Nesting:** `tree.Types` / `tree.Records` are file-level only. Use `tree.AllTypes()` / `type.DescendantTypes()`
  for nested types, `AllMethods()` for all methods.
- **Generated code:** exclude `obj/`, `bin/`, `*.g.cs` if the query walks files itself.
- **Parse errors:** a file with `tree.HasParseErrors` yields an incomplete model; do not trust a green rule on it.
- **Determinism:** no time, no network, no ordering assumptions. Sort findings if the output order matters.

### 6. Documentation page

Every finding links to `docs/code-rules/<ID>.md`. If the page does not exist yet (it is shared with stage 5), create it:
copy an existing page (Why, How to fix, an example that violates and one that complies) and add a line to
`docs/code-rules/README.md`. Without it the `Docs` link of the new rule is a 404.

### 7. Verify - green AND red

1. Run only the new rule:
   `dotnet test src/MyCodingZone.CodeRules.Test --filter "FullyQualifiedName~RECORDS_RULE_001"`
2. If it is red on the current code: the code is wrong, not the rule. Report the findings to the user;
   do not weaken the rule or add exceptions without explicit approval (see AGENTS.md).
3. **Negative check:** temporarily introduce one violation (e.g. add a forbidden `<ProjectReference>`,
   a mutable property), run the rule, confirm the finding reads well, then **restore the file** and
   confirm with `git status` / `git diff` that nothing is left behind.
4. Run the stage 2 category: `dotnet test src/MyCodingZone.CodeRules.Test --filter TestCategory=CodeRules`.

### 8. Report and hand over

Tell the user: rule id and file, what it checks, green on the current code (or the findings), how the
negative check looked (paste the finding), and any known blind spots (e.g. projects outside the slnx).
If the stage 5 counterpart is still missing, continue with `/code-rule-with-engine` (or say clearly that it is missing).
