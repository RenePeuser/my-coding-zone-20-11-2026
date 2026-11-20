# SERVICES_RULE_001 · Each service needs a registration extension in the same file

> Example documentation for the talk. In a real project this page holds the decision behind the rule,
> its owner and the history of known exceptions. Every finding links here (`Docs` in the Human output,
> `documentation` in the AI output).

| | |
|---|---|
| **Id** | `SERVICES_RULE_001` |
| **Group** | Services |
| **Tags** | `DependencyInjection` |
| **Stage 2** | [`SERVICES_RULE_001.cs`](../../src/MyCodingZone.CodeRules.Test/Services/SERVICES_RULE_001.cs) |
| **Stage 5** | [`ServiceNeedsRegistrationInSameFile`](../../src/MyCodingZone.CodeRules/Services/ServiceNeedsRegistrationInSameFile.cs) · tests: [`ServiceNeedsRegistrationInSameFile_Tests.cs`](../../src/MyCodingZone.CodeRules.Engine.Test/Services/ServiceNeedsRegistrationInSameFile_Tests.cs) |

## Why

Whoever uses a service calls exactly one registration method and gets everything the service needs. The registration next to the service keeps both in sync; a service registered somewhere else is easily forgotten and only fails at runtime with 'Unable to resolve service'.

## How to fix

Add a static `Add<Service>Extension` class with an `Add<Service>(this IServiceCollection services)` method to the file of the service.

A service is a non static, non abstract productive class with methods that implements interfaces only and is never created with `new`. Classes in a `Startup` folder are the composition root.

## Example

Violates the rule:

```csharp
// UserService.cs
internal sealed class UserService : IUserService
{
    public User Get(Guid id) => ...;
}
```

Complies with the rule:

```csharp
// UserService.cs
internal static class AddUserServiceExtension
{
    internal static void AddUserService(this IServiceCollection services)
    {
        services.AddSingleton<IUserService, UserService>();
    }
}

internal sealed class UserService : IUserService
{
    public User Get(Guid id) => ...;
}
```

## Accepting a single finding

Stage 5 (Code Rule Engine) only, always with a reason and the approval of a human (see [AGENTS.md](../../AGENTS.md)):

```csharp
[SuppressMessage("CodeRules", "SERVICES_RULE_001", Justification = "...")]
```

## Verify the fix

```bash
dotnet test src/MyCodingZone.CodeRules.Test --filter "FullyQualifiedName~SERVICES_RULE_001"
dotnet test src/MyCodingZone.CodeRules --filter "FullyQualifiedName~SERVICES_RULE_001"
```
