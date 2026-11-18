using System.Collections.Immutable;
using Solution.Parser.CSharp;

namespace MyCodingZone.CodeRules.Services
{
    /// <summary>Stage 2 counterpart: src/MyCodingZone.CodeRules.Test/Services/SERVICES_RULE_004.cs</summary>
    public sealed class ServiceRegistrationRegistersDependencies : IRule<ServiceContext>
    {
        public string Id => "SERVICES_RULE_004";

        public string Title => "A service registration registers the dependencies of the service";

        public string Why => "Whoever uses a service calls only its registration. If that registration does not bring the dependencies along, " +
                             "every caller has to know the internals of the service, and a forgotten dependency only fails at runtime with 'Unable to resolve service'.";

        public string HowToFix => "Call the registration of each dependency from the registration of the service.";

        public ImmutableList<string> Tags => ["DependencyInjection"];

        public IEnumerable<Finding> Evaluate(ServiceContext context)
        {
            // Only dependencies declared in this solution. Framework types (ILogger<T>, IOptions<T>, HttpClient, ...)
            // are registered by the host. IEnumerable<T> collects whatever the features register, the collector must not know them.
            var declaredTypes = context.Code.Classes.Select(type => type.Name)
                                       .Concat(context.Code.Interfaces.Select(type => type.Name))
                                       .Concat(context.Code.Records.Select(type => type.Name))
                                       .ToImmutableHashSet(StringComparer.Ordinal);

            var registrationMethods = context.RegistrationMethods;

            return from service in context.Services
                   let registration = service.Registration
                   where registration is not null
                   from registrationMethod in registration.Methods
                   where registrationMethod.IsServiceRegistration()
                   from parameter in service.Class.ConstructorParameters()
                   let dependency = parameter.Type.TrimEnd('?')
                   where declaredTypes.Contains(dependency)
                   where !Registers(registrationMethod, dependency, [])
                   let providingRegistration = registrationMethods.FirstOrDefault(method => RegistersDirectly(method, dependency))
                   select new Finding
                   {
                       What = $"{service.Class.FullyQualifiedName} → {dependency}",
                       Where = registrationMethod.Location,
                       Current = registrationMethod.SourceCode,
                       ShouldBe = providingRegistration is null ? null : $"services.{providingRegistration.Name}();",
                       HowToFix = providingRegistration is null
                                      ? $"'{dependency}' has no registration yet. Add one next to '{dependency}' (SERVICES_RULE_001) and call it from '{registrationMethod.Name}'."
                                      : $"Call 'services.{providingRegistration.Name}()' in '{registrationMethod.Name}'.",
                       Details = [("Service", service.Class.Name), ("Dependency", $"{parameter.Type} {parameter.Name}")]
                   };

            // Directly, or through a registration it calls (transitive)
            bool Registers(Method method,
                           string dependency,
                           ImmutableHashSet<string> visited)
            {
                if (RegistersDirectly(method, dependency))
                {
                    return true;
                }

                return registrationMethods.Where(called => !visited.Contains(called.FullyQualifiedName) &&
                                                           method.Body.Contains($".{called.Name}(", StringComparison.Ordinal))
                                          .Any(called => Registers(called, dependency, visited.Add(method.FullyQualifiedName)));
            }
        }

        // services.AddSingleton<IFoo, Foo>() / services.AddSingleton<Foo>()
        private static bool RegistersDirectly(Method method,
                                              string dependency)
        {
            return method.Body.Contains($"<{dependency}>", StringComparison.Ordinal) ||
                   method.Body.Contains($"<{dependency},", StringComparison.Ordinal) ||
                   method.Body.Contains($", {dependency}>", StringComparison.Ordinal);
        }
    }
}
