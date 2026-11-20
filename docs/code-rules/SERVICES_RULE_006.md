# SERVICES_RULE_006 · Services inject at most 5 dependencies

> Example documentation for the talk. In a real project this page holds the decision behind the rule,
> its owner and the history of known exceptions. Every finding links here (`Docs` in the Human output,
> `documentation` in the AI output).

| | |
|---|---|
| **Id** | `SERVICES_RULE_006` |
| **Group** | Services |
| **Tags** | `DependencyInjection` |
| **Stage 2** | [`SERVICES_RULE_006.cs`](../../src/MyCodingZone.CodeRules.Test/Services/SERVICES_RULE_006.cs) |
| **Stage 5** | [`ServicesInjectAtMostFiveDependencies`](../../src/MyCodingZone.CodeRules/Services/ServicesInjectAtMostFiveDependencies.cs) · tests: [`ServicesInjectAtMostFiveDependencies_Tests.cs`](../../src/MyCodingZone.CodeRules.Engine.Test/Services/ServicesInjectAtMostFiveDependencies_Tests.cs) |

## Why

Many constructor dependencies are a strong hint that a service does too much (Single Responsibility Principle). Such services are hard to test, because every test has to set up all dependencies.

## How to fix

Split the service or group dependencies that belong together.

The constructor with the most parameters counts. The finding names the number of dependencies in `Details`.

## Example

Violates the rule:

```csharp
internal sealed class OrderService(IOrderRepository orders, ICustomerRepository customers,
                                    IPricing pricing, ITaxCalculator tax,
                                    IMailer mailer, IAuditLog audit) : IOrderService
```

Complies with the rule:

```csharp
internal sealed class OrderService(IOrderRepository orders, ICustomerRepository customers,
                                    IOrderPricing pricing, IOrderNotifications notifications) : IOrderService
```

## Accepting a single finding

Stage 5 (Code Rule Engine) only, always with a reason and the approval of a human (see [AGENTS.md](../../AGENTS.md)):

```csharp
[SuppressMessage("CodeRules", "SERVICES_RULE_006", Justification = "...")]
```

## Verify the fix

```bash
dotnet test src/MyCodingZone.CodeRules.Test --filter "FullyQualifiedName~SERVICES_RULE_006"
dotnet test src/MyCodingZone.CodeRules --filter "FullyQualifiedName~SERVICES_RULE_006"
```
