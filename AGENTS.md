# AGENTS.md

Companion repository for the talk "CodeRules & Analyzer – Self-Defending Codebases" (MyCoding Zone, 20.11.2026).
The goal: project knowledge lives in executable CodeRules. When a rule fails, the finding tells you what, where, why and how to fix it.

The repository tells the story in six stages: 01 solution parsing (`src/MyCodingZone.HowTo`), 02 CodeRules as tests with an own
infrastructure (`src/MyCodingZone.CodeRules.Test`), 03 Roslyn analyzer (`src/MyCodingZone.Analyzer`), 04 Roslyn code fixes (`src/MyCodingZone.CodeFixes`), 05 the same rules as stage 2
on the Siemens Code Rule Engine (`src/MyCodingZone.CodeRules`) and 06 the sample Minimal API with snapshot API tests (`src/MyCodingZone.WebApi`). Stage 2 and stage 5 share the rule IDs; a rule changed in one
stage has to be changed in the other one as well, including its page in `docs/code-rules/<ID>.md`. To add a rule use `/code-rule-with-engine` for stage 5 (`ICodeRule` by default, `ISolutionRule` / `IRule<TContext>` where it fits)
and `/code-rule-as-test` for stage 2 (MSTest with `CodeRuleTestBase`); each skill hands over to the other for the counterpart.

## Commands

```bash
dotnet build code-rules.slnx
dotnet test code-rules.slnx

# all CodeRules (stage 2 and stage 5)
dotnet test code-rules.slnx --filter TestCategory=CodeRules

# stage 5 only: the generated rule tests, or the functional tests of the rules
dotnet test src/MyCodingZone.CodeRules --filter TestCategory=CodeRules
dotnet test src/MyCodingZone.CodeRules.Engine.Test

# HowTo examples only
dotnet test src/MyCodingZone.HowTo --filter TestCategory=HowTo

# strict CI mode (all analyzers, warnings as errors, NuGet audit)
dotnet build code-rules.slnx -p:CI=true
```

The SDK version is pinned in `global.json` (`rollForward: disable`). `Microsoft.CodeAnalysis.*` must match the compiler of that SDK
(5.9.0 for 10.0.404), otherwise the analyzer is not loaded (CS9057). `Microsoft.Build*` stays on 18.9.6, the last version with a
`net10.0` target. Update them together with `global.json`.

## Layout

| Path | Purpose |
|---|---|
| `src/MyCodingZone.HowTo` | Stage 1: minimal examples, MSBuild, Reflection, Roslyn, custom abstraction |
| `src/MyCodingZone.CodeRules.Test` | Stage 2: CodeRules as MSTest tests, one rule per file, shared setup in `CodeRuleTestBase` |
| `src/MyCodingZone.Analyzer` | Stage 3: Roslyn analyzers (`MCZ_ASYNC_*`, `MCZ_MAPI_*`, `MCZ_INLINE_*`), referenced as analyzer by every project via `Directory.Build.props`; severities in `.editorconfig`. See its `AGENTS.md`. |
| `src/MyCodingZone.CodeFixes` | Stage 4: code fixes for `MCZ_MAPI_*` and `MCZ_INLINE_*`, one CodeFix per DiagnosticId, mirrors the analyzer folders. See its `AGENTS.md`. |
| `src/MyCodingZone.CodeFixes.Test` | Specs for the code fixes: explicit source before / after, one scenario per file, see its `AGENTS.md`. |
| `src/MyCodingZone.Analyzer.Test` | Specs for the analyzer, one scenario per file, see its `AGENTS.md`. |
| `src/MyCodingZone.CodeRules` | Stage 5: rules on the Siemens Code Rule Engine. `CodeRuleSetup` registers `MyCodingZoneRulePack`; one test per rule is generated. Rules live in `Projects/`, `Records/`, `Services/`. Stays a test project: as a productive project the SERVICES rules would report the rule classes themselves. |
| `src/MyCodingZone.CodeRules.Engine.Test` | Functional tests of the stage 5 rules with `Assert.That.CodeRule`. Mirrors the rule project: `<Group>/<RuleClass>_Tests.cs` for every `<Group>/<RuleClass>.cs`. |
| `src/MyCodingZone.WebApi` | Sample Minimal API the rules run against, errors via `Siemens.AspNet.ErrorHandling` (`AddErrorHandling` / `UseErrorHandling`) |
| `src/MyCodingZone.WebApi.Test` | Snapshot-based API tests (`Requests/` + `Responses/` JSON), success cases with the classic and the fluent SDK API, error cases in `InvalidRequests/`, see `SNAPSHOT_TESTING.md`. |
| `docs/code-rules` | One page per rule (`<ID>.md`), the target of the `Docs` link in every finding of stage 2 and stage 5. |

## Working with CodeRules

- A failing CodeRule means the code is wrong, not the rule. Fix the code.
- Never weaken a rule, add whitelist or exception entries, or add `[Ignore]` without explicit approval from a human. Every exception needs a reason in a comment.
- Re-run the failing rule to verify the fix before running the full suite.
- Set `CodeRuleSettings__OutputMode=ai` to get findings as JSON instead of the framed Human output (`TestSdkSettings__OutputMode=ai` works too and also switches the API tests).

### Output Mode Configuration

CodeRule test output can be switched between Human (framed console output) and AI (structured JSON) mode via environment variables:

**PowerShell:**
```powershell
# Primary setting
$env:CodeRuleSettings__OutputMode = 'ai'
dotnet test src/MyCodingZone.CodeRules.Test --filter TestCategory=CodeRules

# Fallback (also switches API tests)
$env:TestSdkSettings__OutputMode = 'ai'
dotnet test code-rules.slnx
```

**Bash/Linux:**
```bash
# Primary setting
CodeRuleSettings__OutputMode=ai dotnet test src/MyCodingZone.CodeRules.Test --filter TestCategory=CodeRules

# Fallback (also switches API tests)
TestSdkSettings__OutputMode=ai dotnet test code-rules.slnx
```

**Priority:** `CodeRuleSettings__OutputMode` > `TestSdkSettings__OutputMode` > default (Human). Any value except "ai" (case-insensitive) results in Human mode.

## Writing a new CodeRule

- One rule per file in `src/MyCodingZone.CodeRules.Test/<Group>/`, named after its rule id.
- Add `[TestCategory("CodeRules")]` plus a category, e.g. `[TestCategory("CodeRules Projects")]`.
- Use the same names as stage 5: every finding names `What` and `Where` (plus `Current` / `ShouldBe` / `HowToFix` where it helps), the assert takes `id`, `title`, `why`, `howToFix` and optionally `readMoreUrl`, with the same texts as the stage 5 rule.
- Rules must be deterministic and fast, with few false positives, and have positive and negative fixtures.
- Never use absolute paths. Resolve files relative to the repository root (see `src/MyCodingZone.HowTo/RepositoryPaths.cs`).

### Stage 5 (Siemens Code Rule Engine)

- One rule class per file in `src/MyCodingZone.CodeRules/<Group>/`, implementing `ICodeRule`, `ISolutionRule` or `IRule<TContext>`.
  The namespace segment after `MyCodingZone.CodeRules` becomes the group (`Records`, `Services`, ...). Name the class after
  what it checks, not after its ID; the generated test is named `<ID>_<ClassName>`.
- Register the rule in `MyCodingZoneRulePack`. A rule that is not registered gets no test; the guard test fails when one is lost.
- `Finding.Current` must be exactly the source at `Finding.Where` (fixes replace that span with `ShouldBe`). Synthetic text goes into `Details`.
- Shared derived models belong into a context with one public constructor taking other contexts (see `ServiceContext`), not into static fields.
- Every rule gets a test class `src/MyCodingZone.CodeRules.Engine.Test/<Group>/<RuleClass>_Tests.cs` (same folder as the rule), `sealed`,
  with `[TestCategory("CodeRules.Tests")]` and `[TestCategory("CodeRules.Tests.<Group>")]`:
  - `Should_Report_<What>_When_<Condition>` with `Assert.That.CodeRule.ViolatesAsync<TRule>(code)`, every expected finding marked with `[|...|]`,
  - `Should_Accept_<What>_When_<Condition>` with `Assert.That.CodeRule.CompliesAsync<TRule>(code)`, variants as `[DataRow]`,
  - details of a finding via `Assert.That.CodeRule.ViolatesOnceAsync<TRule>(code)` and `Assert.That.AreEqual(expected, actual, because:, fix:)`.
  Never plain `Assert.AreEqual`: `Assert.That.*` with `because` and `fix` gives the readable failure output.
- Accept a single finding only with a reason: `[SuppressMessage("CodeRules", "<ID>", Justification = "...")]` or `#pragma warning disable <ID> // reason`.

## Conventions

- NuGet versions live only in `Directory.Packages.props`. No `Version` attribute in project files.
- Shared build settings live in `Directory.Build.props`. Do not repeat them in project files.
- Test assertions use `Assert.That.*` with `because:` and `fix:` (package `AspNetCore.Simple.MsTest.Sdk`). The `/check-asserts` skill reviews this.
