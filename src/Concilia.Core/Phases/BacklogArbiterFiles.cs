using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Concilia.Core.Matching;

namespace Concilia.Core.Phases;

// Step 143 (prompts v0.14, decision of the author of 16-11-2026): what the
// arbiter of the backlog reads, smaller than the two whole proposals and the
// whole catalog.
//
// Why: in the step 131 (13-11-2026) the arbiter of the v0.12 did not fit in
// 128,000 tokens with the proposals of the new B (140 and 105 stories): a
// whole story takes about 378 tokens, so the two proposals were about 93,000,
// the catalog about 24,000 and, with the prompt and the base context, it
// needed about 140,000. Reading only the requirements it needs, as the step
// 107 did with the sources of the arbiter of the catalog, was not enough.
//
// So the arbiter gets:
//   input/propuesta_1.txt and _2.txt  one line per epic and per story: its
//       title, "como… quiero… para…", requirements, the name, id and numbers
//       of each criterion, the ids of its tasks and the lines of
//       criterios.txt with their text (about 100 tokens per story); read
//       entirely
//   input/criterios.txt  the text of every criterion and task, one per line,
//       grouped by story; only to consult what it needs
//   input/requisitos.txt  the requirements cited by the stories without any
//       pair "mismo" in the matching (those it has to decide); only to consult
//
// The first proposal (approved by the author on 16-11-2026) had the arbiter
// read entirely the requirements, and the criteria of every story without a
// pair "mismo". But in the two backlogs that did not fit, almost every story
// is "parecido" (B Kimi: 22 "mismo" of 245; SOL 6 and LUNA: 18 of 210), so it
// would have read almost everything again: about 120,000 to 140,000 tokens
// with Kimi. The author chose the same day to adjust it: both files only to
// consult, and the numbers of each criterion in the compact line, so the
// arbiter sees a conflict of numbers ("24" against "48") without reading the
// text (the embeddings do not see it either).
//
// The whole proposals and the catalog are still written to input/ with their
// names of always (PhaseTask.Proposal1File…), outside the reading plan: the
// validator, the citations by id (CriteriaReferences) and FakeProvider need
// them, and the model only reads the files its prompt names.
public static class BacklogArbiterFiles
{
    public const string Proposal1File = "input/propuesta_1.txt";
    public const string Proposal2File = "input/propuesta_2.txt";
    public const string CriteriaFile = "input/criterios.txt";
    public const string RequirementsFile = "input/requisitos.txt";

    // What requisitos.txt says when no story needs to check its requirements.
    public const string NoRequirements = "(Ninguna historia necesita comprobar sus requisitos: todas tienen un par «mismo».)\n";

    // The files of the arbiter, in the order of its reading plan, from the
    // proposals without prefixes, the validated catalog and the matching.
    public static IReadOnlyList<InputFile> Build(string proposal1, string proposal2, string catalog, string matching)
    {
        var criteria = new StringBuilder();
        var line = 0;
        var compact1 = Compact(proposal1, 1, criteria, ref line);
        var compact2 = Compact(proposal2, 2, criteria, ref line);
        return
        [
            new InputFile(Proposal1File, compact1, Required: true),
            new InputFile(Proposal2File, compact2, Required: true),
            new InputFile(CriteriaFile, criteria.ToString(), Required: false),
            new InputFile(RequirementsFile, Requirements(catalog, ToCheck(proposal1, proposal2, matching)), Required: false),
        ];
    }

    // The compact lines of a proposal, and the text of its criteria and tasks
    // added to criteria (line: the last line written there, shared by the two
    // proposals because they go in the same file).
    //   1:EP-01 «Reserva de salas» · Reservar salas y recibir la confirmación.
    //   1:HU-001 «Reservar una sala» · como empleado · quiero reservar una sala · para tenerla asegurada · requisitos: RF-001, RNF-001 · criterios: 1:CA-001 «Sala libre» (cifras: 1), 1:CA-002 «Sala ocupada» · tareas: 1:T-001, 1:T-002 · texto en criterios.txt, líneas 1–4
    public static string Compact(string proposal, int n, StringBuilder criteria, ref int line)
    {
        var text = new StringBuilder();
        foreach (var epic in Epics(proposal))
        {
            text.Append($"{n}:{Id(epic)} «{Str(epic, "titulo")}» · {Str(epic, "descripcion")}\n");
            foreach (var story in Items(epic, "historias"))
            {
                var first = line + 1;
                var names = new List<string>();
                var tasks = new List<string>();
                foreach (var c in Items(story, "criterios"))
                {
                    var numbers = SourcesFile.NumbersInOrder($"{Str(c, "dado")} {Str(c, "cuando")} {Str(c, "entonces")}");
                    names.Add($"{n}:{Id(c)} «{Str(c, "escenario")}»{(numbers.Count > 0 ? $" (cifras: {string.Join(", ", numbers)})" : "")}");
                    criteria.Append($"{n}:{Id(c)} «{Str(c, "escenario")}» · Dado {Str(c, "dado")} · Cuando {Str(c, "cuando")} · Entonces {Str(c, "entonces")}\n");
                    line++;
                }
                foreach (var t in Items(story, "tareas"))
                {
                    tasks.Add($"{n}:{Id(t)}");
                    criteria.Append($"{n}:{Id(t)} {Str(t, "titulo")}\n");
                    line++;
                }
                var where = line >= first ? $" · texto en criterios.txt, {(line == first ? $"línea {first}" : $"líneas {first}–{line}")}" : "";
                text.Append($"{n}:{Id(story)} «{Str(story, "titulo")}» · como {Str(story, "como")} · quiero {Str(story, "quiero")} · para {Str(story, "para")}")
                    .Append($" · requisitos: {Join(Strings(story, "requisitos"))}")
                    .Append($" · criterios: {Join(names)}")
                    .Append($" · tareas: {Join(tasks)}")
                    .Append(where).Append('\n');
            }
        }
        return text.ToString();
    }

    // The requirements the arbiter has to check: those cited by the stories
    // that have no pair "mismo" in the matching (with a pair "mismo", the two
    // stories cover the same; the platform checks the coverage, COB). Without
    // a matching, those of every story.
    public static IReadOnlySet<string> ToCheck(string proposal1, string proposal2, string matching)
    {
        var withSame = new HashSet<string>();
        foreach (var line in matching.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split(' ');
            if (parts.Length >= 3 && parts[2] == "mismo")
            {
                withSame.Add(parts[0]);
                withSame.Add(parts[1]);
            }
        }
        var requirements = new HashSet<string>();
        foreach (var (proposal, n) in new[] { (proposal1, 1), (proposal2, 2) })
            foreach (var story in Epics(proposal).SelectMany(e => Items(e, "historias")))
                if (!withSame.Contains($"{n}:{Id(story)}"))
                    requirements.UnionWith(Strings(story, "requisitos"));
        return requirements;
    }

    // One line per requirement of the set, in the order of the catalog: "RF-031 El sistema…".
    // A cited id that is not in the catalog is left out (the validator reports it).
    public static string Requirements(string catalog, IReadOnlySet<string> ids)
    {
        var text = new StringBuilder();
        foreach (var r in Items(Parse(catalog), "requisitos"))
            if (ids.Contains(Id(r)))
                text.Append($"{Id(r)} {Str(r, "texto")}\n");
        return text.Length > 0 ? text.ToString() : NoRequirements;
    }

    private static IEnumerable<JsonObject> Epics(string proposal) => Items(Parse(proposal), "epicas");

    private static JsonNode? Parse(string json)
    {
        try
        {
            return JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IEnumerable<JsonObject> Items(JsonNode? node, string list) => (node?[list] as JsonArray ?? []).OfType<JsonObject>();

    // The id without a prefix of a proposal, if it had one.
    private static string Id(JsonObject element) => ProposalIds.Bare(Str(element, "id"));

    private static string Str(JsonObject element, string key) =>
        element[key] is JsonValue v && v.TryGetValue<string>(out var s) ? s.ReplaceLineEndings(" ") : "";

    private static List<string> Strings(JsonObject element, string key) =>
        (element[key] as JsonArray ?? []).OfType<JsonValue>()
            .Select(v => v.TryGetValue<string>(out var s) ? s : null).OfType<string>().ToList();

    private static string Join(IReadOnlyList<string> items) => items.Count > 0 ? string.Join(", ", items) : "(ninguno)";
}
