using System.Text;
using System.Text.Json.Nodes;

namespace Concilia.Core.Phases;

// A question of the project as a round centred on some iterations knows it:
// its code, text and status ("respondida", "descartada" or "pendiente").
public record FocusQuestion(string Code, string Text, string Status);

// What a round centred on some iterations adds to its plan (milestone 22,
// step 112, prompts v0.13). Null in the rounds of always ("Todas las
// iteraciones"), which stay exactly as with v0.12.
//   Iterations: those it centres on.
//   NewDocuments: the documents of those iterations ("DOC-08").
//   Questions: every question already asked, for the revisions (only of the
//   pending ones) and for the matching of the new ones against them ("previa").
//   NewAnswers (step 113, an update of the catalog): the answers given after
//   validating the catalog it starts from, the ones that may ask for changes.
public record FocusPlan(IReadOnlyList<int> Iterations, IReadOnlyList<string> NewDocuments, IReadOnlyList<FocusQuestion> Questions,
    IReadOnlyList<string>? NewAnswers = null)
{
    public IReadOnlySet<string> PendingCodes => Questions.Where(q => q.Status == "pendiente").Select(q => q.Code).ToHashSet();
}

// The input files of a round centred on some iterations (option C, decision
// of the author of 26-10-2026): A and B read entirely the documentation of
// those iterations, the earlier fragments related to it and, if there is a
// validated catalog, its related requirements (RelatedContext); the whole
// documentation and catalog are only consulted. The arbiter reads the two
// proposals, the matching and the revisions grouped by question, and consults
// the documentation.
public static class FocusedRound
{
    public const string NewDocumentationFile = "input/documentacion_nueva.txt";
    public const string RelatedFragmentsFile = "input/relacionados.txt";
    public const string RelatedRequirementsFile = "input/requisitos_relacionados.json";
    public const string RevisionsFile = "input/revisiones.txt";

    // The prompts v0.13 of the centred round (the rounds of always stay with v0.12).
    public const string PromptVersion = "v0.13";

    public static IReadOnlyList<InputFile> InputFilesOf(PhaseDefinition phase, PhaseInput input)
    {
        var files = new List<InputFile>();
        if (phase.IsArbiter)
        {
            files.Add(new InputFile(PhaseTask.Proposal1File, ProposalIds.Prefix(phase.Kind, input.Proposal1, 1), Required: true));
            files.Add(new InputFile(PhaseTask.Proposal2File, ProposalIds.Prefix(phase.Kind, input.Proposal2, 2), Required: true));
            if (input.Matching.Length > 0)
                files.Add(new InputFile(Matching.MatchingFile.Path, input.Matching, Required: true));
            if (input.Revisions.Length > 0)
                files.Add(new InputFile(RevisionsFile, input.Revisions, Required: true));
            files.Add(new InputFile(DocumentationFile.Path, input.Documentation, Required: false));
            return files;
        }
        files.Add(new InputFile(NewDocumentationFile, input.NewDocumentation, Required: true));
        if (input.RelatedFragments.Length > 0)
            files.Add(new InputFile(RelatedFragmentsFile, input.RelatedFragments, Required: true));
        files.Add(new InputFile(DocumentationFile.Path, input.Documentation, Required: false));
        if (input.Catalog.Length > 0)
        {
            files.Add(new InputFile(RelatedRequirementsFile, ReadingPlan.OnePerLine(input.RelatedRequirements), Required: true));
            files.Add(new InputFile(PhaseTask.CatalogFile, ReadingPlan.OnePerLine(input.Catalog), Required: false));
        }
        return files;
    }

    // {{ITERACIONES}}: the iterations it centres on and the iteration of each document.
    //   <foco>2</foco>
    //   <documento id="DOC-01" iteracion="1"/>
    public static string IterationsBlock(IReadOnlyList<int> focus, IReadOnlyDictionary<string, int> documents)
    {
        var text = new StringBuilder($"<foco>{string.Join(", ", focus)}</foco>");
        foreach (var (id, number) in documents.OrderBy(d => d.Key, StringComparer.Ordinal))
            text.Append($"\n<documento id=\"{id}\" iteracion=\"{number}\"/>");
        return text.ToString();
    }

    // input/revisiones.txt: the revisions of the two proposals grouped by the
    // pending question they refer to, aligned by its id without AI (decision
    // of the author). For each question, its current text and what each
    // proposal says, or "—":
    //   P-004 «¿Hay límite de duración por reserva?»
    //     1: reformular → «…» · motivo: … · fuentes: DOC-08#F003
    //     2: —
    public static string Revisions(string proposal1, string proposal2, IReadOnlyList<FocusQuestion> questions)
    {
        var byProposal = new[] { RevisionsOf(proposal1), RevisionsOf(proposal2) };
        var text = new StringBuilder();
        foreach (var code in Targets(proposal1, proposal2))
        {
            var current = questions.FirstOrDefault(q => q.Code == code)?.Text ?? "";
            text.Append($"{code} «{current}»\n");
            for (var n = 0; n < 2; n++)
            {
                var revision = byProposal[n].FirstOrDefault(r => Str(r, "pregunta") == code);
                text.Append($"  {n + 1}: ");
                if (revision is null)
                {
                    text.Append("—\n");
                    continue;
                }
                var action = Str(revision, "accion");
                text.Append(action);
                if (action == "reformular")
                    text.Append($" → «{Str(revision, "texto")}»");
                text.Append($" · motivo: {Str(revision, "motivo")} · fuentes: {string.Join(", ", StrList(revision, "fuentes"))}\n");
            }
        }
        return text.ToString();
    }

    // The pending questions with some revision in either proposal, in order.
    public static IReadOnlyList<string> Targets(string proposal1, string proposal2) =>
        RevisionsOf(proposal1).Concat(RevisionsOf(proposal2))
            .Select(r => Str(r, "pregunta")).OfType<string>()
            .Distinct().Order(StringComparer.Ordinal).ToList();

    // The revisions of a proposal, or none if it is not JSON or has none.
    public static IReadOnlyList<JsonObject> RevisionsOf(string proposal)
    {
        try
        {
            return JsonNode.Parse(proposal)?["revisiones"] is JsonArray list ? list.OfType<JsonObject>().ToList() : [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private static string? Str(JsonObject o, string field) => o[field] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    private static List<string> StrList(JsonObject o, string field) =>
        o[field] is JsonArray a ? a.OfType<JsonValue>().Select(v => v.TryGetValue<string>(out var s) ? s : null).OfType<string>().ToList() : [];
}
