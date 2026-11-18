using System.Collections.Immutable;

namespace MyCodingZone.CodeRules.Records
{
    /// <summary>Stage 2 counterpart: src/MyCodingZone.CodeRules.Test/Records/RECORDS_RULE_002.cs</summary>
    public sealed class RecordsMustNotImplementMethods : ICodeRule
    {
        public string Id => "RECORDS_RULE_002";

        public string Title => "Records must not implement methods";

        public string Why => "Records are designed for data transfer and value objects. Mixing data and logic violates the separation of concerns " +
                             "and makes the code harder to test and maintain.";

        public string HowToFix => "Move methods to a separate service class or static helper class.";

        public ImmutableList<string> Tags => ["Records"];

        public IEnumerable<Finding> Evaluate(CodeContext context)
        {
            return from record in context.Records
                   from method in record.Methods
                   select new Finding
                   {
                       What = method.FullyQualifiedName,
                       Where = method.Location,
                       Current = method.SourceCode,
                       HowToFix = $"Move the method '{method.Name}' to a separate service class or static helper.",
                       Details = [("Record", record.Name), ("Method", $"{method.ReturnParameter} {method.Name}(...)")]
                   };
        }
    }
}
