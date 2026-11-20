# SERVICES_RULE_002 · Each service registration has to be called

> Example documentation for the talk. In a real project this page holds the decision behind the rule,
> its owner and the history of known exceptions. Every finding links here (`Docs` in the Human output,
> `documentation` in the AI output).

| | |
|---|---|
| **Id** | `SERVICES_RULE_002` |
| **Group** | Services |
| **Tags** | `DependencyInjection` |
| **Stage 2** | [`SERVICES_RULE_002.cs`](../../src/MyCodingZone.CodeRules.Test/Services/SERVICES_RULE_002.cs) |
| **Stage 5** | [`ServiceRegistrationMustBeCalled`](../../src/MyCodingZone.CodeRules/Services/ServiceRegistrationMustBeCalled.cs) · tests: [`ServiceRegistrationMustBeCalled_Tests.cs`](../../src/MyCodingZone.CodeRules.Engine.Test/Services/ServiceRegistrationMustBeCalled_Tests.cs) |

## Why

A registration that is never called registers nothing. The application compiles, but fails at runtime as soon as the service is resolved.

## How to fix

Call the registration from the registration of the parent feature or from `Program.cs`.

## Example

Violates the rule:

```csharp
internal static void AddUserFeature(this IServiceCollection services)
{
    // AddUserService() is defined, but nobody calls it
}
```

Complies with the rule:

```csharp
internal static void AddUserFeature(this IServiceCollection services)
{
    services.AddUserService();
}
```

## Accepting a single finding

Stage 5 (Code Rule Engine) only, always with a reason and the approval of a human (see [AGENTS.md](../../AGENTS.md)):

```csharp
[SuppressMessage("CodeRules", "SERVICES_RULE_002", Justification = "...")]
```

## Verify the fix

```bash
dotnet test src/MyCodingZone.CodeRules.Test --filter "FullyQualifiedName~SERVICES_RULE_002"
dotnet test src/MyCodingZone.CodeRules --filter "FullyQualifiedName~SERVICES_RULE_002"
```
