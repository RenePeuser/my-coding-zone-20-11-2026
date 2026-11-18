using System.Collections.Immutable;

namespace MyCodingZone.CodeRules.Services
{
    /// <summary>Stage 2 counterpart: src/MyCodingZone.CodeRules.Tests/Services/SERVICES_RULE_003.cs</summary>
    public sealed class InjectMultipleImplementationsAsIEnumerable : IRule<ServiceContext>
    {
        // Collection types Microsoft.Extensions.DependencyInjection cannot resolve as "all implementations of T"
        private static readonly ImmutableHashSet<string> UnsupportedCollectionTypes =
        [
            "List", "IList", "IReadOnlyList", "ICollection", "IReadOnlyCollection", "Collection", "ReadOnlyCollection", "ObservableCollection",
            "HashSet", "ISet", "IReadOnlySet", "SortedSet", "LinkedList", "Queue", "Stack",
            "ImmutableList", "IImmutableList", "ImmutableArray", "ImmutableHashSet", "IImmutableSet", "ImmutableSortedSet", "ImmutableQueue", "ImmutableStack",
            "FrozenSet"
        ];

        public string Id => "SERVICES_RULE_003";

        public string Title => "Inject multiple implementations as IEnumerable<T>";

        public string Why => "Microsoft.Extensions.DependencyInjection resolves all registered implementations of T only as IEnumerable<T>. " +
                             "Any other collection type in a constructor fails at runtime with 'Unable to resolve service'.";

        public string HowToFix => "Replace the collection type in the constructor with IEnumerable<T>.";

        public ImmutableList<string> Tags => ["DependencyInjection"];

        public Uri ReadMoreUrl => new("https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-registration-methods");

        public IEnumerable<Finding> Evaluate(ServiceContext context)
        {
            return from service in context.Services
                   from parameter in service.Class.ConstructorParameters()
                   let collectionType = CollectionType(parameter.Type)
                   where collectionType is not null
                   let elementType = ElementType(parameter.Type)
                   select new Finding
                   {
                       What = $"{service.Class.FullyQualifiedName}({parameter.Type} {parameter.Name})",
                       // The engine replaces exactly the reported span with ShouldBe, so report the parameter, not the class
                       Where = parameter.Location,
                       Current = parameter.SourceCode,
                       ShouldBe = $"IEnumerable<{elementType}> {parameter.Name}",
                       HowToFix = $"Inject 'IEnumerable<{elementType}>' and convert it inside '{service.Class.Name}' if you need a '{collectionType}'."
                   };
        }

        private static string? CollectionType(string type)
        {
            if (type.EndsWith("[]", StringComparison.Ordinal))
            {
                return "array";
            }

            var genericStart = type.IndexOf('<', StringComparison.Ordinal);
            var outerType = genericStart < 0 ? type : type[..genericStart];

            return UnsupportedCollectionTypes.Contains(outerType) ? outerType : null;
        }

        private static string ElementType(string type)
        {
            if (type.EndsWith("[]", StringComparison.Ordinal))
            {
                return type[..^2];
            }

            var genericStart = type.IndexOf('<', StringComparison.Ordinal);

            return genericStart < 0 ? "T" : type[(genericStart + 1)..^1];
        }
    }
}
