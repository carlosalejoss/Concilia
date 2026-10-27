using System.Text;
using System.Text.Json.Nodes;

namespace Concilia.Core.Phases;

// The inputs of an update of the validated backlog with some iterations
// (milestone 22, step 114, prompts v0.13). A and B read entirely the impact
// computed by the platform (BacklogImpact) and the stories it affects; the
// validated catalog and backlog, only to consult. The arbiter reads the two
// proposals, the matching of the new stories (also against the backlog), the
// modifications and deletions grouped by story without AI and the impact; it
// consults the catalog.
public static class BacklogUpdateRound
{
    public const string ImpactFile = "input/impacto.txt";
    public const string AffectedFile = "input/historias_afectadas.json";
    public const string BacklogFile = "input/backlog.json";

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
                files.Add(new InputFile(CatalogUpdateRound.ChangesFile, input.Changes, Required: true));
            files.Add(new InputFile(ImpactFile, input.Impact, Required: true));
            files.Add(new InputFile(PhaseTask.CatalogFile, ReadingPlan.OnePerLine(input.Catalog), Required: false));
            return files;
        }
        files.Add(new InputFile(ImpactFile, input.Impact, Required: true));
        files.Add(new InputFile(AffectedFile, ReadingPlan.OnePerLine(input.AffectedStories), Required: true));
        files.Add(new InputFile(PhaseTask.CatalogFile, ReadingPlan.OnePerLine(input.Catalog), Required: false));
        files.Add(new InputFile(BacklogFile, ReadingPlan.OnePerLine(input.Backlog), Required: false));
        return files;
    }

    // The modifications and deletions of a proposal, each one with its action.
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
        return Of("modificadas", "modificar").Concat(Of("eliminadas", "eliminar")).ToList();
    }

    public static IReadOnlyList<string> Targets(string proposal1, string proposal2) =>
        ChangesOf(proposal1).Concat(ChangesOf(proposal2)).Select(c => c.Id).Distinct().Order(StringComparer.Ordinal).ToList();

    // input/cambios.txt of the arbiter: for each story with some change, how
    // it is now and what each proposal says, or "—":
    //   HU-004 «Anular una reserva» · Como empleado, quiero…, para…
    //     1: modificar → «…» · Como …; requisitos: RF-012 · motivo: …
    //     2: eliminar · motivo: …
    public static string Changes(string proposal1, string proposal2, string backlogJson)
    {
        var current = (JsonNode.Parse(backlogJson.Length > 0 ? backlogJson : "{}")?["epicas"] as JsonArray ?? []).OfType<JsonObject>()
            .SelectMany(e => (e["historias"] as JsonArray ?? []).OfType<JsonObject>())
            .Where(h => h["id"] is JsonValue)
            .GroupBy(h => h["id"]!.GetValue<string>())
            .ToDictionary(g => g.Key, g => g.First());
        var byProposal = new[] { ChangesOf(proposal1), ChangesOf(proposal2) };
        var text = new StringBuilder();
        foreach (var id in Targets(proposal1, proposal2))
        {
            text.Append($"{id} {(current.TryGetValue(id, out var story) ? Summary(story) : "«»")}\n");
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
                    text.Append($" → {Summary(change.Change)}; requisitos: {string.Join(", ", Strings(change.Change["requisitos"]))}");
                text.Append($" · motivo: {change.Change["motivo"]?.GetValue<string>()}\n");
            }
        }
        return text.ToString();
    }

    // «title» · Como …, quiero …, para …
    private static string Summary(JsonObject story) =>
        $"«{story["titulo"]?.GetValue<string>()}» · Como {story["como"]?.GetValue<string>()}, quiero {story["quiero"]?.GetValue<string>()}, para {story["para"]?.GetValue<string>()}";

    private static List<string> Strings(JsonNode? list) =>
        list is JsonArray a ? a.OfType<JsonValue>().Select(v => v.TryGetValue<string>(out var s) ? s : null).OfType<string>().ToList() : [];
}
