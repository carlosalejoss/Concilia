using System.Text.Json.Nodes;
using Concilia.Core.Catalog;
using Concilia.Core.Projects;

namespace Concilia.Core.Deliverables.Export;

// A question of the project as the export shows it: its answer and whether
// that answer is a guess of the AI accepted as it was (step 93). Iterations:
// those of its sources (step 111), filled by CatalogExport.Build.
public record ExportQuestion(string Id, string Text, string? Answer, bool AiGuess, IReadOnlyList<int>? Iterations = null);

public record ExportRequirement(
    string Id, string Notation, string Obligation, string Text,
    IReadOnlyList<string> Sources, IReadOnlyList<string> Questions,
    // Marks for whoever reads the document: it cites a guess of the AI, or it
    // still has a discrepancy the user has not reviewed (only in a draft).
    bool AiGuess, bool PendingDiscrepancy,
    // The iterations of the project it comes from (step 111): those of its sources and questions.
    IReadOnlyList<int> Iterations);

public record ExportSection(string Type, string Title, IReadOnlyList<ExportRequirement> Requirements);

// A cited fragment for the annex of sources, with the name of its document.
// Text is null if the fragment is not in the documentation the catalog was
// generated from (a citation the user typed by hand). Iteration: that of its
// document (step 111), if known.
public record ExportSource(string Id, string Document, string Section, string? Text, int? Iteration = null);

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
    // Step 140: the language of the project, for the texts of the platform.
    public ExportLanguage Language { get; init; } = ExportLanguage.Spanish;

    // The order and names of the web (CatalogEditor.tsx), in Spanish and in English.
    public static readonly (string Type, string Spanish, string English)[] Types =
    [
        ("RF", "Requisitos funcionales", "Functional requirements"),
        ("RNF", "Requisitos no funcionales", "Non-functional requirements"),
        ("RI", "Requisitos de integración", "Integration requirements"),
        ("RA", "Requisitos de arquitectura", "Architecture requirements"),
        ("RT", "Requisitos técnicos", "Technical requirements"),
    ];

    public int Total => Sections.Sum(s => s.Requirements.Count);

    // The texts both files share, so the Word and the PDF say the same.
    public string Title => Language.Text("Catálogo de requisitos", "Requirements catalog");

    // The name of the downloaded file, without its extension.
    public string FileName(string slug) => Language.Text($"catalogo-{slug}", $"catalog-{slug}");

    public string Status => Validated
        ? Language.Text($"Validado el {Language.Date(ValidatedAt ?? Date)}", $"Validated on {Language.Date(ValidatedAt ?? Date)}")
        : Language.Text("Borrador: todavía no está validado", "Draft: not validated yet");

    public string Generated => Language.Text(
        $"Exportado desde Concilia el {Language.Date(Date)} a las {Date:HH:mm}.",
        $"Exported from Concilia on {Language.Date(Date)} at {Date:HH:mm}.");

    public string Footer => $"{Project} · {Title} · {(Validated ? Language.Text("validado", "validated") : Language.Text("BORRADOR", "DRAFT"))}";

    // "· página 3 de 10" in the footer: the number of the page and the total go between them.
    public string Page => Language.Text("página", "page");
    public string PageOf => Language.Text("de", "of");

    // "27 requisitos:" on the cover, before the count of each type.
    public string Count => Language.Text($"{Total} requisitos:", $"{Total} requirements:");

    public string Legend => Language.Text(
        "Cada requisito está escrito en EARS o en MASTeR, con su obligatoriedad: shall (obligatorio), should " +
        "(recomendable) o may (opcional). Las fuentes, como DOC-01#F007, son fragmentos de la documentación del " +
        "proyecto: su texto está en el anexo A. Las preguntas, como P-003, se hicieron al cliente: están en el anexo B " +
        "con su respuesta.",
        "Each requirement is written in EARS or in MASTeR, with its obligation: shall (mandatory), should " +
        "(recommended) or may (optional). The sources, such as DOC-01#F007, are fragments of the documentation of the " +
        "project: their text is in appendix A. The questions, such as P-003, were asked to the client: they are in appendix B " +
        "with their answer.");

    public string AiGuessMark => Language.Text(
        "Suposición de la IA: cita una pregunta respondida con una suposición de la IA aceptada sin cambios.",
        "AI assumption: it cites a question answered with an assumption of the AI accepted without changes.");
    public string DiscrepancyMark => Language.Text(
        "Discrepancia sin revisar: los dos modelos no coincidían y todavía no se ha revisado.",
        "Unreviewed discrepancy: the two models did not agree and it has not been reviewed yet.");

    public string SourcesTitle => Language.Text("Anexo A. Fuentes citadas", "Appendix A. Cited sources");
    public string QuestionsTitle => Language.Text("Anexo B. Preguntas y respuestas", "Appendix B. Questions and answers");
    public string NoSources => Language.Text("Ningún requisito cita fragmentos de la documentación.", "No requirement cites fragments of the documentation.");
    public string NotInDocumentation => Language.Text("No está en la documentación de la que salió el catálogo.", "It is not in the documentation the catalog was generated from.");
    public string NoQuestions => Language.Text("Ningún requisito cita preguntas al cliente.", "No requirement cites questions to the client.");

    // The language of the text of the Word file, for its spelling checker.
    public string WordLanguage => Language.Text("es-ES", "en-US");

    public string Obligation(string value) => value switch
    {
        "shall" => Language.Text("shall (obligatorio)", "shall (mandatory)"),
        "should" => Language.Text("should (recomendable)", "should (recommended)"),
        "may" => Language.Text("may (opcional)", "may (optional)"),
        _ => value,
    };

    public string Details(ExportRequirement r) => $"{r.Notation} · {Obligation(r.Obligation)}";

    // "Fuentes: DOC-01#F003, DOC-01#F007 · Preguntas: P-002 · Iteración: 1" (what it has).
    public string References(ExportRequirement r) => string.Join(" · ", new[]
    {
        r.Sources.Count > 0 ? $"{Language.Text("Fuentes", "Sources")}: {string.Join(", ", r.Sources)}" : null,
        r.Questions.Count > 0 ? $"{Language.Text("Preguntas", "Questions")}: {string.Join(", ", r.Questions)}" : null,
        Iterations(r.Iterations),
    }.Where(s => s is not null));

    // "Iteración: 2" or "Iteraciones: 1, 2" (step 111); null without any.
    public string? Iterations(IReadOnlyList<int> numbers) => numbers.Count switch
    {
        0 => null,
        1 => $"{Language.Text("Iteración", "Iteration")}: {numbers[0]}",
        _ => $"{Language.Text("Iteraciones", "Iterations")}: {string.Join(", ", numbers)}",
    };

    // The iterations of each requirement, by id: those of the stories of the backlog.
    public IReadOnlyDictionary<string, IReadOnlyList<int>> IterationsById =>
        Sections.SelectMany(s => s.Requirements).GroupBy(r => r.Id).ToDictionary(g => g.Key, g => g.First().Iterations);

    public string Answer(ExportQuestion q) =>
        q.Answer is { Length: > 0 } answer
            ? q.AiGuess
                ? Language.Text($"Respuesta (suposición de la IA): {answer}", $"Answer (assumption of the AI): {answer}")
                : Language.Text($"Respuesta: {answer}", $"Answer: {answer}")
            : Language.Text("Sin respuesta.", "No answer.");

    // catalogJson: the working copy (CatalogDraft). fragments and
    // documentation: those of the plan of the job that generated it.
    // questions: all those of the project. iterations: those of the elements
    // of the project (step 111); without them, no element says its iteration.
    // language: that of the project, "es" or "en" (step 140).
    public static CatalogExport Build(
        string project, bool validated, DateTimeOffset date, DateTimeOffset? validatedAt, string catalogJson,
        IReadOnlyList<Fragment> fragments, string documentation, IReadOnlyList<ExportQuestion> questions,
        ElementIterations? iterations = null, string language = "es")
    {
        var exportLanguage = new ExportLanguage(language);
        if (iterations is not null)
            questions = questions.Select(q => q with { Iterations = iterations.OfQuestion(q.Id) }).ToList();
        var guesses = questions.Where(q => q.AiGuess).Select(q => q.Id).ToHashSet();
        var requirements = (JsonNode.Parse(catalogJson)!["requisitos"] as JsonArray ?? []).OfType<JsonObject>().ToList();

        var sections = new List<ExportSection>();
        foreach (var (type, spanish, english) in Types)
        {
            var ofType = requirements
                .Where(r => Text(r, "tipo") == type)
                .Select(r => new ExportRequirement(
                    Text(r, "id"), Text(r, "notacion"), Text(r, "obligatoriedad"), Text(r, "texto"),
                    List(r, "fuentes"), List(r, "preguntas"),
                    AiGuess: List(r, "preguntas").Any(guesses.Contains),
                    PendingDiscrepancy: Flag(r, "discrepancia") && !Flag(r, "revisada"),
                    Iterations: iterations?.OfRequirement(List(r, "fuentes"), List(r, "preguntas")) ?? []))
                .ToList();
            if (ofType.Count > 0)
                sections.Add(new ExportSection(type, exportLanguage.Text(spanish, english), ofType));
        }

        // Each cited fragment once, in the order of the documentation; those
        // that are not in it, at the end.
        var cited = sections.SelectMany(s => s.Requirements).SelectMany(r => r.Sources).ToHashSet();
        var names = DocumentNames(documentation);
        var known = fragments.Where(f => cited.Contains(f.Id)).ToList();
        var sources = known
            .Select(f => new ExportSource(f.Id, names.GetValueOrDefault(f.Source, f.Source), f.Section, f.Text, iterations?.OfFragment(f.Id)))
            .Concat(cited.Except(known.Select(f => f.Id)).Order(StringComparer.Ordinal)
                .Select(id => new ExportSource(id, DocumentOf(id, names), "", null, iterations?.OfFragment(id))))
            .ToList();

        var citedQuestions = sections.SelectMany(s => s.Requirements).SelectMany(r => r.Questions).ToHashSet();
        var shownQuestions = questions.Where(q => citedQuestions.Contains(q.Id)).OrderBy(q => q.Id, StringComparer.Ordinal).ToList();

        return new CatalogExport(project, validated, date, validatedAt, sections, sources, shownQuestions) { Language = exportLanguage };
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
