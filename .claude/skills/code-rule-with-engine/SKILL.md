---
name: code-rule-with-engine
description: Stage 5 - write a CodeRule on the Siemens Code Rule Engine in src/MyCodingZone.CodeRules as a sealed class implementing ICodeRule (default), ISolutionRule or IRule<TContext>, register it in MyCodingZoneRulePack, add its functional tests in src/MyCodingZone.CodeRules.Engine.Test and the docs/code-rules page. Not for MSTest rules with CodeRuleTestBase - that is /code-rule-as-test.
---

# CodeRule with the engine (stage 5)

**Purpose:** Turn a piece of project knowledge into a rule on the Siemens Code Rule Engine. The rule is a small class
that only *evaluates* and returns `Finding`s; the engine parses the solution, creates the contexts, generates one MSTest
test per rule and renders the output.

**Packages:** `Siemens.AspNet.CodeRules.Contracts` (rule API), `Solution.Parser` (the code model the contexts expose).
Cheat sheets: [ENGINE.md](ENGINE.md) for the contracts, [../code-rule-as-test/API.md](../code-rule-as-test/API.md) for
the Solution.Parser model (`Record`, `Class`, `Property`, `ProjectFile`, ...).

## Scope - read first

This repository has several implementations of "a rule" for demo purposes. This skill writes **exactly one kind**:

| Do | Do not |
|---|---|
| `public sealed class <WhatItChecks> : ICodeRule` (or `ISolutionRule` / `IRule<TContext>`, see step 2) in `src/MyCodingZone.CodeRules/<Group>/` | derive from `CodeRuleTestBase`, use `CodeRuleFinding` or `Assert.That.CodeRuleHasNoFindings` (stage 2 → `/code-rule-as-test`) |
| return `IEnumerable<Finding>` from `Evaluate(...)` | write `[TestMethod]`s for the rule itself - the source generator creates them |
| tests via `Assert.That.CodeRule.*` in `src/MyCodingZone.CodeRules.Engine.Test` | write a `DiagnosticAnalyzer` (stage 3) or an example in `src/MyCodingZone.HowTo` (stage 1) |

**Counterpart:** stage 2 and stage 5 share the rule IDs (see AGENTS.md). Ask the user once at the start:
*"Only the engine rule, or also the stage 2 test?"* Default is both: do this skill, then `/code-rule-as-test` with the
**same** `Id`, `Title`, `Why`, `HowToFix`, `ReadMoreUrl`. If the stage 2 rule already exists, copy its texts.

---

## Workflow

### 1. Pin down the rule

State in one sentence each: **What** is forbidden or required, **Scope** (productive / test code; solution, project
or C# level), **Why** it matters, **Fix** for the developer. If the scope or the fix is unclear, ask. Do not guess a rule.

### 2. Pick the interface - `ICodeRule` is the default

| Interface | `Evaluate(...)` gets | Use when | Example |
|---|---|---|---|
| **`ICodeRule`** (default) | `CodeContext` | the rule looks at C# code: types, members, attributes, usings | `Records/RecordPropertiesMustBeImmutable.cs`, `Services/ServiceRegistrationsReturnVoid.cs` |
| `ISolutionRule` | `SolutionContext` | the rule looks at the solution or csproj files: projects, references, packages, properties | `Projects/ProductionProjectsMustNotReferenceTestProjects.cs` |
| `IRule<TContext>` | your own `TContext : IRuleContext` | **several** rules need the same derived model (e.g. "all services with their registration") that is expensive or non-trivial to compute | `Services/ServicesInjectAtMostFiveDependencies.cs` with `Services/ServiceContext.cs` |

Decision: start with `ICodeRule`. Switch to `ISolutionRule` only if the rule needs no C# code. Use `IRule<TContext>`
only if a matching context already exists (reuse `ServiceContext` for a services rule) or if at least a second rule will
share the new model - never create a context for a single rule.

A new context: `sealed class <Name>Context(CodeContext code) : IRuleContext` with `public static string Group => "<Group>";`,
exactly **one public constructor** taking other contexts, models as get-only properties computed once. No static fields,
no registration - the engine creates it once per run.

### 3. Create the file

- **Id**: `<TOPIC>_RULE_<NNN>`. Take the next free number of the topic - check **both** the `Id` properties in
  `src/MyCodingZone.CodeRules/**/*.cs` and `src/MyCodingZone.CodeRules.Test/**/*_RULE_*.cs`.
- **Folder / namespace**: `src/MyCodingZone.CodeRules/<Group>/`, namespace `MyCodingZone.CodeRules.<Group>`. The
  segment after `MyCodingZone.CodeRules` is the group (`Records`, `Services`, `Projects`, ...).
- **Class name** = what it checks, not the ID (`RecordPropertiesMustBeImmutable`). The generated test is `<ID>_<ClassName>`.
- `public sealed class`, one rule per file, XML summary pointing at the stage 2 counterpart.

### 4. Write the rule - template

```csharp
using System.Collections.Immutable;

namespace MyCodingZone.CodeRules.Records
{
    /// <summary>Stage 2 counterpart: src/MyCodingZone.CodeRules.Test/Records/RECORDS_RULE_001.cs</summary>
    public sealed class RecordPropertiesMustBeImmutable : ICodeRule
    {
        public string Id => "RECORDS_RULE_001";

        public string Title => "Record properties must be immutable";

        public string Why => "A record is a snapshot of a state, e.g. data loaded from the database. " +
                             "It must not change while it is being processed. " +
                             "A setter lets any code modify the same instance, so other parts that hold a " +
                             "reference suddenly see different data. " +
                             "A change must produce a new instance instead.";

        public string HowToFix => "Replace 'set;' with 'init;' and create a modified copy with a 'with' expression, e.g. 'order with { Status = OrderStatus.Shipped }'.";

        public ImmutableList<string> Tags => ["Records"];

        public Uri ReadMoreUrl => new("https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record#init-only-properties");

        public IEnumerable<Finding> Evaluate(CodeContext context)
        {
            return from record in context.Records
                   from property in record.Properties
                   where !property.IsReadOnly
                   select new Finding
                   {
                       What = property.FullyQualifiedName,
                       Where = property.Location,
                       Current = property.SourceCode,
                       ShouldBe = property.SourceCode.Replace("set;", "init;", StringComparison.Ordinal),
                       Details = [("Property", $"{property.Type} {property.Name}")]
                   };
        }
    }
}
```

Rules for findings:

- [ ] Object initializer only; `What` names the offending element, `Where` is `declaration.Location` or a `FileInfo`.
- [ ] `Current` must be **exactly** the source at `Where` - fixes replace that span with `ShouldBe`. If `Where` marks
      something bigger (a whole class, a csproj), leave `Current` / `ShouldBe` empty and put the relevant line into `Details`.
- [ ] `HowToFix` on the finding only when it depends on the finding; otherwise the rule's `HowToFix` is used.
- [ ] Synthetic text (counts, suggested registrations, ...) goes into `Details`, keys unique per finding.
- [ ] `Id`, `Title`, `Why`, `HowToFix`, `ReadMoreUrl` identical to the stage 2 counterpart.
- [ ] Use `StringComparison.Ordinal` on string APIs - the project builds with all analyzers in CI.

### 5. Register

Add the rule to `src/MyCodingZone.CodeRules/MyCodingZoneRulePack.cs` (`.Add<YourRule>()`). A rule that is not registered
gets no generated test, and the guard test fails.

### 6. Functional tests

`src/MyCodingZone.CodeRules.Engine.Test/<Group>/<RuleClass>_Tests.cs`, same folder as the rule:

```csharp
using MyCodingZone.CodeRules.Records;

namespace MyCodingZone.CodeRules.Engine.Test.Records
{
    [TestClass]
    [TestCategory("CodeRules.Tests")]
    [TestCategory("CodeRules.Tests.Records")]
    public sealed class RecordPropertiesMustBeImmutable_Tests
    {
        [TestMethod]
        public async Task Should_Report_Record_Property_When_It_Has_A_Setter()
        {
            await Assert.That.CodeRule.ViolatesAsync<RecordPropertiesMustBeImmutable>("""
                public record Order(int Id)
                {
                    [|public string Status { get; set; } = "";|]
                }
                """);
        }

        [TestMethod]
        [DataRow("public string Status { get; init; } = \"\";")]
        [DataRow("public string Status { get; } = \"\";")]
        public async Task Should_Accept_Record_Property_That_Cannot_Change(string property)
        {
            await Assert.That.CodeRule.CompliesAsync<RecordPropertiesMustBeImmutable>($$"""
                public record Order(int Id)
                {
                    {{property}}
                }
                """);
        }

        [TestMethod]
        public async Task Should_Suggest_An_Init_Accessor()
        {
            var finding = await Assert.That.CodeRule.ViolatesOnceAsync<RecordPropertiesMustBeImmutable>("""
                public record Order(int Id)
                {
                    [|public string Status { get; set; } = "";|]
                }
                """);

            Assert.That.AreEqual("public string Status { get; init; } = \"\";",
                                 finding.ShouldBe,
                                 because: "The finding shows the property with 'init' instead of 'set', so the fix can be applied as is",
                                 fix: "Check ShouldBe of RECORDS_RULE_001 (RecordPropertiesMustBeImmutable)");
        }
    }
}
```

- `Should_Report_<What>_When_<Condition>` with `ViolatesAsync<TRule>`, **every** expected finding marked with `[|...|]`
  (the marked span must equal `Where` / `Current`).
- `Should_Accept_<What>_When_<Condition>` with `CompliesAsync<TRule>`, variants as `[DataRow]`.
- Details of one finding via `ViolatesOnceAsync<TRule>` and `Assert.That.AreEqual(expected, actual, because:, fix:)`.
  Never plain `Assert.AreEqual`.
- `ISolutionRule`: there is no C# snippet to test against. Build a tiny solution in a temp folder and pass a
  `SolutionContext` to a `RuleTester<TRule>` - copy `Projects/ProductionProjectsMustNotReferenceTestProjects_Tests.cs`.
- Test class `sealed`, categories `CodeRules.Tests` and `CodeRules.Tests.<Group>`.

### 7. Documentation page

`docs/code-rules/<ID>.md` (shared with stage 2): copy an existing page (Why, How to fix, an example that violates and one
that complies) and add a line to `docs/code-rules/README.md`. Every finding links to it; without it the `Docs` link is a 404.

### 8. Verify - green AND red

1. Functional tests: `dotnet test src/MyCodingZone.CodeRules.Engine.Test --filter "FullyQualifiedName~<RuleClass>"`
2. The generated rule test against the real code: `dotnet test src/MyCodingZone.CodeRules --filter "FullyQualifiedName~<ID>"`
3. If it is red on the current code: the code is wrong, not the rule. Report the findings; do not weaken the rule,
   add suppressions or exceptions without explicit approval (see AGENTS.md). An accepted single finding needs
   `[SuppressMessage("CodeRules", "<ID>", Justification = "...")]` or `#pragma warning disable <ID> // reason`.
4. Run both stages: `dotnet test code-rules.slnx --filter "TestCategory~CodeRules"`.

### 9. Report and hand over

Tell the user: rule id, class and interface (and why that interface), what it checks, green on the current code (or the
findings), and known blind spots. If the stage 2 counterpart is still missing, continue with `/code-rule-as-test`
(or say clearly that it is missing).
