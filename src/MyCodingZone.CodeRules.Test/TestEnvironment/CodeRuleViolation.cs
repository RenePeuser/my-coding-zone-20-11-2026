using System.Collections.Immutable;

namespace MyCodingZone.CodeRules.Test
{
    /// <summary>
    /// The complete result of a failing CodeRule: the rule and every finding. Human and Ai output are both rendered from it.
    /// The rule part uses the names of <c>IRule</c> of the Siemens Code Rule Engine (stage 5).
    /// </summary>
    public sealed record CodeRuleViolation(string Id,
                                           string Title,
                                           string Group,
                                           string Why,
                                           string HowToFix,
                                           string DocumentationUrl,
                                           string? ReadMoreUrl,
                                           FindingLocation RuleSource,
                                           ImmutableList<CodeRuleFinding> Findings);

    /// <summary>
    /// Thrown when a CodeRule has findings. Carries the <see cref="CodeRuleViolation"/>, so fixtures can check
    /// the findings instead of the rendered text.
    /// </summary>
    public sealed class CodeRuleViolationException(CodeRuleViolation violation,
                                                   string message) : AssertFailedException(message)
    {
        public CodeRuleViolation Violation { get; } = violation;
    }
}
