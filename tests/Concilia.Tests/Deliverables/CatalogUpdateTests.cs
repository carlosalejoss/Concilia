using System.Text.Json.Nodes;
using Concilia.Core.Deliverables;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;

namespace Concilia.Tests.Deliverables;

// Step 113 (prompts v0.13): an update of the validated catalog with some
// iterations. Its inputs, its checks, the matching against the catalog, and
// how its changes are applied to the working copy and reviewed by the user.
public class CatalogUpdateTests
{
    private static readonly PhaseDefinition Proposal = PhaseDefinition.For(PhaseKind.CatalogUpdate, PhaseRole.Proposal);
    private static readonly PhaseDefinition Arbiter = PhaseDefinition.For(PhaseKind.CatalogUpdate, PhaseRole.Arbiter);

    private const string Catalog = """
        {"requisitos": [
          {"id": "RF-001", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema DEBERÁ permitir reservar una sala.", "fuentes": ["DOC-01#F001"], "preguntas": []},
          {"id": "RF-002", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema DEBERÁ permitir anular una reserva.", "fuentes": ["DOC-01#F002"], "preguntas": ["P-001"]}
        ]}
        """;

    // DOC-02 is the new documentation; P-004 was answered after validating the catalog.
    private static readonly PhaseContext Context = new()
    {
        FragmentIds = new HashSet<string> { "DOC-01#F001", "DOC-01#F002", "DOC-02#F001", "DOC-02#F002" },
        AnsweredQuestionIds = new HashSet<string> { "P-001", "P-004" },
        NewDocumentIds = new HashSet<string> { "DOC-02" },
        NewAnswerIds = new HashSet<string> { "P-004" },
        Catalog = Catalog,
    };

    private const string ValidProposal = """
        {"nuevos": [{"id": "N-001", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "CUANDO falten 24 horas para una reserva, el sistema DEBERÁ enviar un recordatorio por correo.",
                     "fuentes": ["DOC-02#F001"], "preguntas": [], "motivo": "DOC-02 pide recordatorios"}],
         "modificados": [{"id": "RF-002", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema DEBERÁ permitir a cualquier empleado anular una reserva.",
                          "fuentes": ["DOC-01#F002"], "preguntas": ["P-004"], "motivo": "La respuesta a P-004 amplía quién anula"}],
         "eliminados": []}
        """;

    [Fact]
    public void The_update_has_its_own_prompts_schemas_and_output_file()
    {
        Assert.Equal(("v0/catalogo_actualizacion.md", "v0/esquemas/catalogo_actualizacion.schema.json", "output/cambios.json"),
            (Proposal.PromptResource, Proposal.SchemaResource, Proposal.OutputFile));
        Assert.Equal("v0/arbitro_catalogo_actualizacion.md", Arbiter.PromptResource);
        Assert.Equal("catalog-update", PhasePlan.JobKind(PhaseKind.CatalogUpdate));
    }

    [Fact]
    public void A_and_B_read_as_in_a_centred_round_and_the_arbiter_gets_the_changes_grouped_by_requirement()
    {
        var input = new PhaseInput
        {
            SystemName = "", Language = "es", Documentation = "=== DOC-01 · a ===\n[DOC-01#F001 §1] a\n[DOC-01#F002 §1] b\n=== DOC-02 · c ===\n[DOC-02#F001 §1] c\n",
            NewDocumentation = "=== DOC-02 · c ===\n[DOC-02#F001 §1] c\n", Catalog = Catalog, RelatedRequirements = """{"requisitos": []}""",
        };
        Assert.Equal([(FocusedRound.NewDocumentationFile, true), (FocusedRound.RelatedRequirementsFile, true), (PhaseTask.CatalogFile, false), (DocumentationFile.Path, false)],
            PhaseTask.InputFilesOf(Proposal, input).Select(f => (f.Path, f.Required)));

        const string other = """{"nuevos": [], "modificados": [], "eliminados": [{"id": "RF-002", "fuentes": ["DOC-02#F002"], "preguntas": [], "motivo": "Ya no se anula"}]}""";
        var arbiterFiles = PhaseTask.InputFilesOf(Arbiter, input with { Proposal1 = ValidProposal, Proposal2 = other, Changes = "x" });
        Assert.Equal([PhaseTask.Proposal1File, PhaseTask.Proposal2File, CatalogUpdateRound.ChangesFile, SourcesFile.Path], arbiterFiles.Select(f => f.Path));
        Assert.Equal("=== DOC-01 · a ===\n[DOC-01#F002 §1] b\n=== DOC-02 · c ===\n[DOC-02#F001 §1] c\n", arbiterFiles[^1].Content); // the cited fragments
        Assert.Equal("""
            RF-002 «El sistema DEBERÁ permitir anular una reserva.»
              1: modificar → «El sistema DEBERÁ permitir a cualquier empleado anular una reserva.» · motivo: La respuesta a P-004 amplía quién anula · fuentes: DOC-01#F002 · preguntas: P-004
              2: eliminar · motivo: Ya no se anula · fuentes: DOC-02#F002

            """.ReplaceLineEndings("\n"), CatalogUpdateRound.Changes(ValidProposal, other, Catalog));
    }

    [Fact]
    public void The_answers_given_after_validating_the_catalog_are_marked_as_new()
    {
        var block = CatalogUpdateRound.AnswersBlock([new("P-001", "¿Quién anula?", "respondida", "Quien la hizo"), new("P-004", "¿Y ahora?", "respondida", "Cualquiera"), new("P-005", "x", "pendiente", null)],
            new HashSet<string> { "P-004" });
        Assert.Equal("<pregunta id=\"P-001\"><texto>¿Quién anula?</texto><respuesta>Quien la hizo</respuesta></pregunta>\n" +
                     "<pregunta id=\"P-004\" nueva=\"si\"><texto>¿Y ahora?</texto><respuesta>Cualquiera</respuesta></pregunta>", block);
    }

    [Fact]
    public void A_valid_proposal_passes_and_each_rule_has_its_code()
    {
        var check = PhaseValidator.Validate(Proposal, ValidProposal, Context);
        Assert.True(check.Valid, string.Join("\n", check.Errors));

        var root = JsonNode.Parse(ValidProposal)!;
        root["nuevos"]![0]!["fuentes"] = new JsonArray("DOC-01#F001");                       // nothing new
        root["modificados"]![0]!["tipo"] = "RNF";                                           // a modification changes the type
        root["eliminados"] = new JsonArray(
            new JsonObject { ["id"] = "RF-099", ["fuentes"] = new JsonArray("DOC-02#F001"), ["preguntas"] = new JsonArray(), ["motivo"] = "x" },
            new JsonObject { ["id"] = "RF-002", ["fuentes"] = new JsonArray("DOC-02#F001"), ["preguntas"] = new JsonArray(), ["motivo"] = "x" });
        var errors = PhaseValidator.Validate(Proposal, root.ToJsonString(), Context).Errors;

        Assert.Contains(errors, e => e.StartsWith("N-001 SIN-NUEVA"));
        Assert.Contains(errors, e => e.StartsWith("RF-002 TIPO"));
        Assert.Contains(errors, e => e.StartsWith("RF-099 REF-REQ"));
        Assert.Contains(errors, e => e.StartsWith("RF-002 CAMBIO-DUP"));
    }

    private const string Other = """
        {"nuevos": [{"id": "N-001", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema DEBERÁ enviar recordatorios.", "fuentes": ["DOC-02#F001"], "preguntas": [], "motivo": "x"}],
         "modificados": [], "eliminados": [{"id": "RF-002", "fuentes": ["DOC-02#F002"], "preguntas": [], "motivo": "Ya no se anula"}]}
        """;

    private static string ArbiterOutput(string changes) => $$"""
        {"nuevos": [{"id": "N-001", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "CUANDO falten 24 horas, el sistema DEBERÁ enviar un recordatorio.",
                     "fuentes": ["DOC-02#F001"], "preguntas": [], "motivo": "x", "de": {"propuesta_1": ["N-001"], "propuesta_2": ["N-001"]}, "discrepancia": false, "explicacion": "x"}],
         "descartados": [],
         "cambios": [{{changes}}]}
        """;

    [Fact]
    public void The_arbiter_decides_each_changed_requirement_once_and_a_modification_against_a_deletion_is_a_discrepancy()
    {
        var context = Context with { Proposal1 = ValidProposal, Proposal2 = Other };
        var output = ArbiterOutput("""
            {"id": "RF-002", "accion": "modificar", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema DEBERÁ permitir a cualquier empleado anular una reserva.",
             "fuentes": ["DOC-01#F002"], "preguntas": ["P-004"], "motivo": "x", "de": [1], "discrepancia": false, "explicacion": "x"}
            """);
        var check = PhaseValidator.Validate(Arbiter, output, context);
        Assert.True(check.Valid, string.Join("\n", check.Errors));

        // A modifies RF-002 and B deletes it: the platform marks the discrepancy.
        var fixedOutput = PlatformFixes.Apply(Arbiter, output, context);
        Assert.Contains(fixedOutput.Warnings, w => w.StartsWith("RF-002 CAMBIO-DISC"));
        Assert.True(JsonNode.Parse(fixedOutput.Output)!["cambios"]![0]!["discrepancia"]!.GetValue<bool>());

        var errors = PhaseValidator.Validate(Arbiter, ArbiterOutput("""
            {"id": "RF-002", "accion": "eliminar", "fuentes": [], "preguntas": [], "motivo": "x", "de": [1], "discrepancia": false, "explicacion": "x"}
            """), context).Errors;
        Assert.Contains(errors, e => e.StartsWith("RF-002 ARB-CAMBIO: la propuesta 1 no propone `eliminar`"));
        Assert.Contains(PhaseValidator.Validate(Arbiter, ArbiterOutput(""), context).Errors, e => e.StartsWith("RF-002 ARB-CAMBIO: falta la decisión"));
    }

    [Fact]
    public void A_new_requirement_like_one_of_the_catalog_gets_a_line_existente()
    {
        var matching = new ArbiterMatching(new FakeTextEmbedder(), new MatchThresholds(Same: 0.9, Similar: 0.7));
        var existing = matching.AgainstExisting(PhaseKind.CatalogUpdate,
            """{"nuevos": [{"id": "N-001", "texto": "El sistema DEBERÁ permitir reservar una sala."}]}""", """{"nuevos": []}""",
            [("RF-001", "El sistema DEBERÁ permitir reservar una sala."), ("RF-002", "Algo distinto del todo.")]);

        Assert.Equal("1:N-001 existente:RF-001 mismo 1.00\n", MatchingFile.WithPrevious("", existing, "existente"));
    }

    private static readonly PhasePlan Plan = new(PhaseKind.CatalogUpdate, new PhaseInput { SystemName = "", Language = "" }, [], [], ["B", "A"])
    {
        Focus = new FocusPlan([2], ["DOC-02"], []),
    };

    // The working copy: the validated catalog with the platform's fields.
    private static readonly string WorkingCopy = CatalogDraft.FromResult(new PhaseJobResult("A", false, Catalog, ""), Plan with { Kind = PhaseKind.Catalog });

    private static string Applied(string changes) =>
        CatalogUpdate.Apply(WorkingCopy, new PhaseJobResult("ARB", true, ArbiterOutput(changes), ""), Plan, Guid.Empty, ["RF-001", "RF-002", "RF-003"], DateTimeOffset.UnixEpoch);

    private static JsonObject Requirement(string json, string id) =>
        JsonNode.Parse(json)!["requisitos"]!.AsArray().OfType<JsonObject>().Single(r => r["id"]!.GetValue<string>() == id);

    [Fact]
    public void A_new_requirement_takes_the_next_id_and_a_modification_or_deletion_is_a_pending_proposal()
    {
        var content = Applied("""
            {"id": "RF-002", "accion": "modificar", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "Nuevo texto.",
             "fuentes": ["DOC-02#F001"], "preguntas": [], "motivo": "Lo pide DOC-02", "de": [1, 2], "discrepancia": false, "explicacion": "x"}
            """);

        var added = Requirement(content, "RF-004"); // RF-003 was given before in the history
        Assert.Equal(("ambas", "creado"), (added["origen"]!.GetValue<string>(), added["historial"]![0]!["accion"]!.GetValue<string>()));
        Assert.Equal([2], added["historial"]![0]!["iteraciones"]!.AsArray().Select(n => n!.GetValue<int>()));
        var modified = Requirement(content, "RF-002");
        Assert.Equal("Nuevo texto.", modified["texto"]!.GetValue<string>());
        Assert.Equal(("modificar", "El sistema DEBERÁ permitir anular una reserva."),
            (modified["cambio"]!["accion"]!.GetValue<string>(), modified["cambio"]!["antes"]!["texto"]!.GetValue<string>()));
        // Not validated until it is reviewed.
        Assert.Contains(CatalogDraft.Check(content, new PhaseContext()).Errors, e => e.StartsWith("RF-002 CAMBIO"));
    }

    [Fact]
    public void Accepting_or_rejecting_a_change_keeps_or_gives_back_the_requirement_with_its_history()
    {
        const string modify = """
            {"id": "RF-002", "accion": "modificar", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "Nuevo texto.",
             "fuentes": ["DOC-02#F001"], "preguntas": [], "motivo": "Lo pide DOC-02", "de": [2], "discrepancia": false, "explicacion": "x"}
            """;
        const string delete = """
            {"id": "RF-002", "accion": "eliminar", "fuentes": ["DOC-02#F002"], "preguntas": [], "motivo": "Ya no se anula", "de": [1], "discrepancia": false, "explicacion": "x"}
            """;
        Deliverable Draft(string changes) => new() { Kind = DeliverableKind.Catalog, Content = Applied(changes) };

        var accepted = Draft(modify);
        Assert.True(CatalogUpdate.Accept(accepted, "RF-002", DateTimeOffset.UnixEpoch).Done);
        var kept = Requirement(accepted.Content, "RF-002");
        Assert.Null(kept["cambio"]);
        Assert.Equal(("Nuevo texto.", "modificado", "A"), (kept["texto"]!.GetValue<string>(), kept["historial"]![0]!["accion"]!.GetValue<string>(), kept["historial"]![0]!["origen"]!.GetValue<string>()));

        var rejected = Draft(modify);
        Assert.True(CatalogUpdate.Reject(rejected, "RF-002", DateTimeOffset.UnixEpoch).Done);
        Assert.Equal("El sistema DEBERÁ permitir anular una reserva.", Requirement(rejected.Content, "RF-002")["texto"]!.GetValue<string>());
        Assert.Null(Requirement(rejected.Content, "RF-002")["historial"]);

        var deleted = Draft(delete);
        Assert.True(CatalogUpdate.Accept(deleted, "RF-002", DateTimeOffset.UnixEpoch).Done);
        var root = JsonNode.Parse(deleted.Content)!;
        Assert.DoesNotContain(root["requisitos"]!.AsArray(), r => r!["id"]!.GetValue<string>() == "RF-002");
        Assert.Equal("eliminado", root["eliminados"]![0]!["historial"]![0]!["accion"]!.GetValue<string>());
        // Saving from the web, which does not send `eliminados` back, keeps them.
        var saved = CatalogDraft.Normalize(new JsonObject { ["requisitos"] = root["requisitos"]!.DeepClone() }.ToJsonString(), deleted.Content, []);
        Assert.Equal("RF-002", JsonNode.Parse(saved)!["eliminados"]![0]!["id"]!.GetValue<string>());

        var refused = Draft(delete);
        Assert.True(CatalogUpdate.Reject(refused, "RF-002", DateTimeOffset.UnixEpoch).Done);
        Assert.Null(Requirement(refused.Content, "RF-002")["cambio"]);
        Assert.False(CatalogUpdate.Accept(refused, "RF-002", DateTimeOffset.UnixEpoch).Done); // nothing left to review
    }
}
