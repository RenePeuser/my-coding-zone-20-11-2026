# SERVICES_RULE_007 · Each service registration has to register the service itself

> Example documentation for the talk. In a real project this page holds the decision behind the rule,
> its owner and the history of known exceptions. Every finding links here (`Docs` in the Human output,
> `documentation` in the AI output).

| | |
|---|---|
| **Id** | `SERVICES_RULE_007` |
| **Group** | Services |
| **Tags** | `DependencyInjection` |
| **Stage 2** | [`SERVICES_RULE_007.cs`](../../src/MyCodingZone.CodeRules.Test/Services/SERVICES_RULE_007.cs) |
| **Stage 5** | [`ServiceRegistrationRegistersTheServiceItself`](../../src/MyCodingZone.CodeRules/Services/ServiceRegistrationRegistersTheServiceItself.cs) · tests: [`ServiceRegistrationRegistersTheServiceItself_Tests.cs`](../../src/MyCodingZone.CodeRules.Engine.Test/Services/ServiceRegistrationRegistersTheServiceItself_Tests.cs) |

## Why

Whoever uses a service calls only its registration. A registration that exists and is called, but does not register the service (empty or commented out), compiles and passes every other check, yet fails at runtime with 'Unable to resolve service' or silently drops the feature (e.g. a missing endpoint).

## How to fix

Register the service in its `Add<Service>` method, e.g. `services.AddSingleton<IService, Service>();`.

## Example

Violates the rule:

```csharp
internal static void AddCreateUserEndpoint(this IServiceCollection services)
{
    // services.AddSingleton<IEndpointRegistration, CreateUserEndpoint>();
}
```

Complies with the rule:

```csharp
internal static void AddCreateUserEndpoint(this IServiceCollection services)
{
    services.AddSingleton<IEndpointRegistration, CreateUserEndpoint>();
}
```

## Accepting a single finding

Stage 5 (Code Rule Engine) only, always with a reason and the approval of a human (see [AGENTS.md](../../AGENTS.md)):

```csharp
[SuppressMessage("CodeRules", "SERVICES_RULE_007", Justification = "...")]
```

## Verify the fix

```bash
dotnet test src/MyCodingZone.CodeRules.Test --filter "FullyQualifiedName~SERVICES_RULE_007"
dotnet test src/MyCodingZone.CodeRules --filter "FullyQualifiedName~SERVICES_RULE_007"
```
