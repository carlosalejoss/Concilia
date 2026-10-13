using System.Text;
using System.Text.RegularExpressions;

namespace Concilia.Core.Catalog;

// One citable piece of the input: a sentence of a document ("DOC-01#F007") or
// a resolved doubt ("DUDAS#D-001"). The model cites fragments by id and the
// validator checks that each quote is literal inside its fragment.
// SourceRef is the source's own requirement id ("FR-12"), if the sentence starts with one.
public record Fragment(string Id, string Source, string Kind, string Section, string Text, string? SourceRef);

// Port of prompts/generacion_requisitos/fragmentar.py, the reference
// implementation: for the same text it must give exactly the same fragments
// (the tests compare with the examples' fragmentos.json). Keep both in sync.
public static class Fragmenter
{
    public const string DocumentKind = "document";
    public const string ClarificationKind = "clarification";

    // Section label at the start of a line: "§2.1", "2.1", "2.1.", "3." (followed by a space).
    private static readonly Regex SectionRe = new(@"^(§\s?\d+(?:\.\d+)*\.?|\d+(?:\.\d+)+\.?|\d+\.)\s+");
    private static readonly Regex MdHeadingRe = new(@"^#{1,6}\s+(.*)$");
    private static readonly Regex BulletRe = new(@"^([-*•·]|[a-z]\)|\(?[ivx]+\))\s+", RegexOptions.IgnoreCase);

    // A numbered heading ("2. Requisitos funcionales") is short and has no final punctuation.
    private const int HeadingMaxWords = 10;
    private static readonly Regex HeadingEndRe = new(@"[.,;:!?…]$");

    // Source requirement id at the start of a paragraph ("FR-12", "NFR_3", "REQ-001", "R12").
    private static readonly Regex SourceRefRe = new(@"^((?:[A-Z]{1,6}[-_]\d+(?:[.-]\d+)*)|(?:[A-Z]{1,4}\d+(?:\.\d+)*))\b[:.)]?\s");

    // Sentence break: closing mark + spaces + a sentence start (capital, digit or opening mark).
    private static readonly Regex SentenceSplitRe = new(@"(?<=[.!?…])\s+(?=[«""“(¿¡]?[A-ZÁÉÍÓÚÑÜ0-9])");

    private static readonly Regex NewBlockStartRe = new(@"^[«""“(¿¡]?[A-ZÁÉÍÓÚÑÜ]");
    private static readonly Regex InitialRe = new(@"^[A-ZÁÉÍÓÚÑ]\.$");
    private static readonly Regex WhitespaceRe = new(@"\s+");

    // No sentence break after these (compared with the last word before the break).
    // "etc." is not here: followed by a capital it usually ends the sentence.
    private static readonly HashSet<string> Abbreviations =
    [
        "p.", "ej.", "sr.", "sra.", "srta.", "dr.", "dra.", "núm.", "nº.", "art.", "arts.", "pág.", "págs.",
        "cap.", "aprox.", "vol.", "fig.", "tel.", "ee.", "ud.", "uds.", "máx.", "mín.", "e.g.", "i.e.", "vs.",
        "approx.", "max.", "min.",
    ];

    // NFC and collapsed spaces. Fragment texts are stored already normalized.
    public static string Norm(string s) =>
        WhitespaceRe.Replace(s.Normalize(NormalizationForm.FormC), " ").Trim();

    // Keeps a text from closing the prompt's tags. Applied equally to the JSON
    // and to the prompt, so quotes stay literal with respect to what the model sees.
    public static string SafeText(string s) => s.Replace("<", "＜").Replace(">", "＞");

    public static string SafeAttr(string s) => SafeText(s).Replace('"', '\'');

    public static List<Fragment> FragmentDocument(string docId, string text)
    {
        var fragments = new List<Fragment>();
        var n = 0;
        foreach (var (section, paragraph, isHeading) in Paragraphs(text))
        {
            var sentences = isHeading ? [Norm(paragraph)] : SplitSentences(paragraph);
            foreach (var sentence in sentences)
            {
                n++;
                var sourceRef = isHeading ? null : SourceRefRe.Match(sentence) is { Success: true } m ? m.Groups[1].Value : null;
                fragments.Add(new Fragment($"{docId}#F{n:000}", docId, DocumentKind, SafeAttr(section), SafeText(sentence), sourceRef));
            }
        }
        return fragments;
    }

    // One fragment per resolved doubt; the question goes into Section.
    public static List<Fragment> FragmentDoubts(IEnumerable<(string Id, string Question, string Answer)> doubts) =>
        doubts.Select(d => new Fragment($"DUDAS#{d.Id}", "DUDAS", ClarificationKind,
                                        SafeAttr(Norm(d.Question)), SafeText(Norm(d.Answer)), null))
              .ToList();

    // The <documento> blocks that replace {{DOCUMENTACION}} in the prompt
    // (same format as fragmentar.py --bloque-doc). names: document id -> file name.
    public static string DocumentBlock(IEnumerable<Fragment> fragments, IReadOnlyDictionary<string, string> names)
    {
        var lines = new List<string>();
        string? current = null;
        foreach (var f in fragments.Where(f => f.Kind == DocumentKind))
        {
            if (f.Source != current)
            {
                if (current is not null)
                {
                    lines.Add("</documento>");
                }
                current = f.Source;
                var name = names.TryGetValue(current, out var n) ? n : current;
                lines.Add($"<documento id=\"{current}\" nombre=\"{SafeAttr(name)}\">");
            }
            lines.Add($"<f id=\"{f.Id}\" sec=\"{f.Section}\">{f.Text}</f>");
        }
        if (current is not null)
        {
            lines.Add("</documento>");
        }
        return string.Join("\n", lines);
    }

    // The fragments that replace {{DUDAS_RESUELTAS}} (fragmentar.py --bloque-dudas).
    public static string DoubtsBlock(IEnumerable<Fragment> fragments) =>
        string.Join("\n", fragments
            .Where(f => f.Kind == ClarificationKind)
            .Select(f => $"<f id=\"{f.Id}\" pregunta=\"{f.Section}\">{f.Text}</f>"));

    // {{INDICE}} of the prompt v1.3 (document_index in fragmentar.py): one line
    // per section, in order and without repeats, with its label and, when the
    // section starts with a heading, the heading ("2. Functional requirements").
    // A markdown heading is its own label ("3.1 Roles"); a section without a
    // heading keeps only its label ("§2.1"). It reads the text the same way as
    // FragmentDocument, so the labels match the `sec` of the fragments.
    public static List<string> DocumentIndex(string text)
    {
        var lines = new List<string>();
        var seen = new HashSet<string>();
        foreach (var (section, paragraph, isHeading) in Paragraphs(text))
        {
            if (section.Length == 0 || !seen.Add(section))
                continue;
            var label = SafeAttr(section);
            var title = SafeText(Norm(paragraph));
            lines.Add(isHeading && title != label ? $"{label} {title}" : label);
        }
        return lines;
    }

    // The index of every document (id, name, text). With several documents,
    // each index starts with a line with the document's id and name.
    public static string IndexBlock(IReadOnlyList<(string Id, string Name, string Text)> documents)
    {
        var lines = new List<string>();
        foreach (var (id, name, text) in documents)
        {
            if (documents.Count > 1)
                lines.Add($"{id} {SafeText(name)}");
            lines.AddRange(DocumentIndex(text));
        }
        return string.Join("\n", lines);
    }

    private static List<string> SplitSentences(string paragraph)
    {
        var output = new List<string>();
        foreach (var part in SentenceSplitRe.Split(paragraph))
        {
            var last = output.Count > 0 ? LastWord(output[^1]) : "";
            if (output.Count > 0 && (Abbreviations.Contains(last.ToLowerInvariant()) || InitialRe.IsMatch(last)))
            {
                output[^1] = $"{output[^1]} {part}";
            }
            else
            {
                output.Add(part);
            }
        }
        return output.Select(Norm).Where(p => p.Length > 0).ToList();
    }

    private static string LastWord(string s) =>
        s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) is { Length: > 0 } words ? words[^1] : "";

    private static int WordCount(string s) =>
        s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    // The line opens a block of its own: section label, markdown heading, bullet or capital letter.
    private static bool StartsNewBlock(string line) =>
        SectionRe.IsMatch(line) || MdHeadingRe.IsMatch(line) || BulletRe.IsMatch(line) || NewBlockStartRe.IsMatch(line);

    // After a section label, the rest is a heading if it is short, has no final
    // punctuation and what follows is not its continuation (a line broken by a
    // PDF export continues in lower case).
    private static bool IsNumberedHeading(string rest, string? nextLine)
    {
        var words = WordCount(rest);
        return words > 0 && words <= HeadingMaxWords && !HeadingEndRe.IsMatch(rest)
            && (nextLine is null || nextLine.Length == 0 || StartsNewBlock(nextLine));
    }

    // Yields (section, paragraph, isHeading). A line with a section label, a
    // markdown heading or a bullet opens a new paragraph; lines that continue a
    // paragraph (text extracted from a PDF) are joined.
    private static IEnumerable<(string Section, string Paragraph, bool IsHeading)> Paragraphs(string text)
    {
        var section = "";
        var currentSection = "";
        var current = new List<string>();
        var lines = SplitLines(text).Select(l => l.Trim()).ToList();

        IEnumerable<(string, string, bool)> Flush()
        {
            if (current.Count > 0)
            {
                var paragraph = Norm(string.Join(" ", current));
                current = [];
                yield return (currentSection, paragraph, false);
            }
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Length == 0)
            {
                foreach (var p in Flush()) yield return p;
                continue;
            }

            var heading = MdHeadingRe.Match(line);
            if (heading.Success)
            {
                foreach (var p in Flush()) yield return p;
                section = Norm(heading.Groups[1].Value);
                currentSection = section;
                yield return (section, section, true); // a heading is a single fragment
                continue;
            }

            var sectionLabel = SectionRe.Match(line);
            if (sectionLabel.Success)
            {
                foreach (var p in Flush()) yield return p;
                section = sectionLabel.Groups[1].Value.Replace(" ", "");
                currentSection = section;
                var rest = line[sectionLabel.Length..];
                if (IsNumberedHeading(rest, i + 1 < lines.Count ? lines[i + 1] : null))
                {
                    yield return (section, Norm(rest), true); // numbered heading, without its label
                    continue;
                }
                current = rest.Length > 0 ? [rest] : [];
                continue;
            }

            var bullet = BulletRe.Match(line);
            if (bullet.Success)
            {
                foreach (var p in Flush()) yield return p;
                currentSection = section;
                current = [line[bullet.Length..]]; // the bullet marker is not part of the text
                continue;
            }

            if (current.Count == 0)
            {
                currentSection = section;
            }
            current.Add(line);
        }

        foreach (var p in Flush()) yield return p;
    }

    // Same line breaks as Python's str.splitlines(): besides \n, \r\n and \r it
    // also breaks on \v, \f (page break in PDF text), \x1c-\x1e, \x85, \u2028
    // and \u2029. A final line break does not add an empty line.
    private static List<string> SplitLines(string text)
    {
        var lines = new List<string>();
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is '\n' or '\r' or '\v' or '\f' or '\x1c' or '\x1d' or '\x1e' or '\x85' or '\u2028' or '\u2029')
            {
                lines.Add(text[start..i]);
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }
                start = i + 1;
            }
        }
        if (start < text.Length)
        {
            lines.Add(text[start..]);
        }
        return lines;
    }
}
