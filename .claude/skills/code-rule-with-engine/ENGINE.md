# Siemens Code Rule Engine - contracts cheat sheet

Namespace `Siemens.AspNet.CodeRules.Contracts` (global using in `src/MyCodingZone.CodeRules`).
The code model inside the contexts is Solution.Parser - see [../code-rule-as-test/API.md](../code-rule-as-test/API.md).

## Rule interfaces

| Interface | Method | Group fallback |
|---|---|---|
| `ICodeRule` | `IEnumerable<Finding> Evaluate(CodeContext context)` | `Code` (overridden by the namespace segment) |
| `ISolutionRule` | `IEnumerable<Finding> Evaluate(SolutionContext context)` | `Solution` |
| `IRule<TContext>` | `IEnumerable<Finding> Evaluate(TContext context)` | `TContext.Group` |

Common members (`IRule`): `Id`, `Title`, `Why`, `HowToFix`, `Tags` (`ImmutableList<string>`), optional `ReadMoreUrl` (`Uri`),
`DefaultSeverity`, `AllowSuppression`, `SkipReason`, `Deprecated`, `DocumentationUrl` (set by the setup's URL template).

## Contexts

| Context | Members |
|---|---|
| `CodeContext` | `ProductionTrees`, `TestTrees`, `AllTrees` (`ImmutableList<CSharpSyntaxTree>`), `Classes`, `Records`, `Interfaces`, `Enums`, `Structs` |
| `SolutionContext` | `Solution` (`SolutionFile`), `SolutionDirectory` |
| own `IRuleContext` | `static string Group`, one public ctor taking other contexts (see `Services/ServiceContext.cs`) |

## Finding

`What` (required), `Where` (`CodeLocation` or `FileInfo`), `Current`, `ShouldBe`, `HowToFix`, `Details`
(`[("Key", value)]`), `Severity`.

## Wiring

- `CodeRuleSetup` (`ICodeRuleSetup`) - composition root: solution `code-rules.slnx`, rule pack, output mode, docs URL template. Do not touch for a new rule.
- `MyCodingZoneRulePack` (`ICodeRulePack`) - `rules.Add<TRule>()`. Every new rule goes here.
- The MSTest source generator writes one test per registered rule: `CodeRules.Generated.<ID>_<ClassName>`.

## Testing (`src/MyCodingZone.CodeRules.Engine.Test`)

- `Assert.That.CodeRule.ViolatesAsync<TRule>(code)` - expected findings marked with `[|...|]`
- `Assert.That.CodeRule.CompliesAsync<TRule>(code)` / `CompliesAsync(ruleTester, code)`
- `Assert.That.CodeRule.ViolatesOnceAsync<TRule>(code)` - returns the single `Finding`
- `new RuleTester<TRule>(options => options.AddContext(context)).RunAsync(code)` - for custom contexts such as a `SolutionContext` from a fixture solution
