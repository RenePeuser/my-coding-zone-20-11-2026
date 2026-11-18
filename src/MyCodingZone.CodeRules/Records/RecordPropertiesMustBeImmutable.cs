using System.Collections.Immutable;

namespace MyCodingZone.CodeRules.Records
{
    /// <summary>Stage 2 counterpart: src/MyCodingZone.CodeRules.Tests/Records/RECORDS_RULE_001.cs</summary>
    public sealed class RecordPropertiesMustBeImmutable : ICodeRule
    {
        public string Id => "RECORDS_RULE_001";

        public string Title => "Record properties must be immutable";

        public string Why => "A record is a snapshot of a state, e.g. data loaded from the database. " +
                             "It must not change while it is being processed. " +
                             "A setter lets any code modify the same instance, so other parts that hold a " +
                             "reference suddenly see different data. " +
                             "A change must produce a new instance instead.";

        public string HowToFix => "Replace 'set;' with 'init;' and create a modified copy with a 'with' expression, e.g. 'order with { Status = OrderStatus.Shipped }'.";

        public ImmutableList<string> Tags => ["Records"];

        public Uri ReadMoreUrl => new("https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record#init-only-properties");

        public IEnumerable<Finding> Evaluate(CodeContext context)
        {
            return from record in context.Records
                   from property in record.Properties
                   where !property.IsReadOnly
                   select new Finding
                   {
                       What = property.FullyQualifiedName,
                       Where = property.Location,
                       Current = property.SourceCode,
                       ShouldBe = property.SourceCode.Replace("set;", "init;", StringComparison.Ordinal),
                       Details = [("Property", $"{property.Type} {property.Name}")]
                   };
        }
    }
}
