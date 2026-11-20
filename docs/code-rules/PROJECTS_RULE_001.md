# PROJECTS_RULE_001 · Productive projects must not reference test projects

> Example documentation for the talk. In a real project this page holds the decision behind the rule,
> its owner and the history of known exceptions. Every finding links here (`Docs` in the Human output,
> `documentation` in the AI output).

| | |
|---|---|
| **Id** | `PROJECTS_RULE_001` |
| **Group** | Projects |
| **Tags** | `Architecture` |
| **Stage 2** | [`PROJECTS_RULE_001.cs`](../../src/MyCodingZone.CodeRules.Test/ProjectReferences/PROJECTS_RULE_001.cs) |
| **Stage 5** | [`ProductionProjectsMustNotReferenceTestProjects`](../../src/MyCodingZone.CodeRules/Projects/ProductionProjectsMustNotReferenceTestProjects.cs) · tests: [`ProductionProjectsMustNotReferenceTestProjects_Tests.cs`](../../src/MyCodingZone.CodeRules.Engine.Test/Projects/ProductionProjectsMustNotReferenceTestProjects_Tests.cs) |

## Why

Test projects are never deployed. A productive project that depends on them ships test code and test packages (MSTest, test SDKs, fakes) into production and creates a dependency cycle between production and test code.

## How to fix

Remove the `<ProjectReference>` to the test project.

If the productive code needs something from the test project, move that code into a productive project instead.

## Example

Violates the rule:

```xml
<!-- src/Example.WebApi/Example.WebApi.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Example.WebApi.Test\Example.WebApi.Test.csproj" />
</ItemGroup>
```

Complies with the rule:

```xml
<!-- src/Example.WebApi/Example.WebApi.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Example.Contracts\Example.Contracts.csproj" />
</ItemGroup>
```

## Accepting a single finding

Stage 5 (Code Rule Engine) only, always with a reason and the approval of a human (see [AGENTS.md](../../AGENTS.md)):

```csharp
[SuppressMessage("CodeRules", "PROJECTS_RULE_001", Justification = "...")]
```

## Verify the fix

```bash
dotnet test src/MyCodingZone.CodeRules.Test --filter "FullyQualifiedName~PROJECTS_RULE_001"
dotnet test src/MyCodingZone.CodeRules --filter "FullyQualifiedName~PROJECTS_RULE_001"
```
