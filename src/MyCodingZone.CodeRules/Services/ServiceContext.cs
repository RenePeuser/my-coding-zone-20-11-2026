using System.Collections.Immutable;
using Solution.Parser.CSharp;
using CSharpSyntaxTree = Solution.Parser.CSharp.CSharpSyntaxTree;

namespace MyCodingZone.CodeRules.Services
{
    /// <summary>
    /// A productive class that is resolved via dependency injection, together with the registration extension
    /// found in the same file.
    /// </summary>
    public sealed record Service(Class Class,
                                 CSharpSyntaxTree SyntaxTree,
                                 Class? Registration,
                                 Class? WrongNameRegistration);

    /// <summary>
    /// The derived model all SERVICES rules share. Stage 2 needed a base class with a static Lazy (ServiceCodeRuleBase);
    /// here the engine creates the context once per run - one public constructor taking other contexts, no registration.
    /// Heuristic, the model is syntactic: a service is a non static, non abstract productive class with methods,
    /// that implements interfaces only (or nothing) and is never created with 'new'.
    /// Classes in a 'Startup' folder are the composition root, not services.
    /// </summary>
    public sealed class ServiceContext(CodeContext code) : IRuleContext
    {
        public static string Group => "Services";

        public CodeContext Code { get; } = code;

        public ImmutableList<Service> Services { get; } = [.. FindServices(code)];

        /// <summary>Every extension method on IServiceCollection in the productive code.</summary>
        public ImmutableList<Method> RegistrationMethods { get; } = [.. code.ProductionTrees.SelectMany(tree => tree.Methods).Where(method => method.IsServiceRegistration())];

        private static IEnumerable<Service> FindServices(CodeContext code)
        {
            // Split every class once up front instead of once per (service, class) pair
            var classesWithLines = code.Classes.Select(@class => (Class: @class, Lines: @class.SourceCode.Split(Environment.NewLine))).ToImmutableArray();

            foreach (var syntaxTree in code.ProductionTrees)
            {
                var services = syntaxTree.Classes.Where(@class => !@class.Modifiers.Any(modifier => modifier is Modifier.Static or Modifier.Abstract) &&
                                                                  @class.Methods.Any() &&
                                                                  (@class.BaseTypes.Count == 0 || @class.BaseTypes.All(baseType => baseType.TypeName.StartsWith('I'))));

                foreach (var service in services)
                {
                    if (new FileInfo(service.FilePath).Directory!.Name.Contains("startup", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // new Service() or Service service = new(); -> not resolved via dependency injection
                    var isDirectlyInstantiated = classesWithLines.Any(c => c.Class.SourceCode.Contains($"new {service.Name}(", StringComparison.Ordinal) ||
                                                                           c.Lines.Any(line => line.Contains(service.Name, StringComparison.Ordinal) && line.Contains(" = new(", StringComparison.Ordinal)));

                    if (isDirectlyInstantiated)
                    {
                        continue;
                    }

                    var registration = syntaxTree.Classes.FirstOrDefault(@class => @class.Name.Contains(service.Name, StringComparison.Ordinal) &&
                                                                                   @class.Name.Contains("Extension", StringComparison.Ordinal));

                    var wrongNameRegistration = (from @class in syntaxTree.Classes
                                                 from method in @class.Methods
                                                 where method.IsServiceRegistration() &&
                                                       (method.Body.Contains($"<{service.Name}", StringComparison.Ordinal) || method.Body.Contains($"{service.Name}>", StringComparison.Ordinal))
                                                 select @class).FirstOrDefault();

                    yield return new Service(service, syntaxTree, registration, wrongNameRegistration);
                }
            }
        }
    }

    internal static class ServiceRegistrationExtensions
    {
        /// <summary>An extension method on IServiceCollection, e.g. 'AddUserService(this IServiceCollection services)'.</summary>
        internal static bool IsServiceRegistration(this Method method)
        {
            return method.Parameters.Any(parameter => parameter.Type == "IServiceCollection");
        }

        /// <summary>Primary constructor parameters plus the parameters of every classic constructor.</summary>
        internal static IEnumerable<Parameter> ConstructorParameters(this Class @class)
        {
            return @class.Parameters.Concat(@class.Constructors.Where(constructor => !constructor.IsPrimary).SelectMany(constructor => constructor.Parameters));
        }

        internal static string SampleRegistration(this Class service)
        {
            var serviceInterface = service.BaseTypes.Select(baseType => baseType.TypeName).FirstOrDefault();

            return serviceInterface is null ? $"services.AddSingleton<{service.Name}>();" : $"services.AddSingleton<{serviceInterface}, {service.Name}>();";
        }
    }
}
