using AspNetCore.Simple.MsTest.Sdk;
using System.Collections.Immutable;
using System.Text.Json;

namespace MyCodingZone.CodeRules.Test
{
    /// <summary>
    /// Fixtures for the finding output itself. Not tagged "CodeRules" - these do not check the repository.
    /// </summary>
    [TestCategory("CodeRules Infrastructure")]
    [TestClass]
    public class CodeRuleOutput_Test
    {
        private const string ReadMoreUrl = "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/init";

        private static readonly ImmutableList<CodeRuleFinding> Findings =
            [
                // Sorted by file: src/MyCodingZone.HowTo/... comes before src/MyCodingZone.WebApi/...
                new()
                {
                    What = "MyCodingZone.HowTo.Person.City",
                    Where = new FindingLocation("src/MyCodingZone.HowTo/Person.cs", 9),
                    Current = "public required string City { get; set; }",
                    ShouldBe = "public required string City { get; init; }",
                    Details = [("Property", "string City")]
                },
                new()
                {
                    What = "MyCodingZone.WebApi.UserDto.Email",
                    Where = new FindingLocation("src/MyCodingZone.WebApi/UserDto.cs", 12),
                    Current = "public required string Email { get; set; }",
                    ShouldBe = "public required string Email { get; init; }",
                    Details = [("Property", "string Email")]
                }
            ];

        [TestMethod]
        public void No_Findings_Passes()
        {
            Assert.That.DoesNotThrow(() => Assert.That.CodeRuleHasNoFindings([], id: "TEST_RULE_001", title: "Title", why: "Why", howToFix: "HowToFix"),
                                     because: "A rule without findings is green",
                                     fix: "CodeRuleHasNoFindings must return when the findings are empty");
        }

        [TestMethod]
        public void Findings_Throw_A_Violation_With_Sorted_Findings()
        {
            var exception = Assert.That.ThrowsExactly<CodeRuleViolationException>(() => Assert.That.CodeRuleHasNoFindings(Findings.Reverse(), id: "TEST_RULE_001", title: "Title", why: "Why", howToFix: "HowToFix"),
                                                                                  because: "A rule with findings has to fail with the structured violation",
                                                                                  fix: "CodeRuleHasNoFindings must throw CodeRuleViolationException");

            Assert.That.AreEqual(Findings.Select(finding => finding.What),
                                 exception.Violation.Findings.Select(finding => finding.What),
                                 because: "Findings are sorted by file and line, so the output does not depend on parse order",
                                 fix: "Sort the findings in CodeRuleHasNoFindings by Where, then What");

            Assert.That.AreEqual("TestEnvironment",
                                 exception.Violation.Group,
                                 because: "The group is the folder of the rule file, like the namespace segment of a stage 5 rule",
                                 fix: "Derive the group from the caller file path");
        }

        [TestMethod]
        public void Human_Output_Has_An_Aligned_Table()
        {
            var output = HumanCodeRuleOutput.Render(Violation(Findings));

            var tableLineLengths = output.ReplaceLineEndings("\n")
                                         .Split('\n')
                                         .Where(line => line.StartsWith('┌') || line.StartsWith('│') || line.StartsWith('├') || line.StartsWith('└'))
                                         .Select(line => line.Length)
                                         .Distinct()
                                         .ToImmutableList();

            Assert.That.HasCount(1,
                                 tableLineLengths,
                                 because: "Every line of the findings table has the same width",
                                 fix: "Compute the column widths from the longest cell in HumanCodeRuleOutput.FindingsTable");
        }

        [TestMethod]
        public void Human_Output_Details_Only_The_First_Findings()
        {
            var manyFindings = Enumerable.Range(1, HumanCodeRuleOutput.MaxDetailedFindings + 3)
                                         .Select(number => new CodeRuleFinding { What = $"What {number:00}" })
                                         .ToImmutableList();

            var output = HumanCodeRuleOutput.Render(Violation(manyFindings));

            Assert.That.Contains(output,
                                 "… and 3 more (see table above)",
                                 because: "Long outputs keep the table complete but cut the detailed sections",
                                 fix: "Render at most MaxDetailedFindings finding sections");
        }

        [TestMethod]
        public void Human_Output_Uses_The_Labels_Of_The_Code_Rule_Engine()
        {
            var output = HumanCodeRuleOutput.Render(Violation(Findings));

            foreach (var label in new[] { "Group      : ", "Read more  : ", "Why        : ", "What       : ", "Where      : ", "Current    : ", "Should be  : ", "How to fix : " })
            {
                Assert.That.Contains(output,
                                     label,
                                     because: "Stage 2 and stage 5 report a finding with the same labels, so the same rule reads the same in both stages",
                                     fix: "Use the labels of HumanRuleOutput of the Siemens Code Rule Engine in HumanCodeRuleOutput");
            }
        }

        [TestMethod]
        public void Ai_Output_Is_Json_With_Every_Finding()
        {
            var output = AiCodeRuleOutput.Render(Violation(Findings));

            using var json = JsonDocument.Parse(output);

            Assert.That.AreEqual(2,
                                 json.RootElement.GetProperty("findings").GetArrayLength(),
                                 because: "Agents need every finding, not a shortened list",
                                 fix: "Serialize all findings in AiCodeRuleOutput");

            Assert.That.AreEqual("src/MyCodingZone.WebApi/UserDto.cs",
                                 json.RootElement.GetProperty("findings")[1].GetProperty("file").GetString(),
                                 because: "Paths are relative to the repository root so they work on every machine",
                                 fix: "Write FindingLocation.File, not the clickable absolute link");
        }

        [TestMethod]
        public void Ai_Output_Uses_The_Field_Names_Of_The_Code_Rule_Engine()
        {
            using var json = JsonDocument.Parse(AiCodeRuleOutput.Render(Violation(Findings)));

            var rule = json.RootElement;
            var finding = rule.GetProperty("findings")[0];

            var fields = new (JsonElement Element, string Name)[]
            {
                (rule, "ruleId"), (rule, "title"), (rule, "group"), (rule, "why"), (rule, "howToFix"), (rule, "documentation"), (rule, "readMoreUrl"),
                (finding, "what"), (finding, "file"), (finding, "line"), (finding, "current"), (finding, "shouldBe"), (finding, "howToFix"), (finding, "details")
            };

            foreach (var (element, name) in fields)
            {
                Assert.That.IsTrue(element.TryGetProperty(name, out _),
                                   because: $"'{name}' is the field name of the AiResultFormatter of the Siemens Code Rule Engine, so an agent learns one schema",
                                   fix: $"Serialize '{name}' in AiCodeRuleOutput");
            }
        }

        [TestMethod]
        public void Read_More_Url_Is_Rendered()
        {
            var violation = Violation(Findings);

            Assert.That.Contains(HumanCodeRuleOutput.Render(violation),
                                 $"Read more  : {ReadMoreUrl}",
                                 because: "A rule can point to background reading about the problem, like IRule.ReadMoreUrl in stage 5",
                                 fix: "Render CodeRuleViolation.ReadMoreUrl as 'Read more' field in HumanCodeRuleOutput");

            using var json = JsonDocument.Parse(AiCodeRuleOutput.Render(violation));

            Assert.That.AreEqual(ReadMoreUrl,
                                 json.RootElement.GetProperty("readMoreUrl").GetString(),
                                 because: "Agents need the background reading as well",
                                 fix: "Serialize CodeRuleViolation.ReadMoreUrl as 'readMoreUrl' in AiCodeRuleOutput");
        }

        [TestMethod]
        public void Clickable_Link_Escapes_Special_Characters_In_The_Path()
        {
            var link = new FindingLocation("src/MyCodingZone.HowTo/03_C#_Files/Reflection_PropertyInfo_Test.cs", 9).ClickableLink;

            Assert.That.EndsWith(link,
                                 "/src/MyCodingZone.HowTo/03_C%23_Files/Reflection_PropertyInfo_Test.cs:9",
                                 because: "An unescaped '#' starts the URI fragment, so the link would end at '03_C' and open nothing",
                                 fix: "Build FindingLocation.ClickableLink with System.Uri instead of concatenating the raw path");
        }

        private static CodeRuleViolation Violation(ImmutableList<CodeRuleFinding> findings)
        {
            var violation = new CodeRuleViolation(Id: "TEST_RULE_001",
                                                  Title: "Record properties must be immutable",
                                                  Group: "Records",
                                                  Why: "Records are value objects.",
                                                  HowToFix: "Replace 'set;' with 'init;'.",
                                                  DocumentationUrl: CodeRuleSettings.DocumentationUrlTemplate.Replace("{Id}", "TEST_RULE_001", StringComparison.Ordinal),
                                                  ReadMoreUrl: ReadMoreUrl,
                                                  RuleSource: new FindingLocation("src/MyCodingZone.CodeRules.Test/Records/TEST_RULE_001.cs", 1),
                                                  Findings: findings);

            return violation;
        }
    }
}
