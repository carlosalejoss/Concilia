using System.Text.Json.Nodes;
using Concilia.Core.Deliverables;
using Concilia.Core.Phases;

namespace Concilia.Tests.Deliverables;

// Step 114 (prompts v0.13): an update of the validated backlog. The impact the
// platform computes without AI, the checks of the changes, and how they are
// applied to the working copy and reviewed by the user.
public class BacklogUpdateTests
{
    private static readonly PhaseDefinition Proposal = PhaseDefinition.For(PhaseKind.BacklogUpdate, PhaseRole.Proposal);
    private static readonly PhaseDefinition Arbiter = PhaseDefinition.For(PhaseKind.BacklogUpdate, PhaseRole.Arbiter);

    private const string Before = """
        {"requisitos": [
          {"id": "RF-001", "tipo": "RF", "texto": "Reservar una sala."},
          {"id": "RF-002", "tipo": "RF", "texto": "Anular una reserva."},
          {"id": "RF-003", "tipo": "RF", "texto": "Ver las reservas."}
        ]}
        """;

    // RF-001 modified, RF-003 deleted, RF-004 and RNF-001 new.
    private const string After = """
        {"requisitos": [
          {"id": "RF-001", "tipo": "RF", "texto": "Reservar una sala para varios días."},
          {"id": "RF-002", "tipo": "RF", "texto": "Anular una reserva."},
          {"id": "RF-004", "tipo": "RF", "texto": "Enviar un recordatorio."},
          {"id": "RNF-001", "tipo": "RNF", "texto": "Responder en menos de 2 s."}
        ]}
        """;

    private const string Backlog = """
        {"epicas": [{"id": "EP-01", "titulo": "Reservas", "descripcion": "", "historias": [
          {"id": "HU-001", "titulo": "Reservar", "como": "empleado", "quiero": "reservar", "para": "tener sala", "requisitos": ["RF-001"],
           "criterios": [{"id": "CA-001", "escenario": "Reserva", "dado": "x", "cuando": "y", "entonces": "z"}], "tareas": [{"id": "T-001", "titulo": "Formulario"}]},
          {"id": "HU-002", "titulo": "Anular", "como": "empleado", "quiero": "anular", "para": "liberar", "requisitos": ["RF-002"],
           "criterios": [{"id": "CA-002", "escenario": "Anula", "dado": "x", "cuando": "y", "entonces": "z"}], "tareas": []},
          {"id": "HU-003", "titulo": "Ver", "como": "empleado", "quiero": "ver", "para": "saber", "requisitos": ["RF-003"],
           "criterios": [{"id": "CA-003", "escenario": "Ve", "dado": "x", "cuando": "y", "entonces": "z"}], "tareas": []}
        ]}]}
        """;

    [Fact]
    public void The_impact_is_what_changed_in_the_catalog_and_the_stories_that_cite_it()
    {
        var impact = BacklogImpact.Compute(Before, After, Backlog);

        Assert.Equal(["RF-001"], impact.Modified.Select(m => m.Id));
        Assert.Equal(["RF-003"], impact.Deleted.Select(d => d.Id));
        Assert.Equal(["RF-004", "RNF-001"], impact.New.Select(n => n.Id));
        Assert.Equal(["HU-001", "HU-003"], impact.Affected.Select(a => a.Id));
        Assert.Contains("- RF-001 antes: «Reservar una sala.» · ahora: «Reservar una sala para varios días.»", impact.Text());
        Assert.Contains("- HU-003 (EP-01 «Reservas»): cita RF-003 (eliminado)", impact.Text());
        var affected = JsonNode.Parse(impact.AffectedJson(Backlog))!["historias"]!.AsArray();
        Assert.Equal(("HU-001", "EP-01"), (affected[0]!["id"]!.GetValue<string>(), affected[0]!["epica"]!["id"]!.GetValue<string>()));
        Assert.True(BacklogImpact.Compute(After, After, Backlog).IsEmpty);
    }

    private static readonly PhaseContext Context = new() { Catalog = After, Backlog = Backlog };

    private const string ValidProposal = """
        {"nuevas": [{"id": "N-001", "epica": "EP-01", "titulo": "Recordar", "como": "empleado", "quiero": "un recordatorio", "para": "no olvidar",
                     "requisitos": ["RF-004"], "criterios": [{"escenario": "Avisa", "dado": "x", "cuando": "y", "entonces": "z"}], "tareas": [], "motivo": "RF-004 es nuevo"}],
         "modificadas": [{"id": "HU-001", "titulo": "Reservar", "como": "empleado", "quiero": "reservar varios días", "para": "tener sala", "requisitos": ["RF-001"],
                          "criterios": ["CA-001", {"escenario": "Varios días", "dado": "x", "cuando": "y", "entonces": "z"}], "tareas": ["T-001"], "motivo": "RF-001 cambia"}],
         "eliminadas": [{"id": "HU-003", "motivo": "RF-003 se elimina"}]}
        """;

    [Fact]
    public void A_valid_proposal_passes_and_each_rule_has_its_code()
    {
        var check = PhaseValidator.Validate(Proposal, ValidProposal, Context);
        Assert.True(check.Valid, string.Join("\n", check.Errors));

        var root = JsonNode.Parse(ValidProposal)!;
        root["nuevas"]![0]!["epica"] = "EP-09";
        root["nuevas"]![0]!["requisitos"] = new JsonArray("RF-003");     // deleted from the catalog
        root["modificadas"]![0]!["criterios"]![0] = "CA-002";              // a criterion of another story
        root["eliminadas"]!.AsArray().Add(new JsonObject { ["id"] = "HU-001", ["motivo"] = "x" });
        var errors = PhaseValidator.Validate(Proposal, root.ToJsonString(), Context).Errors;

        Assert.Contains(errors, e => e.StartsWith("N-001 REF-EP"));
        Assert.Contains(errors, e => e.StartsWith("N-001 REF-REQ"));
        Assert.Contains(errors, e => e.StartsWith("HU-001 CITA: CA-002"));
        Assert.Contains(errors, e => e.StartsWith("HU-001 CAMBIO-DUP"));
    }

    private static string ArbiterOutput(string changes) => $$"""
        {"nuevas": [{"id": "N-001", "epica": {"titulo": "Avisos", "descripcion": "Recordatorios"}, "titulo": "Recordar", "como": "empleado", "quiero": "un recordatorio", "para": "no olvidar",
                     "requisitos": ["RF-004"], "criterios": [{"escenario": "Avisa", "dado": "x", "cuando": "y", "entonces": "z"}], "tareas": [{"titulo": "Enviar correo"}],
                     "motivo": "x", "de": {"propuesta_1": ["N-001"], "propuesta_2": []}, "discrepancia": false, "explicacion": "x"}],
         "descartadas": [],
         "cambios": [{{changes}}]}
        """;

    private const string Modify = """
        {"id": "HU-001", "accion": "modificar", "titulo": "Reservar", "como": "empleado", "quiero": "reservar varios días", "para": "tener sala", "requisitos": ["RF-001"],
         "criterios": ["CA-001", {"escenario": "Varios días", "dado": "x", "cuando": "y", "entonces": "z"}], "tareas": ["T-001"], "motivo": "RF-001 cambia", "de": [1], "discrepancia": false, "explicacion": "x"}
        """;

    [Fact]
    public void The_arbiter_decides_each_changed_story_once()
    {
        var context = Context with { Proposal1 = ValidProposal, Proposal2 = """{"nuevas": [], "modificadas": [], "eliminadas": []}""" };
        var output = ArbiterOutput(Modify + """, {"id": "HU-003", "accion": "eliminar", "motivo": "x", "de": [1], "discrepancia": false, "explicacion": "x"}""");
        var check = PhaseValidator.Validate(Arbiter, output, context);
        Assert.True(check.Valid, string.Join("\n", check.Errors));
        Assert.Contains(PhaseValidator.Validate(Arbiter, ArbiterOutput(Modify), context).Errors, e => e.StartsWith("HU-003 ARB-CAMBIO: falta la decisión"));
    }

    private static readonly PhasePlan Plan = new(PhaseKind.BacklogUpdate, new PhaseInput { SystemName = "", Language = "" }, [], [], ["A", "B"])
    {
        Focus = new FocusPlan([2], [], []),
    };

    private static string Applied(string changes) =>
        BacklogUpdate.Apply(Backlog, new PhaseJobResult("ARB", true, ArbiterOutput(changes), ""), Plan, Guid.Empty, BacklogDraft.Ids(Backlog), DateTimeOffset.UnixEpoch);

    private static JsonObject Story(string json, string id) =>
        JsonNode.Parse(json)!["epicas"]!.AsArray().SelectMany(e => e!["historias"]!.AsArray()).OfType<JsonObject>().Single(h => h["id"]!.GetValue<string>() == id);

    [Fact]
    public void A_new_story_goes_to_a_new_epic_with_new_ids_and_a_modification_keeps_the_cited_criteria()
    {
        var content = Applied(Modify);

        var epics = JsonNode.Parse(content)!["epicas"]!.AsArray();
        Assert.Equal(["EP-01", "EP-02"], epics.Select(e => e!["id"]!.GetValue<string>()));
        var added = Story(content, "HU-004");
        Assert.Equal(("CA-004", "T-002", "creada"), (added["criterios"]![0]!["id"]!.GetValue<string>(), added["tareas"]![0]!["id"]!.GetValue<string>(), added["historial"]![0]!["accion"]!.GetValue<string>()));

        var modified = Story(content, "HU-001");
        Assert.Equal(["CA-001", "CA-005"], modified["criterios"]!.AsArray().Select(c => c!["id"]!.GetValue<string>()));
        Assert.Equal("Reserva", modified["criterios"]![0]!["escenario"]!.GetValue<string>()); // the cited one, as it was
        Assert.Equal("reservar", modified["cambio"]!["antes"]!["quiero"]!.GetValue<string>());
        Assert.Contains(BacklogDraft.Check(content, new PhaseContext { Catalog = After }).Errors, e => e.StartsWith("HU-001 CAMBIO"));
    }

    [Fact]
    public void Accepting_a_deletion_removes_the_story_and_rejecting_a_modification_gives_it_back()
    {
        var deleted = new Deliverable { Kind = DeliverableKind.Backlog, Content = Applied("""{"id": "HU-003", "accion": "eliminar", "motivo": "RF-003 se elimina", "de": [1], "discrepancia": false, "explicacion": "x"}""") };
        Assert.True(BacklogUpdate.Accept(deleted, "HU-003", DateTimeOffset.UnixEpoch).Done);
        var root = JsonNode.Parse(deleted.Content)!;
        Assert.DoesNotContain(root["epicas"]![0]!["historias"]!.AsArray(), h => h!["id"]!.GetValue<string>() == "HU-003");
        Assert.Equal(("HU-003", "eliminada", "EP-01"), (root["eliminadas"]![0]!["id"]!.GetValue<string>(),
            root["eliminadas"]![0]!["historial"]![0]!["accion"]!.GetValue<string>(), root["eliminadas"]![0]!["epica"]!.GetValue<string>()));
        var saved = BacklogDraft.Normalize(new JsonObject { ["epicas"] = root["epicas"]!.DeepClone() }.ToJsonString(), deleted.Content, []);
        Assert.NotNull(JsonNode.Parse(saved)!["eliminadas"]);

        var rejected = new Deliverable { Kind = DeliverableKind.Backlog, Content = Applied(Modify) };
        Assert.True(BacklogUpdate.Reject(rejected, "HU-001", DateTimeOffset.UnixEpoch).Done);
        var back = Story(rejected.Content, "HU-001");
        Assert.Equal(("reservar", 1), (back["quiero"]!.GetValue<string>(), back["criterios"]!.AsArray().Count));
        Assert.Null(back["cambio"]);
    }
}
