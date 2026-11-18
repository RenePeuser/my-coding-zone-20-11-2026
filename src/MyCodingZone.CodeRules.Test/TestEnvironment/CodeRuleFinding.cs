using System.Collections.Immutable;

namespace MyCodingZone.CodeRules.Test
{
    /// <summary>
    /// One violation of a CodeRule: what is wrong and where. Same shape as the <c>Finding</c> of the Siemens Code Rule Engine (stage 5).
    /// Why and the default fix live on the rule, see <see cref="CodeRuleViolation"/>.
    /// </summary>
    public sealed record CodeRuleFinding
    {
        /// <summary>The affected element: the offending type, member, reference or package.</summary>
        public required string What { get; init; }

        /// <summary>Where it is. Accepts a <see cref="Solution.Parser.CSharp.CodeLocation"/> or a <see cref="FileInfo"/>.</summary>
        public FindingLocation? Where { get; init; }

        /// <summary>The offending code as it is now.</summary>
        public string? Current { get; init; }

        /// <summary>What the code should look like instead.</summary>
        public string? ShouldBe { get; init; }

        /// <summary>Overrides the HowToFix of the rule when the fix depends on this finding.</summary>
        public string? HowToFix { get; init; }

        /// <summary>Extra "Key : value" lines, shown in the order given, e.g. <c>Details = [("Service", service.Name)]</c>.</summary>
        public ImmutableList<(string Key, string Value)>? Details { get; init; }
    }

    /// <summary>
    /// Where a finding sits, with the path relative to the repository root so the output is the same on every machine.
    /// </summary>
    public sealed record FindingLocation(string File,
                                         int? Line = null)
    {
        public static implicit operator FindingLocation(Solution.Parser.CSharp.CodeLocation location)
        {
            var findingLocation = new FindingLocation(RepositoryRoot.RelativePath(location.FilePath), location.StartLine);

            return findingLocation;
        }

        public static implicit operator FindingLocation(FileInfo file)
        {
            var findingLocation = new FindingLocation(RepositoryRoot.RelativePath(file.FullName));

            return findingLocation;
        }

        public string FileName => Path.GetFileName(File);

        public string ShortForm => Line is null ? FileName : $"{FileName}:{Line}";

        public string ClickableLink
        {
            get
            {
                // Uri escapes '#' (e.g. "03_C#_Files"), which would otherwise start the fragment and cut the link
                var fileUri = new Uri(Path.GetFullPath(Path.Combine(RepositoryRoot.Directory.FullName, File))).AbsoluteUri;

                return Line is null ? fileUri : $"{fileUri}:{Line}";
            }
        }
    }
}
