using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Concilia.Core.Phases;

// What FakeProvider writes in a run of a v0 phase (it knows it is one by
// input/fase.json): an output that passes PhaseValidator, built from the run's
// input/ files, so the whole flow can be tried without models.
// - A and B: questions about the first fragments, one requirement per fragment
//   of the first ones, or one story per functional requirement of the catalog.
// - Arbiter: proposal 1 element by element, each paired with the element of
//   proposal 2 in the same position; what proposal 2 has left over is discarded.
public static class FakePhaseOutput
{
    // The repair annex wraps the previous output in this tag.
    public const string RepairMarker = "<salida_previa>";

    private const string Note = "Simulado por FakeProvider; ningún modelo ha intervenido.";

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static bool IsPhaseRun(string workDirectory) => File.Exists(Path.Combine(workDirectory, PhaseTask.PhaseFile));

    // broken: the first attempt with simulateRepair, cut in half like a Write
    // that reached the output cap, so the repair can be seen working.
    public static string For(string workDirectory, bool broken)
    {
        var phase = JsonNode.Parse(Read(workDirectory, PhaseTask.PhaseFile))!;
        var kind = Enum.Parse<PhaseKind>(phase["kind"]!.GetValue<string>());
        var role = Enum.Parse<PhaseRole>(phase["role"]!.GetValue<string>());
        var output = role == PhaseRole.Proposal ? Proposal(kind, workDirectory) : Arbiter(kind, workDirectory);
        var text = output.ToJsonString(Indented);
        return broken ? text[..(text.Length / 2)] : text;
    }

    private static JsonObject Proposal(PhaseKind kind, string workDirectory)
    {
        var fragments = JsonNode.Parse(Read(workDirectory, PhaseTask.FragmentsFile))!["fragments"]!.AsArray()
            .Where(f => f!["kind"]!.GetValue<string>() == "document")
            .Select(f => f!["id"]!.GetValue<string>())
            .ToList();
        switch (kind)
        {
            case PhaseKind.Suggestions:
                var pending = JsonSerializer.Deserialize<List<string>>(Read(workDirectory, PhaseTask.PendingFile))!;
                return new JsonObject
                {
                    ["sugerencias"] = Array(pending.Select(p => new JsonObject
                    {
                        ["pregunta"] = p,
                        ["respuesta"] = $"Respuesta simulada para {p}: revísala antes de guardarla.",
                        ["base"] = "suposicion",
                        ["fuentes"] = new JsonArray(),
                        ["explicacion"] = Note,
                    })),
                };
            case PhaseKind.Questions:
                return new JsonObject
                {
                    ["preguntas"] = Array(fragments.Take(2).Select((f, i) => new JsonObject
                    {
                        ["id"] = $"P-{i + 1:000}",
                        ["pregunta"] = $"¿Puedes concretar lo que dice el fragmento {f}?",
                        ["motivo"] = Note,
                        ["fuentes"] = new JsonArray(f),
                    })),
                };
            case PhaseKind.Catalog:
                return new JsonObject
                {
                    ["requisitos"] = Array(fragments.Take(3).Select((f, i) => new JsonObject
                    {
                        ["id"] = $"RF-{i + 1:000}",
                        ["tipo"] = "RF",
                        ["notacion"] = "EARS",
                        ["obligatoriedad"] = "shall",
                        ["texto"] = $"El sistema DEBERÁ hacer lo que describe el fragmento {f} (simulado).",
                        ["fuentes"] = new JsonArray(f),
                        ["preguntas"] = new JsonArray(),
                    })),
                };
            default:
                var requirements = JsonNode.Parse(Read(workDirectory, PhaseTask.CatalogFile))!["requisitos"]!.AsArray()
                    .Select(r => (Id: r!["id"]!.GetValue<string>(), Type: r["tipo"]!.GetValue<string>()))
                    .ToList();
                var functional = requirements.Where(r => r.Type is "RF" or "RI").ToList();
                var covered = functional.Count > 0 ? functional : requirements;
                return new JsonObject
                {
                    ["epicas"] = new JsonArray(new JsonObject
                    {
                        ["id"] = "EP-01",
                        ["titulo"] = "Épica simulada",
                        ["descripcion"] = Note,
                        ["historias"] = Array(covered.Select((r, i) => new JsonObject
                        {
                            ["id"] = $"HU-{i + 1:000}",
                            ["titulo"] = $"Historia de {r.Id}",
                            ["como"] = "usuario",
                            ["quiero"] = $"lo que pide {r.Id}",
                            ["para"] = "probar el flujo sin modelos",
                            ["requisitos"] = new JsonArray(r.Id),
                            ["criterios"] = new JsonArray(new JsonObject
                            {
                                ["id"] = $"CA-{i + 1:000}",
                                ["escenario"] = $"{r.Id} se cumple",
                                ["dado"] = "el sistema en marcha",
                                ["cuando"] = "el usuario lo usa",
                                ["entonces"] = $"se cumple {r.Id}",
                            }),
                            ["tareas"] = new JsonArray(new JsonObject { ["id"] = $"T-{i + 1:000}", ["titulo"] = $"Implementar {r.Id}" }),
                        })),
                    }),
                };
        }
    }

    private static JsonObject Arbiter(PhaseKind kind, string workDirectory)
    {
        var p1 = JsonNode.Parse(Read(workDirectory, PhaseTask.Proposal1File))!;
        var p2 = JsonNode.Parse(Read(workDirectory, PhaseTask.Proposal2File))!;
        var (list, discards) = kind switch
        {
            PhaseKind.Questions => ("preguntas", "descartadas"),
            PhaseKind.Catalog => ("requisitos", "descartados"),
            _ => ("historias", "descartadas"),
        };

        // The elements the arbiter compares (the stories of every epic in the backlog).
        List<JsonObject> Elements(JsonNode proposal) => kind == PhaseKind.Backlog
            ? proposal["epicas"]!.AsArray().SelectMany(e => e!["historias"]!.AsArray()).Select(h => h!.AsObject()).ToList()
            : proposal[list]!.AsArray().Select(x => x!.AsObject()).ToList();

        var others = Elements(p2);
        var position = 0;
        JsonObject Joined(JsonObject element)
        {
            var copy = element.DeepClone().AsObject();
            copy["id"] = ProposalIds.Bare(element["id"]!.GetValue<string>()); // the joined element has its own id, without prefix
            var pair = position < others.Count ? new JsonArray(others[position]["id"]!.GetValue<string>()) : new JsonArray();
            position++;
            copy["de"] = new JsonObject { ["propuesta_1"] = new JsonArray(element["id"]!.GetValue<string>()), ["propuesta_2"] = pair };
            if (kind != PhaseKind.Questions)
                copy["discrepancia"] = false;
            copy["explicacion"] = Note;
            return copy;
        }

        var output = new JsonObject();
        if (kind == PhaseKind.Backlog)
        {
            output["epicas"] = Array(p1["epicas"]!.AsArray().Select(e =>
            {
                var epic = e!.DeepClone().AsObject();
                epic["historias"] = Array(e["historias"]!.AsArray().Select(h => Joined(h!.AsObject())));
                return epic;
            }));
        }
        else
        {
            output[list] = Array(Elements(p1).Select(Joined));
        }
        output[discards] = Array(others.Skip(position).Select(x => new JsonObject
        {
            ["propuesta"] = 2,
            ["id"] = x["id"]!.GetValue<string>(),
            ["motivo"] = Note,
        }));
        return output;
    }

    private static JsonArray Array(IEnumerable<JsonObject> items) => new(items.Select(x => (JsonNode)x).ToArray());

    private static string Read(string workDirectory, string relativePath) =>
        File.ReadAllText(Path.Combine(workDirectory, relativePath));
}
