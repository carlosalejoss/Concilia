using System.IO.Compression;
using System.Text;
using Gherkin;

namespace Concilia.Core.Deliverables.Export;

// The backlog as Gherkin (step 97): one .feature file per epic, all of them in
// a .zip. Decisions of the author (22-10-2026): the keywords in English, the
// language Gherkin assumes when a file does not say another, with the text as
// it is; and the ids as tags, so a test tool can filter by story or by
// requirement.
//
//   # Backlog de ReservaSalas · validado el 20-10-2026 · exportado desde Concilia…
//   @EP-01
//   Feature: Reserva de salas
//     Reservar salas y recibir la confirmación…
//
//     @HU-001 @RF-001 @RF-002
//     Rule: Reservar una sala
//       Como empleado, quiero reservar una sala…, para…
//       Tarea T-001: Pantalla de nueva reserva
//
//       @CA-001
//       Scenario: Reserva de una sala libre
//         Given una sala libre en la franja elegida
//         When el empleado confirma la reserva…
//         Then la reserva queda registrada…
//
// Epic → Feature, story → Rule (the Gherkin keyword for a business rule with
// its examples, since Gherkin 6) and acceptance criterion → Scenario. Every
// file is read back with the official parser (cucumber/gherkin) before it is
// handed out: a file it rejects is a bug of this class, not something to
// download.
public static class BacklogGherkin
{
    private static readonly GherkinDialect English = new GherkinDialectProvider("en").DefaultDialect;

    public static byte[] Write(BacklogExport backlog)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            foreach (var (name, content) in Features(backlog))
            {
                var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                writer.Write(content);
            }
        }
        return stream.ToArray();
    }

    // One file per epic: "EP-01-reserva-de-salas.feature" and its content,
    // already checked with the parser.
    public static IReadOnlyList<(string Name, string Content)> Features(BacklogExport backlog) =>
        backlog.Epics.Select(e => ($"{e.Id}-{ExportFiles.Slug(e.Title, "epica")}.feature", Check(Feature(backlog, e)))).ToList();

    public static string Feature(BacklogExport backlog, ExportEpic epic)
    {
        var text = new StringBuilder();
        text.Append($"# Backlog de {Line(backlog.Project)} · {backlog.Status} · {backlog.Generated}\n");
        text.Append($"@{epic.Id}\n");
        text.Append($"Feature: {Name(epic.Title, epic.Id)}\n");
        Description(text, "  ", [epic.Description]);
        foreach (var story in epic.Stories)
        {
            text.Append('\n');
            var tags = new[] { story.Id }.Concat(story.Requirements).Select(Tag);
            if (story.PendingDiscrepancy)
                tags = tags.Append("@discrepancia-sin-revisar");
            text.Append($"  {string.Join(' ', tags)}\n");
            text.Append($"  Rule: {Name(story.Title, story.Id)}\n");
            Description(text, "    ", [story.Narrative, .. story.Tasks.Select(t => $"Tarea {t.Id}: {t.Title}")]);
            foreach (var c in story.Criteria)
            {
                text.Append('\n');
                text.Append($"    {Tag(c.Id)}\n");
                text.Append($"    Scenario: {Name(c.Scenario, c.Id)}\n");
                Steps(text, "Given", c.Given);
                Steps(text, "When", c.When);
                Steps(text, "Then", c.Then);
            }
        }
        return text.ToString();
    }

    // Reads the file with the official parser: an exception if Gherkin does
    // not accept it.
    public static string Check(string feature)
    {
        try
        {
            new Parser().Parse(new StringReader(feature)); // Parse(string) would take it as a file path
            return feature;
        }
        catch (ParserException e)
        {
            throw new InvalidOperationException($"El .feature generado no es Gherkin válido: {e.Message}", e);
        }
    }

    // A step per line of the text: the first with its keyword, the rest with
    // And. A text with nothing to say still gives a step, so the scenario
    // keeps its three parts.
    private static void Steps(StringBuilder text, string keyword, string value)
    {
        var lines = Lines(value);
        if (lines.Count == 0)
            lines = ["(sin indicar)"];
        for (var i = 0; i < lines.Count; i++)
            text.Append($"      {(i == 0 ? keyword : "And")} {lines[i]}\n");
    }

    // Free text under a Feature or a Rule. The parser never rejects a line
    // there, but some change the meaning without warning (checked with
    // cucumber/gherkin 42): "Scenario: …", "Rule: …" or "Background: …" start
    // a new element, "@…" becomes a tag of the next one and "# …" is a comment
    // that disappears. Those lines get "- " in front, so they stay text.
    // Steps, tables and the rest are kept as text by the parser.
    private static void Description(StringBuilder text, string indent, IEnumerable<string> paragraphs)
    {
        foreach (var line in paragraphs.SelectMany(Lines))
            text.Append($"{indent}{(ChangesTheMeaning(line) ? "- " : "")}{line}\n");
    }

    private static readonly string[] BlockKeywords =
        [.. English.RuleKeywords, .. English.BackgroundKeywords, .. English.ScenarioKeywords, .. English.ScenarioOutlineKeywords];

    private static bool ChangesTheMeaning(string line) =>
        line[0] is '#' or '@' || BlockKeywords.Any(k => line.StartsWith(k + ":", StringComparison.Ordinal));

    // The non-empty lines of a text, trimmed and without control characters.
    private static List<string> Lines(string value) =>
        value.ReplaceLineEndings("\n").Split('\n')
            .Select(l => new string(l.Where(ch => !char.IsControl(ch) || ch == '\t').ToArray()).Trim())
            .Where(l => l.Length > 0)
            .ToList();

    // A name in one line (the title, or the id if it has none).
    private static string Name(string title, string id) => Line(title) is { Length: > 0 } name ? name : id;

    private static string Line(string value) => string.Join(' ', Lines(value));

    // A tag cannot have spaces: the ids never do, but whatever the user typed
    // is made safe.
    private static string Tag(string id) => "@" + string.Concat(id.Where(ch => !char.IsWhiteSpace(ch) && ch != '@' && ch != '#'));
}
