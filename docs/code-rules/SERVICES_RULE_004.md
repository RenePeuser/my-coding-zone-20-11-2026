# SERVICES_RULE_004 · A service registration registers the dependencies of the service

> Example documentation for the talk. In a real project this page holds the decision behind the rule,
> its owner and the history of known exceptions. Every finding links here (`Docs` in the Human output,
> `documentation` in the AI output).

| | |
|---|---|
| **Id** | `SERVICES_RULE_004` |
| **Group** | Services |
| **Tags** | `DependencyInjection` |
| **Stage 2** | [`SERVICES_RULE_004.cs`](../../src/MyCodingZone.CodeRules.Test/Services/SERVICES_RULE_004.cs) |
| **Stage 5** | [`ServiceRegistrationRegistersDependencies`](../../src/MyCodingZone.CodeRules/Services/ServiceRegistrationRegistersDependencies.cs) · tests: [`ServiceRegistrationRegistersDependencies_Tests.cs`](../../src/MyCodingZone.CodeRules.Engine.Test/Services/ServiceRegistrationRegistersDependencies_Tests.cs) |

## Why

Whoever uses a service calls only its registration. If that registration does not bring the dependencies along, every caller has to know the internals of the service, and a forgotten dependency only fails at runtime with 'Unable to resolve service'.

## How to fix

Call the registration of each dependency from the registration of the service.

## Example

Violates the rule:

```csharp
internal static void AddUserService(this IServiceCollection services)
{
    // UserService needs IUserRepository, but its registration is missing
    services.AddSingleton<IUserService, UserService>();
}
```

Complies with the rule:

```csharp
internal static void AddUserService(this IServiceCollection services)
{
    services.AddUserRepository();
    services.AddSingleton<IUserService, UserService>();
}
```

## Accepting a single finding

Stage 5 (Code Rule Engine) only, always with a reason and the approval of a human (see [AGENTS.md](../../AGENTS.md)):

```csharp
[SuppressMessage("CodeRules", "SERVICES_RULE_004", Justification = "...")]
```

## Verify the fix

```bash
dotnet test src/MyCodingZone.CodeRules.Test --filter "FullyQualifiedName~SERVICES_RULE_004"
dotnet test src/MyCodingZone.CodeRules --filter "FullyQualifiedName~SERVICES_RULE_004"
```
