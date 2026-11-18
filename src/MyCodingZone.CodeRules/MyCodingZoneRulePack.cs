using MyCodingZone.CodeRules.Projects;
using MyCodingZone.CodeRules.Records;
using MyCodingZone.CodeRules.Services;

namespace MyCodingZone.CodeRules
{
    /// <summary>
    /// A rule pack decides which of its rules a consumer gets. Moved into its own library that references only
    /// Siemens.AspNet.CodeRules.Contracts, the same pack runs unchanged in every repository (MSTest, xUnit, NUnit, CLI).
    /// </summary>
    public sealed class MyCodingZoneRulePack : ICodeRulePack
    {
        public void Register(ICodeRuleRegistry rules)
        {
            rules.Add<ProductionProjectsMustNotReferenceTestProjects>()
                 .Add<RecordPropertiesMustBeImmutable>()
                 .Add<RecordsMustNotImplementMethods>()
                 .Add<ServiceNeedsRegistrationInSameFile>()
                 .Add<ServiceRegistrationMustBeCalled>()
                 .Add<InjectMultipleImplementationsAsIEnumerable>()
                 .Add<ServiceRegistrationRegistersDependencies>()
                 .Add<ServiceRegistrationsReturnVoid>()
                 .Add<ServicesInjectAtMostFiveDependencies>()
                 .Add<ServiceRegistrationRegistersTheServiceItself>();
        }
    }
}
