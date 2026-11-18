using System.Collections.Immutable;

namespace MyCodingZone.CodeRules.Services
{
    /// <summary>
    /// Stage 2 counterpart: src/MyCodingZone.CodeRules.Test/Services/SERVICES_RULE_005.cs.
    /// Needs no service model, so it evaluates the plain CodeContext.
    /// </summary>
    public sealed class ServiceRegistrationsReturnVoid : ICodeRule
    {
        public string Id => "SERVICES_RULE_005";

        public string Title => "Service registrations return void";

        public string Why => "One style for all registrations: a registration is a statement, not a builder. " +
                             "Returning IServiceCollection invites long fluent chains in which a missing or duplicate registration is hard to spot in a review.";

        public string HowToFix => "Return 'void' from the registration and call each registration as its own statement.";

        public ImmutableList<string> Tags => ["DependencyInjection"];

        public IEnumerable<Finding> Evaluate(CodeContext context)
        {
            return from @class in context.Classes
                   where @class.IsStatic
                   from method in @class.Methods
                   where method.IsStatic &&
                         method.Name.StartsWith("Add", StringComparison.Ordinal) &&
                         method.IsServiceRegistration() &&
                         method.ReturnParameter != "void"
                   select new Finding
                   {
                       What = $"{@class.FullyQualifiedName}.{method.Name}",
                       Where = method.Location,
                       Current = method.SourceCode,
                       HowToFix = $"Change the return type of '{method.Name}' from '{method.ReturnParameter}' to 'void' and remove the 'return'.",
                       Details = [("Returns", method.ReturnParameter)]
                   };
        }
    }
}
