# SERVICES_RULE_005 · Service registrations return void

> Example documentation for the talk. In a real project this page holds the decision behind the rule,
> its owner and the history of known exceptions. Every finding links here (`Docs` in the Human output,
> `documentation` in the AI output).

| | |
|---|---|
| **Id** | `SERVICES_RULE_005` |
| **Group** | Services |
| **Tags** | `DependencyInjection` |
| **Stage 2** | [`SERVICES_RULE_005.cs`](../../src/MyCodingZone.CodeRules.Test/Services/SERVICES_RULE_005.cs) |
| **Stage 5** | [`ServiceRegistrationsReturnVoid`](../../src/MyCodingZone.CodeRules/Services/ServiceRegistrationsReturnVoid.cs) · tests: [`ServiceRegistrationsReturnVoid_Tests.cs`](../../src/MyCodingZone.CodeRules.Engine.Test/Services/ServiceRegistrationsReturnVoid_Tests.cs) |

## Why

One style for all registrations: a registration is a statement, not a builder. Returning `IServiceCollection` invites long fluent chains in which a missing or duplicate registration is hard to spot in a review.

## How to fix

Return `void` from the registration and call each registration as its own statement.

## Example

Violates the rule:

```csharp
internal static IServiceCollection AddUserService(this IServiceCollection services)
{
    return services.AddSingleton<IUserService, UserService>();
}
```

Complies with the rule:

```csharp
internal static void AddUserService(this IServiceCollection services)
{
    services.AddSingleton<IUserService, UserService>();
}
```

## Accepting a single finding

Stage 5 (Code Rule Engine) only, always with a reason and the approval of a human (see [AGENTS.md](../../AGENTS.md)):

```csharp
[SuppressMessage("CodeRules", "SERVICES_RULE_005", Justification = "...")]
```

## Verify the fix

```bash
dotnet test src/MyCodingZone.CodeRules.Test --filter "FullyQualifiedName~SERVICES_RULE_005"
dotnet test src/MyCodingZone.CodeRules --filter "FullyQualifiedName~SERVICES_RULE_005"
```
