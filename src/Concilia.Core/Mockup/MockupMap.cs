using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Concilia.Core.Mockup;

// One screen of the prototype: its id ("P01"), title, what it shows and the
// stories it solves; since prompts v0.9, also the application it belongs to
// ("App del socio"), the roles that see it, whether it is an entry of that
// application's menu, and the screens from which one gets to it.
public record MockupScreen(
    string Id, string Title, string Description, IReadOnlyList<string> StoryIds,
    string Application = "", IReadOnlyList<string>? Roles = null, bool Menu = true, IReadOnlyList<string>? From = null)
{
    public IReadOnlyList<string> RolesOrEmpty => Roles ?? [];
    public IReadOnlyList<string> FromOrEmpty => From ?? [];
}

// The map of the prototype (output/prototipo/mapa.json), written in the
// design step: the screens the application would really have, with the
// stories each one solves. A story can be in more than one screen; every
// story of the backlog is in at least one.
//
// Prompts v0.9 (15-10-2026): the prototype is a working application, not an
// index of screens (the v0.8 mockup had a menu with every screen, because the
// design prompt asked for it). The map says how the application is used: it
// starts at the sign-in screen ("inicio"), each role enters its application,
// whose menu has only its main sections ("menu": true), and every other
// screen is reached from others ("desde"), by a button, a card or a row.
//
//   {"inicio":"P01","pantallas":[{"id":"P01","titulo":"Iniciar sesión","aplicacion":"Común",
//     "roles":["socio","junta"],"menu":false,"desde":[],"descripcion":"…","historias":["HU-001"]}, …]}
public record MockupMap(IReadOnlyList<MockupScreen> Screens, string Start = "")
{
    private static readonly Regex ScreenId = new(@"^P\d{2}$");

    // There is no maximum of screens (decision of the author, 15-10-2026): the
    // application has the ones it needs. Above this number the design gets a
    // warning, because each screen is a run of its own (more time and cost).
    public const int ManyScreens = 25;

    // The links of the map: from a screen to another one reached from it.
    public IEnumerable<(string From, MockupScreen To)> Links =>
        Screens.SelectMany(to => to.FromOrEmpty.Select(from => (from, to)));

    // Reads the map, or returns why it cannot be used. Errors start with the
    // element and a code, like the other checks, and go to the repair.
    public static (MockupMap? Map, IReadOnlyList<string> Errors) Parse(string? json, IReadOnlyList<string> storyIds)
    {
        const string file = MockupTask.MapFile;
        if (json is null)
            return (null, [$"{file} FICHERO: no se ha escrito el mapa de pantallas."]);

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException e)
        {
            return (null, [$"{file} JSON: no es un JSON válido ({e.Message})."]);
        }
        if (root?["pantallas"] is not JsonArray screens || screens.Count == 0)
            return (null, [$"{file} MAPA: falta la lista \"pantallas\" o está vacía."]);

        var start = Text(root["inicio"]);
        var errors = new List<string>();
        var parsed = new List<MockupScreen>();
        foreach (var node in screens)
        {
            var id = Text(node?["id"]);
            var title = Text(node?["titulo"]);
            var stories = Texts(node?["historias"]);
            if (!ScreenId.IsMatch(id))
                errors.Add($"{(id.Length > 0 ? id : "(sin id)")} MAPA-ID: el id de una pantalla es P01, P02…");
            else if (parsed.Any(s => s.Id == id))
                errors.Add($"{id} MAPA-ID: hay dos pantallas con este id.");
            if (title.Length == 0)
                errors.Add($"{id} MAPA-TITULO: la pantalla no tiene título.");
            // The sign-in screen may solve no story: the backlog may not have one.
            if (stories.Count == 0 && id != start)
                errors.Add($"{id} MAPA-HU: la pantalla no resuelve ninguna historia.");
            foreach (var unknown in stories.Where(s => !storyIds.Contains(s)))
                errors.Add($"{id} MAPA-HU: {unknown} no es una historia del backlog.");

            var application = Text(node?["aplicacion"]);
            var roles = Texts(node?["roles"]);
            if (application.Length == 0)
                errors.Add($"{id} MAPA-APP: falta la aplicación a la que pertenece la pantalla (\"aplicacion\").");
            if (roles.Count == 0)
                errors.Add($"{id} MAPA-ROL: falta qué roles ven la pantalla (\"roles\").");
            var menu = node?["menu"] is JsonValue m && m.TryGetValue<bool>(out var isMenu) && isMenu;
            parsed.Add(new MockupScreen(id, title, Text(node?["descripcion"]), stories.Distinct().ToList(),
                application, roles, menu, Texts(node?["desde"]).Distinct().ToList()));
        }

        var covered = parsed.SelectMany(s => s.StoryIds).ToHashSet();
        foreach (var missing in storyIds.Where(id => !covered.Contains(id)))
            errors.Add($"{missing} MAPA-HU: la historia no está en ninguna pantalla.");
        errors.AddRange(NavigationErrors(parsed, start));

        return errors.Count == 0 ? (new MockupMap(parsed, start), []) : (null, errors);
    }

    // The navigation of the map: a sign-in screen that exists, links between
    // screens that exist, and every screen reachable from the sign-in: after
    // it, the menu entries; from each screen reached, the screens that say
    // they are reached from it.
    private static IEnumerable<string> NavigationErrors(List<MockupScreen> screens, string start)
    {
        var ids = screens.Select(s => s.Id).ToHashSet();
        if (!ids.Contains(start))
        {
            yield return $"{MockupTask.MapFile} MAPA-INICIO: \"inicio\" debe ser el id de la pantalla de inicio de sesión.";
            yield break;
        }
        foreach (var screen in screens)
            foreach (var from in screen.FromOrEmpty.Where(f => !ids.Contains(f)))
                yield return $"{screen.Id} MAPA-DESDE: {from} no es una pantalla del mapa.";

        var reached = new HashSet<string> { start };
        foreach (var screen in screens.Where(s => s.Menu))
            reached.Add(screen.Id);
        bool grew;
        do
        {
            grew = false;
            foreach (var screen in screens.Where(s => !reached.Contains(s.Id) && s.FromOrEmpty.Any(reached.Contains)))
                grew |= reached.Add(screen.Id);
        } while (grew);

        foreach (var screen in screens.Where(s => !reached.Contains(s.Id)))
            yield return $"{screen.Id} MAPA-ACCESO: no se llega a esta pantalla: no es una entrada de menú (\"menu\") y ninguna pantalla a la que se llega la tiene en \"desde\".";
    }

    // What is worth a look but is not an error.
    public IReadOnlyList<string> Warnings() =>
        Screens.Count > ManyScreens
            ? [$"{MockupTask.MapFile} MAPA-PANTALLAS: tiene {Screens.Count} pantallas; cada una es una ejecución aparte, con su tiempo y su coste. Comprueba que no hay pantallas que se puedan juntar."]
            : [];

    // Loads a map that was already checked (from the job's folder).
    public static MockupMap Load(string json, IReadOnlyList<string> storyIds) =>
        Parse(json, storyIds).Map ?? throw new InvalidOperationException("The map of the prototype is not valid.");

    private static string Text(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text.Trim() : "";

    private static List<string> Texts(JsonNode? node) =>
        (node as JsonArray ?? []).Select(Text).Where(s => s.Length > 0).ToList();
}
