using System.Text;
using System.Text.Json.Nodes;

namespace Concilia.Core.Deliverables;

public record ChangedRequirement(string Id, string Before, string After);
public record RequirementText(string Id, string Type, string Text);
public record AffectedStory(string Id, string EpicId, string EpicTitle, IReadOnlyList<string> Modified, IReadOnlyList<string> Deleted);

// What an update of the backlog has to look at (milestone 22, step 114),
// computed by the platform without AI: from the catalog the backlog was
// generated from to the validated one now, the requirements modified, deleted
// and new (those not in any story yet), and the stories that cite a modified
// or deleted one. The models only work on that.
public record BacklogImpact(
    IReadOnlyList<ChangedRequirement> Modified, IReadOnlyList<RequirementText> Deleted,
    IReadOnlyList<RequirementText> New, IReadOnlyList<AffectedStory> Affected)
{
    public bool IsEmpty => Modified.Count == 0 && Deleted.Count == 0 && New.Count == 0;

    // fromCatalog and toCatalog: as the next phases receive them (CatalogDraft.ForNextPhase);
    // backlog: the validated backlog (BacklogDraft.ForNextPhase).
    public static BacklogImpact Compute(string fromCatalog, string toCatalog, string backlog)
    {
        var before = Requirements(fromCatalog);
        var after = Requirements(toCatalog);
        var modified = after.Values.Where(r => before.TryGetValue(r.Id, out var old) && old.Json != r.Json)
            .Select(r => new ChangedRequirement(r.Id, before[r.Id].Text, r.Text)).ToList();
        var deleted = before.Values.Where(r => !after.ContainsKey(r.Id)).Select(r => new RequirementText(r.Id, r.Type, r.Text)).ToList();

        var epics = (JsonNode.Parse(backlog)?["epicas"] as JsonArray ?? []).OfType<JsonObject>().ToList();
        var cited = epics.SelectMany(Stories).SelectMany(h => Strings(h["requisitos"])).ToHashSet();
        var created = after.Values.Where(r => !before.ContainsKey(r.Id) && !cited.Contains(r.Id))
            .Select(r => new RequirementText(r.Id, r.Type, r.Text)).ToList();

        var modifiedIds = modified.Select(m => m.Id).ToHashSet();
        var deletedIds = deleted.Select(d => d.Id).ToHashSet();
        var affected = new List<AffectedStory>();
        foreach (var e in epics)
            foreach (var h in Stories(e))
            {
                var requirements = Strings(h["requisitos"]);
                var m = requirements.Where(modifiedIds.Contains).ToList();
                var d = requirements.Where(deletedIds.Contains).ToList();
                if (m.Count + d.Count > 0)
                    affected.Add(new AffectedStory(Str(h, "id"), Str(e, "id"), Str(e, "titulo"), m, d));
            }
        return new BacklogImpact(modified, deleted, created, affected);
    }

    // input/impacto.txt: what the models read first.
    public string Text()
    {
        var text = new StringBuilder();
        text.Append("Requisitos modificados:\n");
        foreach (var m in Modified)
            text.Append($"- {m.Id} antes: «{m.Before}» · ahora: «{m.After}»\n");
        text.Append("Requisitos eliminados:\n");
        foreach (var d in Deleted)
            text.Append($"- {d.Id} «{d.Text}»\n");
        text.Append("Requisitos nuevos sin historia:\n");
        foreach (var n in New)
            text.Append($"- {n.Id} ({n.Type}) «{n.Text}»\n");
        text.Append("Historias afectadas:\n");
        foreach (var a in Affected)
        {
            var why = a.Modified.Select(id => $"{id} (modificado)").Concat(a.Deleted.Select(id => $"{id} (eliminado)"));
            text.Append($"- {a.Id} ({a.EpicId} «{a.EpicTitle}»): cita {string.Join(", ", why)}\n");
        }
        return text.ToString();
    }

    // input/historias_afectadas.json: those stories entire, with their epic.
    public string AffectedJson(string backlog)
    {
        var ids = Affected.Select(a => a.Id).ToHashSet();
        var stories = new JsonArray();
        foreach (var e in (JsonNode.Parse(backlog)?["epicas"] as JsonArray ?? []).OfType<JsonObject>())
            foreach (var h in Stories(e).Where(h => ids.Contains(Str(h, "id"))))
            {
                var copy = new JsonObject { ["epica"] = new JsonObject { ["id"] = Str(e, "id"), ["titulo"] = Str(e, "titulo") } };
                foreach (var (key, value) in h)
                    copy[key] = value?.DeepClone();
                stories.Add(copy);
            }
        return new JsonObject { ["historias"] = stories }.ToJsonString();
    }

    private record Requirement(string Id, string Type, string Text, string Json);

    private static Dictionary<string, Requirement> Requirements(string catalog) =>
        (JsonNode.Parse(catalog.Length > 0 ? catalog : "{}")?["requisitos"] as JsonArray ?? []).OfType<JsonObject>()
            .Select(r => new Requirement(Str(r, "id"), Str(r, "tipo"), Str(r, "texto"), r.ToJsonString()))
            .GroupBy(r => r.Id).ToDictionary(g => g.Key, g => g.First());

    private static IEnumerable<JsonObject> Stories(JsonObject epic) => (epic["historias"] as JsonArray ?? []).OfType<JsonObject>();

    private static string Str(JsonObject o, string field) => o[field] is JsonValue v && v.TryGetValue<string>(out var s) ? s : "";

    private static List<string> Strings(JsonNode? list) =>
        list is JsonArray a ? a.OfType<JsonValue>().Select(v => v.TryGetValue<string>(out var s) ? s : null).OfType<string>().ToList() : [];
}
