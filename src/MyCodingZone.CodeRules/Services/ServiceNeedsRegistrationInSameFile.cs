using System.Collections.Immutable;

namespace MyCodingZone.CodeRules.Services
{
    /// <summary>Stage 2 counterpart: src/MyCodingZone.CodeRules.Tests/Services/SERVICES_RULE_001.cs</summary>
    public sealed class ServiceNeedsRegistrationInSameFile : IRule<ServiceContext>
    {
        public string Id => "SERVICES_RULE_001";

        public string Title => "Each service needs a registration extension in the same file";

        public string Why => "Whoever uses a service calls exactly one registration method and gets everything the service needs. " +
                             "The registration next to the service keeps both in sync; a service registered somewhere else " +
                             "is easily forgotten and only fails at runtime with 'Unable to resolve service'.";

        public string HowToFix => "Add a static 'Add<Service>Extension' class with an 'Add<Service>(this IServiceCollection services)' method to the file of the service.";

        public ImmutableList<string> Tags => ["DependencyInjection"];

        public IEnumerable<Finding> Evaluate(ServiceContext context)
        {
            return from service in context.Services
                   where service.Registration is null
                   select new Finding
                   {
                       What = service.Class.FullyQualifiedName,
                       // Current must be the source at Where: the wrongly named registration if there is one, otherwise the service
                       Where = service.WrongNameRegistration?.Location ?? service.Class.Location,
                       Current = service.WrongNameRegistration?.SourceCode,
                       ShouldBe = SampleRegistrationClass(service),
                       HowToFix = service.WrongNameRegistration is null
                                      ? $"Add a static class 'Add{service.Class.Name}Extension' with an 'Add{service.Class.Name}(this IServiceCollection services)' method to '{Path.GetFileName(service.Class.FilePath)}'."
                                      : $"Rename '{service.WrongNameRegistration.Name}' to 'Add{service.Class.Name}Extension', the registration has to be named after the service.",
                       Details = [("Service", service.Class.Name)]
                   };
        }

        private static string SampleRegistrationClass(Service service)
        {
            var name = service.Class.Name;

            return $$"""
                     internal static class Add{{name}}Extension
                     {
                         internal static void Add{{name}}(this IServiceCollection services)
                         {
                             {{service.Class.SampleRegistration()}}
                         }
                     }
                     """;
        }
    }
}
