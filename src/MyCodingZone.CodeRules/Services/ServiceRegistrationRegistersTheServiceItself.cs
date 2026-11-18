using System.Collections.Immutable;
using Solution.Parser.CSharp;

namespace MyCodingZone.CodeRules.Services
{
    /// <summary>Stage 2 counterpart: src/MyCodingZone.CodeRules.Test/Services/SERVICES_RULE_007.cs</summary>
    public sealed class ServiceRegistrationRegistersTheServiceItself : IRule<ServiceContext>
    {
        public string Id => "SERVICES_RULE_007";

        public string Title => "Each service registration has to register the service itself";

        public string Why => "Whoever uses a service calls only its registration. A registration that exists and is called, " +
                             "but does not register the service (empty or commented out), compiles and passes every other check, " +
                             "yet fails at runtime with 'Unable to resolve service' or silently drops the feature (e.g. a missing endpoint).";

        public string HowToFix => "Register the service in its 'Add<Service>' method, e.g. 'services.AddSingleton<IService, Service>();'.";

        public ImmutableList<string> Tags => ["DependencyInjection"];

        public IEnumerable<Finding> Evaluate(ServiceContext context)
        {
            return from service in context.Services
                   let registration = service.Registration
                   where registration is not null
                   let registrationMethods = registration.Methods.Where(method => method.IsServiceRegistration()).ToImmutableList()
                   where !registrationMethods.IsEmpty
                   where !registrationMethods.Any(method => RegistersService(method, service.Class.Name))
                   from registrationMethod in registrationMethods
                   select new Finding
                   {
                       What = $"{registration.FullyQualifiedName}.{registrationMethod.Name}",
                       Where = registrationMethod.Location,
                       Current = registrationMethod.SourceCode,
                       ShouldBe = service.Class.SampleRegistration(),
                       HowToFix = $"Register '{service.Class.Name}' in '{registrationMethod.Name}', e.g. '{service.Class.SampleRegistration()}'. Commented out registrations do not count.",
                       Details = [("Service", service.Class.Name)]
                   };
        }

        // services.AddSingleton<IFoo, Foo>() / services.AddSingleton<Foo>() / services.AddHostedService<Foo>() - comments are ignored
        private static bool RegistersService(Method method,
                                             string service)
        {
            var code = string.Join(Environment.NewLine,
                                   method.Statements
                                         .SelectMany(statement => statement.Split('\n'))
                                         .Select(line => line.Trim())
                                         .Where(line => !line.StartsWith("//", StringComparison.Ordinal)));

            return code.Contains($"<{service}>", StringComparison.Ordinal) ||
                   code.Contains($"<{service},", StringComparison.Ordinal) ||
                   code.Contains($", {service}>", StringComparison.Ordinal);
        }
    }
}
