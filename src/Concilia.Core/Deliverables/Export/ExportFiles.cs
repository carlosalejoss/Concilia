using System.Globalization;
using System.Text;

namespace Concilia.Core.Deliverables.Export;

public static class ExportFiles
{
    // "Clínicas Veterinarias" → "clinicas-veterinarias", for the names of the
    // files: ASCII letters and digits, separated by single hyphens.
    public static string Slug(string text, string fallback = "proyecto")
    {
        var plain = new StringBuilder();
        foreach (var ch in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            plain.Append(char.IsAsciiLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-');
        }
        var slug = string.Join('-', plain.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        return slug.Length > 0 ? slug : fallback;
    }
}
