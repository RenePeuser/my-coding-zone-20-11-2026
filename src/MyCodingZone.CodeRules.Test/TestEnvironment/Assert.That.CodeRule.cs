using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace MyCodingZone.CodeRules.Test
{
    public static class AssertThatCodeRuleExtensions
    {
        /// <summary>
        /// Fails with a <see cref="CodeRuleViolationException"/> when <paramref name="findings"/> is not empty.
        /// The output is Human or Ai, see <see cref="CodeRuleSettings.OutputMode"/>. The group is the folder of the rule file.
        /// The parameters are named like the members of <c>IRule</c> of the Siemens Code Rule Engine (stage 5).
        /// </summary>
        /// <param name="_">Hook for <c>Assert.That</c>.</param>
        /// <param name="findings">The findings of the rule.</param>
        /// <param name="id">The rule id, e.g. <c>RECORDS_RULE_001</c>. The same id as the stage 5 rule.</param>
        /// <param name="title">Short statement of the convention.</param>
        /// <param name="why">The decision behind the convention: why we do it this way and what goes wrong otherwise.</param>
        /// <param name="howToFix">How to fix a violation. A finding can override it.</param>
        /// <param name="readMoreUrl">Background reading about the problem, e.g. the docs of the API or pattern the rule is about.</param>
        /// <param name="callerFilePath">Set by the compiler: the rule file, shown as link to the rule. Its folder is the group.</param>
        /// <param name="callerLineNumber">Set by the compiler: the line of the assert in the rule file.</param>
        public static void CodeRuleHasNoFindings(this Assert _,
                                                 IEnumerable<CodeRuleFinding> findings,
                                                 string id,
                                                 string title,
                                                 string why,
                                                 string howToFix,
                                                 string? readMoreUrl = null,
                                                 [CallerFilePath] string callerFilePath = "",
                                                 [CallerLineNumber] int callerLineNumber = 0)
        {
            // Sorted, so the output does not depend on parse order
            var sortedFindings = findings.OrderBy(finding => finding.Where?.File, StringComparer.Ordinal)
                                         .ThenBy(finding => finding.Where?.Line)
                                         .ThenBy(finding => finding.What, StringComparer.Ordinal)
                                         .ToImmutableList();

            if (sortedFindings.IsEmpty)
            {
                return;
            }

            var violation = new CodeRuleViolation(Id: id,
                                                  Title: title,
                                                  Group: Path.GetFileName(Path.GetDirectoryName(callerFilePath)) ?? "-",
                                                  Why: why,
                                                  HowToFix: howToFix,
                                                  DocumentationUrl: CodeRuleSettings.DocumentationUrlTemplate.Replace("{Id}", id, StringComparison.Ordinal),
                                                  ReadMoreUrl: readMoreUrl,
                                                  RuleSource: new FindingLocation(RepositoryRoot.RelativePath(callerFilePath), callerLineNumber),
                                                  Findings: sortedFindings);

            var message = CodeRuleSettings.OutputMode == CodeRuleOutputMode.Ai ? AiCodeRuleOutput.Render(violation) : HumanCodeRuleOutput.Render(violation);

            throw new CodeRuleViolationException(violation, message);
        }
    }
}
