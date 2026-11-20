using Microsoft.Extensions.DependencyInjection;
using Siemens.AspNet.CodeRules.Engine;

// The engine creates every context once and thread safe, the solution is parsed once per test run.
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace MyCodingZone.CodeRules
{
    /// <summary>
    /// The composition root of this code rule project - the only class it needs.
    /// The source generator of Siemens.AspNet.CodeRules.MSTest reads these registrations at build time and writes
    /// one MSTest test per rule (CodeRules.Generated.*). The 'coderules' CLI runs the very same setup.
    /// </summary>
    public sealed class CodeRuleSetup : ICodeRuleSetup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCodeRuleEngine("code-rules.slnx",
                                       options => options.AddRulePack<MyCodingZoneRulePack>()
                                                         .UseOutputMode(CodeRuleOutputMode.Human)
                                                         .UseDocumentationUrlTemplate("https://github.com/RenePeuser/my-coding-zone-20-11-2026/blob/main/docs/code-rules/{Id}.md"));
        }
    }
}
