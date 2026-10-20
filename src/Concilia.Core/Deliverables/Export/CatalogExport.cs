using System.Text.Json.Nodes;
using Concilia.Core.Catalog;

namespace Concilia.Core.Deliverables.Export;

// A question of the project as the export shows it: its answer and whether
// that answer is a guess of the AI accepted as it was (step 93).
public record ExportQuestion(string Id, string Text, string? Answer, bool AiGuess);

public record ExportRequirement(
    string Id, string Notation, string Obligation, string Text,
    IReadOnlyList<string> Sources, IReadOnlyList<string> Questions,
    // Marks for whoever reads the document: it cites a guess of the AI, or it
    // still has a discrepancy the user has not reviewed (only in a draft).
    bool AiGuess, bool PendingDiscrepancy);

public record ExportSection(string Type, string Title, IReadOnlyList<ExportRequirement> Requirements);

// A cited fragment for the annex of sources, with the name of its document.
// Text is null if the fragment is not in the documentation the catalog was
// generated from (a citation the user typed by hand).
public record ExportSource(string Id, string Document, string Section, string? Text);

// The catalog as the Word and PDF files show it (step 96, decisions of the
// author of 20-10-2026): a section per type, each requirement with its
// notation, obligation, sources and questions by id, and two annexes with the
// text of each cited fragment and each cited question with its answer, once
// each, so the document stays short and traceable. The origin (A, B or both)
// and the arbiter's explanation are internal and are left out.
public record CatalogExport(
    string Project, bool Validated, DateTimeOffset Date, DateTimeOffset? ValidatedAt,
    IReadOnlyList<ExportSection> Sections, IReadOnlyList<ExportSource> Sources, IReadOnlyList<ExportQuestion> Questions)
{
    // The order and names of the web (CatalogEditor.tsx).
    public static readonly (string Type, string Title)[] Types =
    [
        ("RF", "Requisitos funcionales"),
        ("RNF", "Requisitos no funcionales"),
        ("RI", "Requisitos de integración"),
        ("RA", "Requisitos de arquitectura"),
        ("RT", "Requisitos técnicos"),
    ];

    public int Total => Sections.Sum(s => s.Requirements.Count);

    // The texts both files share, so the Word and the PDF say the same.
    public const string Title = "Catálogo de requisitos";

    public string Status => Validated
        ? $"Validado el {ValidatedAt ?? Date:dd-MM-yyyy}"
        : "Borrador: todavía no está validado";

    public string Generated => $"Exportado desde Concilia el {Date:dd-MM-yyyy} a las {Date:HH:mm}.";

    public string Footer => $"{Project} · {Title} · {(Validated ? "validado" : "BORRADOR")}";

    public const string Legend =
        "Cada requisito está escrito en EARS o en MASTeR, con su obligatoriedad: shall (obligatorio), should " +
        "(recomendable) o may (opcional). Las fuentes, como DOC-01#F007, son fragmentos de la documentación del " +
        "proyecto: su texto está en el anexo A. Las preguntas, como P-003, se hicieron al cliente: están en el anexo B " +
        "con su respuesta.";

    public const string AiGuessMark = "Suposición de la IA: cita una pregunta respondida con una suposición de la IA aceptada sin cambios.";
    public const string DiscrepancyMark = "Discrepancia sin revisar: los dos modelos no coincidían y todavía no se ha revisado.";

    public const string SourcesTitle = "Anexo A. Fuentes citadas";
    public const string QuestionsTitle = "Anexo B. Preguntas y respuestas";

    public static string Obligation(string value) => value switch
    {
        "shall" => "shall (obligatorio)",
        "should" => "should (recomendable)",
        "may" => "may (opcional)",
        _ => value,
    };

    public static string Details(ExportRequirement r) => $"{r.Notation} · {Obligation(r.Obligation)}";

    // "Fuentes: DOC-01#F003, DOC-01#F007 · Preguntas: P-002" (what it has).
    public static string References(ExportRequirement r) => string.Join(" · ", new[]
    {
        r.Sources.Count > 0 ? $"Fuentes: {string.Join(", ", r.Sources)}" : null,
        r.Questions.Count > 0 ? $"Preguntas: {string.Join(", ", r.Questions)}" : null,
    }.Where(s => s is not null));

    public static string Answer(ExportQuestion q) =>
        q.Answer is { Length: > 0 } answer
            ? q.AiGuess ? $"Respuesta (suposición de la IA): {answer}" : $"Respuesta: {answer}"
            : "Sin respuesta.";

    // catalogJson: the working copy (CatalogDraft). fragments and
    // documentation: those of the plan of the job that generated it.
    // questions: all those of the project.
    public static CatalogExport Build(
        string project, bool validated, DateTimeOffset date, DateTimeOffset? validatedAt, string catalogJson,
        IReadOnlyList<Fragment> fragments, string documentation, IReadOnlyList<ExportQuestion> questions)
    {
        var guesses = questions.Where(q => q.AiGuess).Select(q => q.Id).ToHashSet();
        var requirements = (JsonNode.Parse(catalogJson)!["requisitos"] as JsonArray ?? []).OfType<JsonObject>().ToList();

        var sections = new List<ExportSection>();
        foreach (var (type, title) in Types)
        {
            var ofType = requirements
                .Where(r => Text(r, "tipo") == type)
                .Select(r => new ExportRequirement(
                    Text(r, "id"), Text(r, "notacion"), Text(r, "obligatoriedad"), Text(r, "texto"),
                    List(r, "fuentes"), List(r, "preguntas"),
                    AiGuess: List(r, "preguntas").Any(guesses.Contains),
                    PendingDiscrepancy: Flag(r, "discrepancia") && !Flag(r, "revisada")))
                .ToList();
            if (ofType.Count > 0)
                sections.Add(new ExportSection(type, title, ofType));
        }

        // Each cited fragment once, in the order of the documentation; those
        // that are not in it, at the end.
        var cited = sections.SelectMany(s => s.Requirements).SelectMany(r => r.Sources).ToHashSet();
        var names = DocumentNames(documentation);
        var known = fragments.Where(f => cited.Contains(f.Id)).ToList();
        var sources = known
            .Select(f => new ExportSource(f.Id, names.GetValueOrDefault(f.Source, f.Source), f.Section, f.Text))
            .Concat(cited.Except(known.Select(f => f.Id)).Order(StringComparer.Ordinal)
                .Select(id => new ExportSource(id, DocumentOf(id, names), "", null)))
            .ToList();

        var citedQuestions = sections.SelectMany(s => s.Requirements).SelectMany(r => r.Questions).ToHashSet();
        var shownQuestions = questions.Where(q => citedQuestions.Contains(q.Id)).OrderBy(q => q.Id, StringComparer.Ordinal).ToList();

        return new CatalogExport(project, validated, date, validatedAt, sections, sources, shownQuestions);
    }

    // "DOC-01" → "DOC-01 · requisitos.docx", from the "=== DOC-01 · name ==="
    // lines of the documentation file (DocumentationFile.Build).
    private static Dictionary<string, string> DocumentNames(string documentation) =>
        documentation.Split('\n')
            .Where(line => line.StartsWith("=== ", StringComparison.Ordinal) && line.TrimEnd().EndsWith(" ===", StringComparison.Ordinal))
            .Select(line => line.TrimEnd()[4..^4])
            .Select(title => (Id: title.Split(' ', 2)[0], Title: title))
            .DistinctBy(d => d.Id)
            .ToDictionary(d => d.Id, d => d.Title);

    private static string DocumentOf(string fragmentId, Dictionary<string, string> names)
    {
        var doc = fragmentId.Split('#')[0];
        return names.GetValueOrDefault(doc, doc);
    }

    private static string Text(JsonObject r, string field) =>
        r[field] is JsonValue v && v.TryGetValue<string>(out var s) ? s : "";

    private static bool Flag(JsonObject r, string field) =>
        r[field] is JsonValue v && v.TryGetValue<bool>(out var b) && b;

    private static List<string> List(JsonObject r, string field) =>
        r[field] is JsonArray a ? a.OfType<JsonValue>().Select(v => v.TryGetValue<string>(out var s) ? s : "").Where(s => s.Length > 0).ToList() : [];
}
