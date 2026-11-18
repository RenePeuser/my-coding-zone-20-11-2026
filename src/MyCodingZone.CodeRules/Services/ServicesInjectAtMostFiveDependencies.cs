using System.Collections.Immutable;

namespace MyCodingZone.CodeRules.Services
{
    /// <summary>Stage 2 counterpart: src/MyCodingZone.CodeRules.Test/Services/SERVICES_RULE_006.cs</summary>
    public sealed class ServicesInjectAtMostFiveDependencies : IRule<ServiceContext>
    {
        private const int MaxDependencies = 5;

        public string Id => "SERVICES_RULE_006";

        public string Title => "Services inject at most 5 dependencies";

        public string Why => "Many constructor dependencies are a strong hint that a service does too much (Single Responsibility Principle). " +
                             "Such services are hard to test, because every test has to set up all dependencies.";

        public string HowToFix => "Split the service or group dependencies that belong together.";

        public ImmutableList<string> Tags => ["DependencyInjection"];

        public IEnumerable<Finding> Evaluate(ServiceContext context)
        {
            // Primary constructor or the classic constructor with the most parameters
            return from service in context.Services
                   let parameters = service.Class.Constructors
                                           .Select(constructor => constructor.Parameters)
                                           .Append(service.Class.Parameters)
                                           .MaxBy(parameters => parameters.Count)
                   where parameters is not null && parameters.Count > MaxDependencies
                   select new Finding
                   {
                       What = service.Class.FullyQualifiedName,
                       Where = service.Class.Location,
                       HowToFix = $"'{service.Class.Name}' injects {parameters.Count} dependencies. Split it into smaller services with one responsibility each, or group dependencies that are always used together behind one service.",
                       // Current would have to be the source of the whole class, so the constructor goes into the details
                       Details = [("Dependencies", $"{parameters.Count} (max {MaxDependencies})"),
                                  ("Constructor", $"{service.Class.Name}({string.Join(", ", parameters.Select(parameter => $"{parameter.Type} {parameter.Name}"))})")]
                   };
        }
    }
}
