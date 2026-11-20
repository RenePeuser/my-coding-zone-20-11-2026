# RECORDS_RULE_002 · Records must not implement methods

> Example documentation for the talk. In a real project this page holds the decision behind the rule,
> its owner and the history of known exceptions. Every finding links here (`Docs` in the Human output,
> `documentation` in the AI output).

| | |
|---|---|
| **Id** | `RECORDS_RULE_002` |
| **Group** | Records |
| **Tags** | `Records` |
| **Stage 2** | [`RECORDS_RULE_002.cs`](../../src/MyCodingZone.CodeRules.Test/Records/RECORDS_RULE_002.cs) |
| **Stage 5** | [`RecordsMustNotImplementMethods`](../../src/MyCodingZone.CodeRules/Records/RecordsMustNotImplementMethods.cs) · tests: [`RecordsMustNotImplementMethods_Tests.cs`](../../src/MyCodingZone.CodeRules.Engine.Test/Records/RecordsMustNotImplementMethods_Tests.cs) |

## Why

Records are designed for data transfer and value objects. Mixing data and logic violates the separation of concerns and makes the code harder to test and maintain.

## How to fix

Move methods to a separate service class or static helper class.

## Example

Violates the rule:

```csharp
public record Order(decimal Net)
{
    public decimal Gross() => Net * 1.19m;
}
```

Complies with the rule:

```csharp
public record Order(decimal Net);

public static class OrderPricing
{
    public static decimal Gross(Order order) => order.Net * 1.19m;
}
```

## Accepting a single finding

Stage 5 (Code Rule Engine) only, always with a reason and the approval of a human (see [AGENTS.md](../../AGENTS.md)):

```csharp
[SuppressMessage("CodeRules", "RECORDS_RULE_002", Justification = "...")]
```

## Verify the fix

```bash
dotnet test src/MyCodingZone.CodeRules.Test --filter "FullyQualifiedName~RECORDS_RULE_002"
dotnet test src/MyCodingZone.CodeRules --filter "FullyQualifiedName~RECORDS_RULE_002"
```
