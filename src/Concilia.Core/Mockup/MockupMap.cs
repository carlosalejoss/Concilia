using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Concilia.Core.Mockup;

// One screen of the prototype: its id ("P01"), title, what it shows and the
// stories it solves.
public record MockupScreen(string Id, string Title, string Description, IReadOnlyList<string> StoryIds);

// The map of the prototype (output/prototipo/mapa.json), written in the
// design step (prompts v0.6): the screens the application would really have,
// in the order of the menu, with the stories each one solves. A story can be
// in more than one screen; every story of the backlog is in at least one.
//
//   {"pantallas":[{"id":"P01","titulo":"Reservas","descripcion":"…","historias":["HU-001","HU-004"]}]}
public record MockupMap(IReadOnlyList<MockupScreen> Screens)
{
    private static readonly Regex ScreenId = new(@"^P\d{2}$");

    // Each screen is built in a run of its own: more than this would be a
    // catalog of stories, not an application.
    public const int MaxScreens = 20;

    // Reads the map, or returns why it cannot be used. Errors start with the
    // element and a code, like the other checks, and go to the repair.
    public static (MockupMap? Map, IReadOnlyList<string> Errors) Parse(string? json, IReadOnlyList<string> storyIds)
    {
        const string file = MockupTask.MapFile;
        if (json is null)
            return (null, [$"{file} FICHERO: no se ha escrito el mapa de pantallas."]);

        JsonArray? screens;
        try
        {
            screens = JsonNode.Parse(json)?["pantallas"] as JsonArray;
        }
        catch (JsonException e)
        {
            return (null, [$"{file} JSON: no es un JSON válido ({e.Message})."]);
        }
        if (screens is null || screens.Count == 0)
            return (null, [$"{file} MAPA: falta la lista \"pantallas\" o está vacía."]);

        var errors = new List<string>();
        var parsed = new List<MockupScreen>();
        foreach (var node in screens)
        {
            var id = Text(node?["id"]);
            var title = Text(node?["titulo"]);
            var stories = (node?["historias"] as JsonArray ?? []).Select(Text).Where(s => s.Length > 0).ToList();
            if (!ScreenId.IsMatch(id))
                errors.Add($"{(id.Length > 0 ? id : "(sin id)")} MAPA-ID: el id de una pantalla es P01, P02…");
            else if (parsed.Any(s => s.Id == id))
                errors.Add($"{id} MAPA-ID: hay dos pantallas con este id.");
            if (title.Length == 0)
                errors.Add($"{id} MAPA-TITULO: la pantalla no tiene título.");
            if (stories.Count == 0)
                errors.Add($"{id} MAPA-HU: la pantalla no resuelve ninguna historia.");
            foreach (var unknown in stories.Where(s => !storyIds.Contains(s)))
                errors.Add($"{id} MAPA-HU: {unknown} no es una historia del backlog.");
            parsed.Add(new MockupScreen(id, title, Text(node?["descripcion"]), stories.Distinct().ToList()));
        }

        if (parsed.Count > MaxScreens)
            errors.Add($"{file} MAPA: tiene {parsed.Count} pantallas; el máximo es {MaxScreens}. Junta en una pantalla las historias que se hacen en el mismo sitio.");
        var covered = parsed.SelectMany(s => s.StoryIds).ToHashSet();
        foreach (var missing in storyIds.Where(id => !covered.Contains(id)))
            errors.Add($"{missing} MAPA-HU: la historia no está en ninguna pantalla.");

        return errors.Count == 0 ? (new MockupMap(parsed), []) : (null, errors);
    }

    // Loads a map that was already checked (from the job's folder).
    public static MockupMap Load(string json, IReadOnlyList<string> storyIds) =>
        Parse(json, storyIds).Map ?? throw new InvalidOperationException("The map of the prototype is not valid.");

    private static string Text(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text.Trim() : "";
}
