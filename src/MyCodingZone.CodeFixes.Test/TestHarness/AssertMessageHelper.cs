using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MyCodingZone.CodeFixes.Test.TestHarness
{
    internal static class AssertMessageHelper
    {
        public static string FormatDiagnostics(ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.IsDefault || diagnostics.IsEmpty)
            {
                return "<none>";
            }

            var sb = new StringBuilder();

            for (var i = 0; i < diagnostics.Length; i++)
            {
                var d = diagnostics[i];

                if (i > 0)
                {
                    sb.AppendLine();
                }

                sb.Append('[').Append(i).Append("] ")
                  .Append(d.Id)
                  .Append(" (Severity: ")
                  .Append(d.Severity)
                  .Append(") at ")
                  .Append(d.Location.GetLineSpan().StartLinePosition);

                var message = d.GetMessage(CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    sb.AppendLine();
                    sb.Append("    ").Append(message);
                }
            }

            return sb.ToString();
        }

        public static string FormatDiagnosticIds(ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.IsDefault || diagnostics.IsEmpty)
            {
                return "<none>";
            }

            return string.Join(", ", diagnostics.Select(d => d.Id));
        }
    }
}