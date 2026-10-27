using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Concilia.Core.Phases;

// What FakeProvider writes in a run of a v0 phase (it knows it is one by
// input/fase.json): an output that passes PhaseValidator, built from the run's
// input/ files, so the whole flow can be tried without models.
// - A and B: questions about the first fragments, one requirement per fragment
//   of the first ones, or one story per functional requirement of the catalog.
// - Arbiter: proposal 1 element by element, each paired with the element of
//   proposal 2 in the same position; what proposal 2 has left over is discarded.
public static partial class FakePhaseOutput
{
    // The title of the repair annex (prompts/v0/anexo_reparacion.md).
    public const string RepairMarker = "# REPARACIÓN (intento";

    private const string Note = "Simulado por FakeProvider; ningún modelo ha intervenido.";

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static bool IsPhaseRun(string workDirectory) => File.Exists(Path.Combine(workDirectory, PhaseTask.PhaseFile));

    // broken: the first attempt with simulateRepair, cut in half like a Write
    // that reached the output cap, so the repair can be seen working.
    // prompt: a round centred on some iterations (step 112) takes from it the
    // pending questions it may revise.
    public static string For(string workDirectory, bool broken, string prompt = "")
    {
        var phase = JsonNode.Parse(Read(workDirectory, PhaseTask.PhaseFile))!;
        var kind = Enum.Parse<PhaseKind>(phase["kind"]!.GetValue<string>());
        var role = Enum.Parse<PhaseRole>(phase["role"]!.GetValue<string>());
        var focused = phase["focused"] is JsonValue f && f.GetValue<bool>();
        var output = (role, focused) switch
        {
            (PhaseRole.Proposal, _) when kind == PhaseKind.CatalogUpdate => UpdateProposal(workDirectory),
            (_, _) when kind == PhaseKind.CatalogUpdate => UpdateArbiter(workDirectory),
            (PhaseRole.Proposal, true) => FocusedProposal(workDirectory, prompt),
            (PhaseRole.Proposal, false) => Proposal(kind, workDirectory),
            (_, true) => FocusedArbiter(workDirectory),
            _ => Arbiter(kind, workDirectory),
        };
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
            PhaseKind.CatalogUpdate => ("nuevos", "descartados"),
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

    // A centred round: a question about the first fragment of the new
    // documentation and, if there is a pending question, the proposal that the
    // new documentation solves it.
    private static JsonObject FocusedProposal(string workDirectory, string prompt)
    {
        var fragment = Read(workDirectory, FocusedRound.NewDocumentationFile).Split('\n')
            .Select(line => NewFragment().Match(line)).First(m => m.Success).Groups[1].Value;
        var pending = PendingQuestion().Match(prompt);
        return new JsonObject
        {
            ["preguntas"] = new JsonArray(new JsonObject
            {
                ["id"] = "P-001",
                ["pregunta"] = $"¿Puedes concretar lo que dice el fragmento nuevo {fragment}?",
                ["motivo"] = Note,
                ["fuentes"] = new JsonArray(fragment),
            }),
            ["revisiones"] = pending.Success
                ? new JsonArray(new JsonObject
                {
                    ["pregunta"] = pending.Groups[1].Value,
                    ["accion"] = "resuelta",
                    ["motivo"] = Note,
                    ["fuentes"] = new JsonArray(fragment),
                })
                : new JsonArray(),
        };
    }

    // The arbiter of a centred round: the new questions as in a round of
    // always, and for each revised question the revision of proposal 1 (or of
    // proposal 2 if 1 has none), with both proposals in `de` if they agree.
    private static JsonObject FocusedArbiter(string workDirectory)
    {
        var output = Arbiter(PhaseKind.Questions, workDirectory);
        var p1 = Read(workDirectory, PhaseTask.Proposal1File);
        var p2 = Read(workDirectory, PhaseTask.Proposal2File);
        var byProposal = new[] { FocusedRound.RevisionsOf(p1), FocusedRound.RevisionsOf(p2) };
        output["revisiones"] = Array(FocusedRound.Targets(p1, p2).Select(target =>
        {
            var mine = byProposal[0].FirstOrDefault(r => r["pregunta"]!.GetValue<string>() == target)
                       ?? byProposal[1].First(r => r["pregunta"]!.GetValue<string>() == target);
            var de = Enumerable.Range(1, 2).Where(n => byProposal[n - 1].Any(r =>
                r["pregunta"]!.GetValue<string>() == target && r["accion"]!.GetValue<string>() == mine["accion"]!.GetValue<string>()));
            var decision = mine.DeepClone().AsObject();
            decision["de"] = new JsonArray(de.Select(n => (JsonNode)n).ToArray());
            decision["discrepancia"] = false;
            decision["explicacion"] = Note;
            return decision;
        }));
        return output;
    }

    // An update of the catalog (step 113): a new requirement about the first
    // fragment of the new documentation and, if there is a related
    // requirement, its modification citing that fragment too.
    private static JsonObject UpdateProposal(string workDirectory)
    {
        var fragment = Read(workDirectory, FocusedRound.NewDocumentationFile).Split('\n')
            .Select(line => NewFragment().Match(line)).First(m => m.Success).Groups[1].Value;
        var related = JsonNode.Parse(Read(workDirectory, FocusedRound.RelatedRequirementsFile))!["requisitos"]!.AsArray().OfType<JsonObject>().FirstOrDefault();
        var modified = new JsonArray();
        if (related is not null)
        {
            var copy = related.DeepClone().AsObject();
            copy["texto"] = $"{related["texto"]!.GetValue<string>()} (actualizado)";
            copy["fuentes"]!.AsArray().Add(fragment);
            copy["motivo"] = Note;
            modified.Add(copy);
        }
        return new JsonObject
        {
            ["nuevos"] = new JsonArray(new JsonObject
            {
                ["id"] = "N-001",
                ["tipo"] = "RF",
                ["notacion"] = "EARS",
                ["obligatoriedad"] = "shall",
                ["texto"] = $"El sistema DEBERÁ hacer lo que describe el fragmento nuevo {fragment} (simulado).",
                ["fuentes"] = new JsonArray(fragment),
                ["preguntas"] = new JsonArray(),
                ["motivo"] = Note,
            }),
            ["modificados"] = modified,
            ["eliminados"] = new JsonArray(),
        };
    }

    // Its arbiter: the new requirements as when joining a catalog, and for each
    // changed requirement the change of proposal 1 (or 2), with both in `de` if they agree.
    private static JsonObject UpdateArbiter(string workDirectory)
    {
        var output = Arbiter(PhaseKind.CatalogUpdate, workDirectory);
        var p1 = Read(workDirectory, PhaseTask.Proposal1File);
        var p2 = Read(workDirectory, PhaseTask.Proposal2File);
        var byProposal = new[] { CatalogUpdateRound.ChangesOf(p1), CatalogUpdateRound.ChangesOf(p2) };
        output["cambios"] = Array(CatalogUpdateRound.Targets(p1, p2).Select(id =>
        {
            var mine = byProposal[0].Concat(byProposal[1]).First(c => c.Id == id);
            var de = Enumerable.Range(1, 2).Where(n => byProposal[n - 1].Any(c => c.Id == id && c.Action == mine.Action));
            var decision = mine.Change.DeepClone().AsObject();
            decision["accion"] = mine.Action;
            decision["de"] = new JsonArray(de.Select(n => (JsonNode)n).ToArray());
            decision["discrepancia"] = false;
            decision["explicacion"] = Note;
            return decision;
        }));
        return output;
    }

    [GeneratedRegex(@"^\[(DOC-[0-9]{2,}#F[0-9]{3,}) ")]
    private static partial Regex NewFragment();

    [GeneratedRegex("<pregunta id=\"(P-[0-9]{3,})\" estado=\"pendiente\">")]
    private static partial Regex PendingQuestion();

    private static JsonArray Array(IEnumerable<JsonObject> items) => new(items.Select(x => (JsonNode)x).ToArray());

    private static string Read(string workDirectory, string relativePath) =>
        File.ReadAllText(Path.Combine(workDirectory, relativePath));
}
