# SERVICES_RULE_003 · Inject multiple implementations as IEnumerable<T>

> Example documentation for the talk. In a real project this page holds the decision behind the rule,
> its owner and the history of known exceptions. Every finding links here (`Docs` in the Human output,
> `documentation` in the AI output).

| | |
|---|---|
| **Id** | `SERVICES_RULE_003` |
| **Group** | Services |
| **Tags** | `DependencyInjection` |
| **Stage 2** | [`SERVICES_RULE_003.cs`](../../src/MyCodingZone.CodeRules.Test/Services/SERVICES_RULE_003.cs) |
| **Stage 5** | [`InjectMultipleImplementationsAsIEnumerable`](../../src/MyCodingZone.CodeRules/Services/InjectMultipleImplementationsAsIEnumerable.cs) · tests: [`InjectMultipleImplementationsAsIEnumerable_Tests.cs`](../../src/MyCodingZone.CodeRules.Engine.Test/Services/InjectMultipleImplementationsAsIEnumerable_Tests.cs) |

## Why

Microsoft.Extensions.DependencyInjection resolves all registered implementations of T only as `IEnumerable<T>`. Any other collection type in a constructor fails at runtime with 'Unable to resolve service'.

## How to fix

Replace the collection type in the constructor with `IEnumerable<T>`.

The finding points at the constructor parameter, not at the class.

## Example

Violates the rule:

```csharp
internal sealed class EndpointMapper(IReadOnlyList<IEndpointRegistration> registrations) : IEndpointMapper
```

Complies with the rule:

```csharp
internal sealed class EndpointMapper(IEnumerable<IEndpointRegistration> registrations) : IEndpointMapper
```

## Accepting a single finding

Stage 5 (Code Rule Engine) only, always with a reason and the approval of a human (see [AGENTS.md](../../AGENTS.md)):

```csharp
[SuppressMessage("CodeRules", "SERVICES_RULE_003", Justification = "...")]
```

## Verify the fix

```bash
dotnet test src/MyCodingZone.CodeRules.Test --filter "FullyQualifiedName~SERVICES_RULE_003"
dotnet test src/MyCodingZone.CodeRules --filter "FullyQualifiedName~SERVICES_RULE_003"
```

## Read more

- https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-registration-methods
