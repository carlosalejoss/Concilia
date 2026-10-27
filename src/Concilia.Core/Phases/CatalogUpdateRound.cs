using System.Text;
using System.Text.Json.Nodes;
using Concilia.Core.Catalog;

namespace Concilia.Core.Phases;

// The inputs of an update of the validated catalog with some iterations
// (milestone 22, step 113, prompts v0.13). A and B read as in a centred round
// (option C): the documentation of those iterations, the earlier fragments
// related to it and the related requirements, entirely; the whole catalog and
// documentation, only to consult. The arbiter reads the two proposals, the
// matching of the new requirements (also against the catalog) and the
// modifications and deletions grouped by requirement, aligned by its id
// without AI; it consults the fragments the changes cite.
public static class CatalogUpdateRound
{
    public const string ChangesFile = "input/cambios.txt";

    public static IReadOnlyList<InputFile> InputFilesOf(PhaseDefinition phase, PhaseInput input)
    {
        var files = new List<InputFile>();
        if (phase.IsArbiter)
        {
            files.Add(new InputFile(PhaseTask.Proposal1File, ProposalIds.Prefix(phase.Kind, input.Proposal1, 1), Required: true));
            files.Add(new InputFile(PhaseTask.Proposal2File, ProposalIds.Prefix(phase.Kind, input.Proposal2, 2), Required: true));
            if (input.Matching.Length > 0)
                files.Add(new InputFile(Matching.MatchingFile.Path, input.Matching, Required: true));
            if (input.Changes.Length > 0)
                files.Add(new InputFile(ChangesFile, input.Changes, Required: true));
            files.Add(new InputFile(SourcesFile.Path, Sources(input.Documentation, input.Proposal1, input.Proposal2), Required: false));
            return files;
        }
        files.Add(new InputFile(FocusedRound.NewDocumentationFile, input.NewDocumentation, Required: true));
        if (input.RelatedFragments.Length > 0)
            files.Add(new InputFile(FocusedRound.RelatedFragmentsFile, input.RelatedFragments, Required: true));
        files.Add(new InputFile(FocusedRound.RelatedRequirementsFile, ReadingPlan.OnePerLine(input.RelatedRequirements), Required: true));
        files.Add(new InputFile(PhaseTask.CatalogFile, ReadingPlan.OnePerLine(input.Catalog), Required: false));
        files.Add(new InputFile(DocumentationFile.Path, input.Documentation, Required: false));
        return files;
    }

    // {{RESPUESTAS}} of an update: the answered questions, and nueva="si" in
    // those answered after validating the catalog it starts from.
    public static string AnswersBlock(IEnumerable<PhaseQuestion> questions, IReadOnlySet<string> newAnswers) =>
        string.Join("\n", questions.Where(q => q.Status == "respondida").Select(q =>
            $"<pregunta id=\"{q.Id}\"{(newAnswers.Contains(q.Id) ? " nueva=\"si\"" : "")}><texto>{Fragmenter.SafeText(q.Text)}</texto><respuesta>{Fragmenter.SafeText(q.Answer ?? "")}</respuesta></pregunta>"));

    // The modifications and deletions of a proposal, each one with its action
    // ("modificar" or "eliminar").
    public static IReadOnlyList<(string Id, string Action, JsonObject Change)> ChangesOf(string proposal)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(proposal);
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
        IEnumerable<(string, string, JsonObject)> Of(string list, string action) =>
            (root?[list] as JsonArray ?? []).OfType<JsonObject>()
                .Where(c => c["id"] is JsonValue)
                .Select(c => (c["id"]!.GetValue<string>(), action, c));
        return Of("modificados", "modificar").Concat(Of("eliminados", "eliminar")).ToList();
    }

    // The requirements of the catalog with some change in either proposal, in order.
    public static IReadOnlyList<string> Targets(string proposal1, string proposal2) =>
        ChangesOf(proposal1).Concat(ChangesOf(proposal2)).Select(c => c.Id).Distinct().Order(StringComparer.Ordinal).ToList();

    // input/cambios.txt: for each requirement with some change, its validated
    // text and what each proposal says, or "—":
    //   RF-012 «El sistema DEBERÁ…»
    //     1: modificar → «…» · motivo: … · fuentes: DOC-08#F003
    //     2: eliminar · motivo: … · fuentes: DOC-08#F004
    public static string Changes(string proposal1, string proposal2, string catalogJson)
    {
        var current = (JsonNode.Parse(catalogJson.Length > 0 ? catalogJson : "{}")?["requisitos"] as JsonArray ?? []).OfType<JsonObject>()
            .Where(r => r["id"] is JsonValue)
            .ToDictionary(r => r["id"]!.GetValue<string>(), r => r["texto"]?.GetValue<string>() ?? "");
        var byProposal = new[] { ChangesOf(proposal1), ChangesOf(proposal2) };
        var text = new StringBuilder();
        foreach (var id in Targets(proposal1, proposal2))
        {
            text.Append($"{id} «{current.GetValueOrDefault(id, "")}»\n");
            for (var n = 0; n < 2; n++)
            {
                text.Append($"  {n + 1}: ");
                var change = byProposal[n].FirstOrDefault(c => c.Id == id);
                if (change.Change is null)
                {
                    text.Append("—\n");
                    continue;
                }
                text.Append(change.Action);
                if (change.Action == "modificar")
                    text.Append($" → «{change.Change["texto"]?.GetValue<string>()}»");
                text.Append($" · motivo: {change.Change["motivo"]?.GetValue<string>()} · fuentes: {string.Join(", ", Strings(change.Change["fuentes"]))}");
                if (Strings(change.Change["preguntas"]).Count > 0)
                    text.Append($" · preguntas: {string.Join(", ", Strings(change.Change["preguntas"]))}");
                text.Append('\n');
            }
        }
        return text.ToString();
    }

    // input/fuentes.txt of the arbiter: the fragments that the changes of the
    // two proposals cite (new, modified and deleted), as in the documentation.
    public static string Sources(string documentation, string proposal1, string proposal2)
    {
        var cited = new HashSet<string>();
        foreach (var proposal in new[] { proposal1, proposal2 })
        {
            JsonNode? root;
            try
            {
                root = JsonNode.Parse(proposal);
            }
            catch (System.Text.Json.JsonException)
            {
                continue;
            }
            foreach (var list in new[] { "nuevos", "modificados", "eliminados" })
                foreach (var change in (root?[list] as JsonArray ?? []).OfType<JsonObject>())
                    cited.UnionWith(Strings(change["fuentes"]));
        }
        return SourcesFile.Filter(documentation, cited);
    }

    private static List<string> Strings(JsonNode? list) =>
        list is JsonArray a ? a.OfType<JsonValue>().Select(v => v.TryGetValue<string>(out var s) ? s : null).OfType<string>().ToList() : [];
}
