using System.Text.Json.Nodes;
using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Deliverables;
using ReqArbiter.Core.Phases;

namespace ReqArbiter.Tests.Deliverables;

// Step 50: the result of a catalog-v0 job becomes the working copy of the
// catalog (origin, discrepancy, explanation and "revisada"), renumbered by
// type; it is checked with the v0 validator plus the unreviewed discrepancies.
public class CatalogDraftTests
{
    private static string Example(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts-v0", "ejemplo", file));

    // The arbiter saw B as "Propuesta 1" and A as "Propuesta 2".
    private static readonly PhasePlan Plan = new(PhaseKind.Catalog, new PhaseInput { SystemName = "", Language = "" }, [], [], ["B", "A"]);

    private static readonly PhaseContext Context = new()
    {
        FragmentIds = Fragmenter.FragmentDocument("DOC-01", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fuentes", "DOC-01.txt")))
            .Select(f => f.Id).ToHashSet(),
        AnsweredQuestionIds = new HashSet<string> { "P-001", "P-002", "P-003", "P-004" },
    };

    private static JsonArray Requirements(string json) => JsonNode.Parse(json)!["requisitos"]!.AsArray();

    private static string FromArbiter() => CatalogDraft.FromResult(new PhaseJobResult("ARB", true, Example("catalogo_arbitro.json"), ""), Plan);

    [Fact]
    public void The_working_copy_keeps_origin_discrepancy_and_explanation()
    {
        var requirements = Requirements(FromArbiter());

        Assert.Equal(12, requirements.Count);
        Assert.All(requirements, r =>
        {
            Assert.Contains(r!["origen"]!.GetValue<string>(), new[] { "A", "B", "ambas" });
            Assert.False(r["revisada"]!.GetValue<bool>());
            Assert.False(string.IsNullOrEmpty(r["explicacion"]!.GetValue<string>()));
            Assert.Null(r["de"]);
        });
        Assert.Contains(requirements, r => r!["discrepancia"]!.GetValue<bool>());
    }

    [Fact]
    public void Without_cross_check_every_requirement_comes_from_the_only_model()
    {
        var requirements = Requirements(CatalogDraft.FromResult(new PhaseJobResult("B", false, Example("catalogo_propuesta_2.json"), ""), Plan));

        Assert.All(requirements, r =>
        {
            Assert.Equal("B", r!["origen"]!.GetValue<string>());
            Assert.False(r["discrepancia"]!.GetValue<bool>());
            Assert.Contains("Sin verificación cruzada", r["explicacion"]!.GetValue<string>());
        });
    }

    [Fact]
    public void Ids_are_renumbered_by_type_after_deleting_or_adding()
    {
        var root = JsonNode.Parse(FromArbiter())!;
        var list = root["requisitos"]!.AsArray();
        list.RemoveAt(0); // RF-001 out
        list.Add(new JsonObject { ["id"] = "nuevo", ["tipo"] = "RNF", ["notacion"] = "MASTeR", ["obligatoriedad"] = "should", ["texto"] = "x", ["fuentes"] = new JsonArray("DOC-01#F010"), ["preguntas"] = new JsonArray() });

        var ids = Requirements(CatalogDraft.Normalize(root.ToJsonString())).Select(r => r!["id"]!.GetValue<string>()).ToList();

        Assert.Equal("RF-001", ids[0]);
        Assert.Equal("RNF-002", ids[^1]);
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void An_unreviewed_discrepancy_keeps_it_from_being_validated()
    {
        var check = CatalogDraft.Check(FromArbiter(), Context);

        Assert.Contains(check.Errors, e => e.Contains("DISC"));
        Assert.All(check.Errors, e => Assert.Contains("DISC", e));
    }

    [Fact]
    public void With_every_discrepancy_reviewed_it_can_be_validated()
    {
        var root = JsonNode.Parse(FromArbiter())!;
        foreach (var r in root["requisitos"]!.AsArray())
            r!["revisada"] = true;

        Assert.True(CatalogDraft.Check(root.ToJsonString(), Context).Valid);
    }

    [Fact]
    public void An_edit_that_breaks_a_rule_is_reported_with_its_code()
    {
        var root = JsonNode.Parse(FromArbiter())!;
        foreach (var r in root["requisitos"]!.AsArray())
            r!["revisada"] = true;
        root["requisitos"]![0]!["obligatoriedad"] = "may"; // EARS only with shall
        root["requisitos"]![1]!["preguntas"] = new JsonArray("P-005"); // discarded question

        var errors = CatalogDraft.Check(root.ToJsonString(), Context).Errors;

        Assert.Contains(errors, e => e.StartsWith("RF-001 EARS"));
        Assert.Contains(errors, e => e.StartsWith("RF-002 REF-PREG"));
    }

    [Fact]
    public void The_next_phase_receives_only_the_schema_fields()
    {
        var clean = JsonNode.Parse(CatalogDraft.ForNextPhase(FromArbiter()))!;

        Assert.All(clean["requisitos"]!.AsArray(), r =>
            Assert.Equal(CatalogDraft.SchemaFields, r!.AsObject().Select(p => p.Key)));
        Assert.True(PhaseValidator.Validate(PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal), clean.ToJsonString(), Context).Valid);
    }
}
