using System.Text.Json.Nodes;

namespace Concilia.Core.Projects;

// The iterations of each element of the project (milestone 22, step 111),
// computed by the platform and not by the models, so they are always right,
// cost no tokens and also hold for what the user adds by hand: a fragment
// belongs to the iteration of its document (DOC-03#F010 → DOC-03 → its file);
// a question, to those of its sources; a requirement, to those of its sources
// and of the questions it cites; and a story, to those of its requirements.
// An element may rest on documentation of several iterations, so each one has
// a list, in order; it is empty if nothing it cites is known.
public sealed class ElementIterations(
    IReadOnlyDictionary<string, int> documents,
    IReadOnlyDictionary<string, IReadOnlyList<string>> questionSources)
{
    // "DOC-03" → 2: the iteration of each document of the project.
    public IReadOnlyDictionary<string, int> Documents => documents;

    // The iteration of a fragment ("DOC-03#F010"), or null if its document is not known.
    public int? OfFragment(string fragmentId) =>
        documents.TryGetValue(fragmentId.Split('#')[0], out var number) ? number : null;

    public IReadOnlyList<int> OfFragments(IEnumerable<string> fragmentIds) =>
        Sorted(fragmentIds.Select(OfFragment).OfType<int>());

    public IReadOnlyList<int> OfQuestion(string code) =>
        questionSources.TryGetValue(code, out var sources) ? OfFragments(sources) : [];

    public IReadOnlyList<int> OfRequirement(IEnumerable<string> sources, IEnumerable<string> questions) =>
        Sorted(OfFragments(sources).Concat(questions.SelectMany(OfQuestion)));

    // The iterations of each requirement of a catalog (CatalogDraft), by id:
    // what the stories of the backlog need.
    public IReadOnlyDictionary<string, IReadOnlyList<int>> OfCatalog(string catalogJson)
    {
        var requirements = JsonNode.Parse(catalogJson)?["requisitos"] as JsonArray ?? [];
        var result = new Dictionary<string, IReadOnlyList<int>>();
        foreach (var r in requirements.OfType<JsonObject>())
        {
            if (r["id"] is JsonValue v && v.TryGetValue<string>(out var id))
                result[id] = OfRequirement(Strings(r["fuentes"]), Strings(r["preguntas"]));
        }
        return result;
    }

    private static IEnumerable<string> Strings(JsonNode? list) =>
        list is JsonArray items ? items.OfType<JsonValue>().Select(v => v.TryGetValue<string>(out var s) ? s : null).OfType<string>() : [];

    // A story: the iterations of its requirements (requirementIterations, by id).
    public static IReadOnlyList<int> OfStory(IEnumerable<string> requirements, IReadOnlyDictionary<string, IReadOnlyList<int>> requirementIterations) =>
        Sorted(requirements.SelectMany(id => requirementIterations.TryGetValue(id, out var numbers) ? numbers : []));

    private static List<int> Sorted(IEnumerable<int> numbers) => numbers.Distinct().Order().ToList();
}
