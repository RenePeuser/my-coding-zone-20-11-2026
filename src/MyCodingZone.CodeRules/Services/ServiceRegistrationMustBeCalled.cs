using System.Collections.Immutable;

namespace MyCodingZone.CodeRules.Services
{
    /// <summary>Stage 2 counterpart: src/MyCodingZone.CodeRules.Test/Services/SERVICES_RULE_002.cs</summary>
    public sealed class ServiceRegistrationMustBeCalled : IRule<ServiceContext>
    {
        public string Id => "SERVICES_RULE_002";

        public string Title => "Each service registration has to be called";

        public string Why => "A registration that is never called registers nothing. The application compiles, " +
                             "but fails at runtime as soon as the service is resolved.";

        public string HowToFix => "Call the registration from the registration of the parent feature or from Program.cs.";

        public ImmutableList<string> Tags => ["DependencyInjection"];

        public IEnumerable<Finding> Evaluate(ServiceContext context)
        {
            // Registrations are called from other registrations or from the top-level statements in Program.cs
            var calls = context.Code.AllTrees.SelectMany(tree => tree.Methods)
                               .SelectMany(method => method.Statements)
                               .Concat(context.Code.ProductionTrees.Where(tree => tree.FileName.EndsWith("Program.cs", StringComparison.Ordinal))
                                              .Select(tree => tree.SourceCode))
                               .ToImmutableList();

            return from service in context.Services
                   let registration = service.Registration
                   where registration is not null
                   from registrationMethod in registration.Methods
                   where registrationMethod.IsServiceRegistration()
                   where !calls.Any(call => call.Contains($".{registrationMethod.Name}(", StringComparison.Ordinal))
                   select new Finding
                   {
                       What = $"{registration.FullyQualifiedName}.{registrationMethod.Name}",
                       Where = registrationMethod.Location,
                       Current = registrationMethod.SourceCode,
                       ShouldBe = $"services.{registrationMethod.Name}();",
                       HowToFix = $"Call 'services.{registrationMethod.Name}()' from the registration of the feature that uses '{service.Class.Name}', or from Program.cs.",
                       Details = [("Service", service.Class.Name)]
                   };
        }
    }
}
