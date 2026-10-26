using System.Text;
using System.Text.Json.Nodes;
using Concilia.Core.Catalog;
using Concilia.Core.Deliverables;
using Concilia.Core.Deliverables.Export;

namespace Concilia.Tests.Deliverables;

// Step 110: stable ids in the working copy of the catalog and the backlog.
// What is kept keeps its id, what is new takes the next number of its prefix,
// a number given before is never given again, and the references, the
// exports and the traceability work with gaps.
public class StableIdsTests
{
    [Fact]
    public void Kept_ids_stay_and_new_ones_take_the_next_number_of_their_prefix()
    {
        var ids = new StableIds(kept: ["RF-001", "RF-003", "RNF-001"], used: []);

        Assert.Equal("RF-003", ids.Assign("RF-003", "RF", 3));
        Assert.Equal("RF-004", ids.Assign("nuevo", "RF", 3));
        Assert.Equal("RF-005", ids.Assign(null, "RF", 3));
        Assert.Equal("RNF-002", ids.Assign("RF-001", "RNF", 3)); // its type changed
        Assert.Equal("RT-001", ids.Assign("nuevo", "RT", 3));    // the first of its type
        Assert.Equal("EP-01", ids.Assign("nueva", "EP", 2));
    }

    [Fact]
    public void An_id_that_was_not_in_the_draft_or_is_repeated_gets_a_new_one()
    {
        var ids = new StableIds(kept: ["HU-001", "HU-002"], used: []);

        Assert.Equal("HU-001", ids.Assign("HU-001", "HU", 3));
        Assert.Equal("HU-003", ids.Assign("HU-001", "HU", 3)); // a copy
        Assert.Equal("HU-004", ids.Assign("HU-050", "HU", 3)); // typed by hand, never given
    }

    // A number given in the history of the draft is not given again: after
    // deleting RF-010 and saving, a new requirement is RF-011, not RF-010.
    [Fact]
    public void A_number_from_the_history_is_never_given_again()
    {
        var ids = new StableIds(kept: ["RF-001", "RF-002"], used: ["RF-001", "RF-002", "RF-010"]);

        Assert.Equal("RF-011", ids.Assign("nuevo", "RF", 3));
    }

    [Fact]
    public void Generating_from_scratch_still_numbers_consecutively()
    {
        const string catalog = """{"requisitos": [{"id": "RF-007", "tipo": "RF"}, {"id": "x", "tipo": "RNF"}, {"id": "RF-002", "tipo": "RF"}]}""";

        Assert.Equal(["RF-001", "RNF-001", "RF-002"], CatalogDraft.Ids(CatalogDraft.Renumber(catalog)));
    }

    private const string Catalog = """
        {"requisitos": [
          {"id": "RF-001", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema deberá permitir reservar una sala.",
           "fuentes": ["DOC-01#F001"], "preguntas": [], "discrepancia": false, "revisada": false},
          {"id": "RF-002", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema deberá permitir anular una reserva.",
           "fuentes": ["DOC-01#F001"], "preguntas": [], "discrepancia": false, "revisada": false},
          {"id": "RF-003", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema deberá avisar al anular una reserva.",
           "fuentes": ["DOC-01#F001"], "preguntas": [], "discrepancia": false, "revisada": false}
        ]}
        """;

    private const string Backlog = """
        {"epicas": [
          {"id": "EP-01", "titulo": "Reservas", "descripcion": "", "historias": [
            {"id": "HU-001", "titulo": "Anular", "como": "empleado", "quiero": "anular mi reserva", "para": "liberar la sala",
             "requisitos": ["RF-003"], "criterios": [], "tareas": [], "discrepancia": false, "revisada": false}
          ]}
        ]}
        """;

    // The reference of a story to RF-003 survives deleting RF-002: before,
    // renumbering turned RF-003 into RF-002.
    [Fact]
    public void A_story_still_points_to_its_requirement_after_deleting_another_one()
    {
        var root = JsonNode.Parse(Catalog)!;
        root["requisitos"]!.AsArray().RemoveAt(1); // RF-002 out
        var edited = root.ToJsonString();
        var saved = CatalogDraft.Normalize(edited, Catalog, CatalogDraft.Ids(Catalog));

        Assert.Equal(["RF-001", "RF-003"], CatalogDraft.Ids(saved));
        var trace = Traceability.Build(Export(saved), BacklogExport.Build("ReservaSalas", false, DateTimeOffset.Now, null, Backlog));
        Assert.Equal(["HU-001"], trace.Requirements.Single(r => r.Id == "RF-003").Stories);
        Assert.Empty(trace.MissingRequirements);
    }

    // RF-002 deleted and a new non-functional requirement: Word, PDF and the
    // CSV show the ids as they are, with their gaps.
    [Fact]
    public void The_exports_keep_the_gaps()
    {
        var root = JsonNode.Parse(Catalog)!;
        var list = root["requisitos"]!.AsArray();
        var added = list[1]!.DeepClone();
        list.RemoveAt(1);
        added["id"] = "nuevo";
        added["tipo"] = "RNF";
        list.Add(added);
        var saved = CatalogDraft.Normalize(root.ToJsonString(), Catalog, CatalogDraft.Ids(Catalog));

        var export = Export(saved);
        Assert.Equal(["RF-001", "RF-003"], export.Sections.Single(s => s.Type == "RF").Requirements.Select(r => r.Id));
        Assert.Equal(["RNF-001"], export.Sections.Single(s => s.Type == "RNF").Requirements.Select(r => r.Id));
        var jira = Encoding.UTF8.GetString(BacklogCsv.Jira(BacklogExport.Build("ReservaSalas", false, DateTimeOffset.Now, null,
            Backlog.Replace("HU-001", "HU-004").Replace("EP-01", "EP-03"))));
        Assert.Contains("\"HU-004\"", jira);
        Assert.Contains("\"EP-03\"", jira);
    }

    private static CatalogExport Export(string catalog) =>
        CatalogExport.Build("ReservaSalas", false, DateTimeOffset.Now, null, catalog,
            [new Fragment("DOC-01#F001", "DOC-01", Fragmenter.DocumentKind, "§1", "El empleado reserva y anula salas.", null)],
            "=== DOC-01 · a.md ===\n[DOC-01#F001 §1] El empleado reserva y anula salas.\n", []);
}
