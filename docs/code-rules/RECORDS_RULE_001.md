# RECORDS_RULE_001 · Record properties must be immutable

> Example documentation for the talk. In a real project this page holds the decision behind the rule,
> its owner and the history of known exceptions. Every finding links here (`Docs` in the Human output,
> `documentation` in the AI output).

| | |
|---|---|
| **Id** | `RECORDS_RULE_001` |
| **Group** | Records |
| **Tags** | `Records` |
| **Stage 2** | [`RECORDS_RULE_001.cs`](../../src/MyCodingZone.CodeRules.Test/Records/RECORDS_RULE_001.cs) |
| **Stage 5** | [`RecordPropertiesMustBeImmutable`](../../src/MyCodingZone.CodeRules/Records/RecordPropertiesMustBeImmutable.cs) · tests: [`RecordPropertiesMustBeImmutable_Tests.cs`](../../src/MyCodingZone.CodeRules.Engine.Test/Records/RecordPropertiesMustBeImmutable_Tests.cs) |

## Why

A record is a snapshot of a state, e.g. data loaded from the database. It must not change while it is being processed. A setter lets any code modify the same instance, so other parts that hold a reference suddenly see different data. A change must produce a new instance instead.

## How to fix

Replace `set;` with `init;` and create a modified copy with a `with` expression, e.g. `order with { Status = OrderStatus.Shipped }`.

The finding carries `Current` and `ShouldBe`, so a fix can replace the property declaration directly.

## Example

Violates the rule:

```csharp
public record CreateUserRequest
{
    public required string Email { get; set; }
}
```

Complies with the rule:

```csharp
public record CreateUserRequest
{
    public required string Email { get; init; }
}

var changed = request with { Email = "new@example.com" };
```

## Accepting a single finding

Stage 5 (Code Rule Engine) only, always with a reason and the approval of a human (see [AGENTS.md](../../AGENTS.md)):

```csharp
[SuppressMessage("CodeRules", "RECORDS_RULE_001", Justification = "...")]
```

## Verify the fix

```bash
dotnet test src/MyCodingZone.CodeRules.Test --filter "FullyQualifiedName~RECORDS_RULE_001"
dotnet test src/MyCodingZone.CodeRules --filter "FullyQualifiedName~RECORDS_RULE_001"
```

## Read more

- https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record#init-only-properties
