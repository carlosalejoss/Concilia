using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Concilia.Core.Catalog;

namespace Concilia.Core.Phases;

// The documentation as the model reads it by parts (input/documentacion.txt,
// decision of 09-10-2026): one fragment per line, so Read can go through it
// by line ranges and every line keeps the id to cite.
//
//   === DOC-01 · DOC-01.txt ===
//   [DOC-01#F007 §2.5] El empleado puede reservar una sala…
//   [DOC-01#F008 sigue] …the rest of a fragment longer than MaxLineChars
public static class DocumentationFile
{
    public const string Path = "input/documentacion.txt";

    // Read cuts lines longer than 2,000 characters: a longer fragment goes on
    // in "sigue" lines, so nothing is lost and every piece keeps its id.
    public const int MaxLineChars = 1_500;

    public static string Build(IEnumerable<Fragment> fragments, IReadOnlyDictionary<string, string> names)
    {
        var text = new StringBuilder();
        string? current = null;
        foreach (var f in fragments.Where(f => f.Kind == Fragmenter.DocumentKind))
        {
            if (f.Source != current)
            {
                current = f.Source;
                text.Append($"=== {current} · {(names.TryGetValue(current, out var name) ? name : current)} ===\n");
            }
            var section = f.Section.Length > 0 ? $" {f.Section}" : "";
            var pieces = Pieces(f.Text.ReplaceLineEndings(" "), MaxLineChars);
            for (var i = 0; i < pieces.Count; i++)
                text.Append(i == 0 ? $"[{f.Id}{section}] {pieces[i]}\n" : $"[{f.Id} sigue] {pieces[i]}\n");
        }
        return text.ToString();
    }

    // The fragment of each line of the file (1-based), from the "[DOC-01#F007 …]"
    // at its start; header lines are left out.
    public static IReadOnlyDictionary<int, string> FragmentAtLine(string documentation)
    {
        var lines = new Dictionary<int, string>();
        var number = 0;
        foreach (var line in documentation.Split('\n'))
        {
            number++;
            var id = LineId.Match(line);
            if (id.Success)
                lines[number] = id.Groups[1].Value;
        }
        return lines;
    }

    private static readonly System.Text.RegularExpressions.Regex LineId = new(@"^\[([A-Z]+-\d+#F\d+)[ \]]");

    // Splits a text in pieces of at most max characters, at a space when there is one.
    private static List<string> Pieces(string text, int max)
    {
        var pieces = new List<string>();
        while (text.Length > max)
        {
            var cut = text.LastIndexOf(' ', max);
            if (cut <= 0)
                cut = max;
            pieces.Add(text[..cut].TrimEnd());
            text = text[cut..].TrimStart();
        }
        pieces.Add(text);
        return pieces;
    }
}

// A file of input/ that the model reads: its content, and whether it must read
// it entirely (the platform checks it) or only consult what it needs.
public record InputFile(string Path, string Content, bool Required);

// The reading plan the prompt carries ({{LECTURA}}): for each file, its size
// and the Read calls (offset and limit) that read it, each small enough for
// one call (Read refuses a call of more than about 25,000 tokens).
public static class ReadingPlan
{
    public const int MaxCharsPerRead = 40_000; // about 11,000 tokens
    public const int MaxLinesPerRead = 1_000;

    // [From, To] line ranges covering the whole text, in order.
    public static IReadOnlyList<(int From, int To)> Ranges(string content)
    {
        var lines = content.Split('\n');
        var count = Runner.ReadCoverage.LineCount(content);
        var ranges = new List<(int From, int To)>();
        var from = 1;
        var chars = 0;
        for (var line = 1; line <= count; line++)
        {
            var length = lines[line - 1].Length + 1;
            if (line > from && (chars + length > MaxCharsPerRead || line - from >= MaxLinesPerRead))
            {
                ranges.Add((from, line - 1));
                from = line;
                chars = 0;
            }
            chars += length;
        }
        if (count >= from)
            ranges.Add((from, count));
        return ranges;
    }

    public static string Block(IEnumerable<InputFile> files) => string.Join("\n", files.Select(f =>
    {
        var lines = Runner.ReadCoverage.LineCount(f.Content);
        if (f.Required)
            return $"- `{f.Path}` ({lines} líneas), entero, con estas llamadas a Read: {Calls(f.Content)}.";
        var consult = $"- `{f.Path}` ({lines} líneas), solo para consultar lo que necesites: no lo leas entero.";
        var starts = DocumentStarts(f.Content);
        return starts.Count == 0 ? consult : $"{consult} Dónde empieza cada documento: {string.Join("; ", starts)}.";
    }));

    // "DOC-01 en la línea 1", from the "=== DOC-01 · name ===" lines of the
    // documentation, so a model that only consults it can go to a document
    // (the arbiter read it all and did not fit; 14-10-2026).
    private static List<string> DocumentStarts(string content) =>
        content.Split('\n')
            .Select((line, i) => (line, number: i + 1))
            .Where(l => l.line.StartsWith("=== ", StringComparison.Ordinal))
            .Select(l => $"{l.line.Split(' ', 3)[1]} en la línea {l.number}")
            .ToList();

    // The exact arguments of each Read call: "`offset` 1, `limit` 242; `offset` 243, `limit` 203".
    // v0.2: with ranges of lines ("1–242") LUNA counted from 0 and never read
    // the last line (first comparison, 09-10-2026).
    public static string Calls(string content) =>
        string.Join("; ", Ranges(content).Select(r => $"`offset` {r.From}, `limit` {r.To - r.From + 1}"));

    // A JSON input (a proposal, the catalog) with one element per line: each
    // requirement or question on its own line, compact. A real arbiter of the
    // catalog (14-10-2026, two proposals of about 230 requirements) did not
    // fit in 128k with them indented, one value per line: it compacted the
    // context and read them again until it ran out of turns. Without the
    // indentation they take about a fifth less. Whatever does not fit in a
    // line of MaxLineChars (Read cuts lines at 2,000 characters) opens one
    // level, and so on. Text that is not JSON is kept as it is.
    public const int MaxLineChars = 1_800;

    private static readonly JsonSerializerOptions CompactJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string OnePerLine(string json)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return json;
        }
        var text = new StringBuilder();
        Write(root, text);
        return text.Append('\n').ToString();
    }

    private static void Write(JsonNode? node, StringBuilder text)
    {
        var compact = node?.ToJsonString(CompactJson) ?? "null";
        if (compact.Length <= MaxLineChars || node is not (JsonObject or JsonArray))
        {
            text.Append(compact);
            return;
        }
        if (node is JsonObject obj)
        {
            text.Append("{\n");
            var first = true;
            foreach (var (key, value) in obj)
            {
                if (!first)
                    text.Append(",\n");
                first = false;
                text.Append(JsonSerializer.Serialize(key, CompactJson)).Append(": ");
                Write(value, text);
            }
            text.Append("\n}");
        }
        else
        {
            var array = (JsonArray)node;
            text.Append("[\n");
            for (var i = 0; i < array.Count; i++)
            {
                if (i > 0)
                    text.Append(",\n");
                Write(array[i], text);
            }
            text.Append("\n]");
        }
    }

    // A JSON input (a proposal, the catalog, the backlog) indented, one value
    // per line: the model's output may come in a single line, which Read would
    // cut at 2,000 characters. Text that is not JSON is kept as it is.
    public static string Indented(string json)
    {
        try
        {
            return JsonNode.Parse(json)!.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }) + "\n";
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
