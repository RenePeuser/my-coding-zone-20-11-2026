using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyCodingZone.CodeRules.Test
{
    /// <summary>
    /// Renders a violation as JSON for AI agents: every finding, repository relative paths, no decoration.
    /// Field names follow the AiResultFormatter of the Siemens Code Rule Engine (stage 5), so an agent learns one schema.
    /// </summary>
    public static class AiCodeRuleOutput
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            // Keep <, > and ' readable - findings quote csproj XML and C# code
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static string Render(CodeRuleViolation violation)
        {
            var output = new
            {
                ErrorCode = "CODE_RULE_VIOLATED",
                RuleId = violation.Id,
                violation.Title,
                violation.Group,
                violation.Why,
                violation.HowToFix,
                Documentation = violation.DocumentationUrl,
                violation.ReadMoreUrl,
                Source = new { violation.RuleSource.File, violation.RuleSource.Line },
                FindingCount = violation.Findings.Count,
                Findings = violation.Findings.Select(finding => new
                {
                    finding.What,
                    finding.Where?.File,
                    finding.Where?.Line,
                    finding.Current,
                    finding.ShouldBe,
                    HowToFix = finding.HowToFix ?? violation.HowToFix,
                    Details = finding.Details?.ToDictionary(detail => detail.Key, detail => detail.Value)
                }),
                Rerun = $"dotnet test src/MyCodingZone.CodeRules.Test --filter \"FullyQualifiedName~{violation.Id}\"",
                Instructions = "Fix the code, not the rule. Do not weaken the rule, add exceptions or [Ignore] without human approval. " +
                               "Use 'rerun' to check your fix."
            };

            return Environment.NewLine + JsonSerializer.Serialize(output, Options);
        }
    }
}
