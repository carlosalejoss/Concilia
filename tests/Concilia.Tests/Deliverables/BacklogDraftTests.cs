using System.Text.Json.Nodes;
using Concilia.Core.Deliverables;
using Concilia.Core.Phases;

namespace Concilia.Tests.Deliverables;

// Step 54: the result of a backlog job becomes the working copy (stories with
// origin, discrepancy, explanation and "revisada"), renumbered; it is checked
// against the validated catalog: schema, unique ids, existing requirements,
// unreviewed discrepancies and, as a warning, functional requirements without a story.
public class BacklogDraftTests
{
    private static string Example(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts-v0", "ejemplo", file));

    private static readonly PhasePlan Plan = new(PhaseKind.Backlog, new PhaseInput { SystemName = "", Language = "" }, [], [], ["A", "B"]);

    private static readonly PhaseContext Context = new() { Catalog = CatalogDraft.ForNextPhase(Example("catalogo_arbitro.json")) };

    private static string FromArbiter() => BacklogDraft.FromResult(new PhaseJobResult("ARB", true, Example("backlog_arbitro.json"), ""), Plan);

    private static IEnumerable<JsonNode> Stories(string json) =>
        JsonNode.Parse(json)!["epicas"]!.AsArray().SelectMany(e => e!["historias"]!.AsArray())!;

    private static string Reviewed(string json)
    {
        var root = JsonNode.Parse(json)!;
        foreach (var h in root["epicas"]!.AsArray().SelectMany(e => e!["historias"]!.AsArray()))
            h!["revisada"] = true;
        return root.ToJsonString();
    }

    [Fact]
    public void The_working_copy_keeps_origin_discrepancy_and_explanation_of_each_story()
    {
        var stories = Stories(FromArbiter()).ToList();

        Assert.Equal(7, stories.Count);
        Assert.All(stories, h =>
        {
            Assert.Contains(h["origen"]!.GetValue<string>(), new[] { "A", "B", "ambas" });
            Assert.False(h["revisada"]!.GetValue<bool>());
            Assert.Null(h["de"]);
        });
    }

    [Fact]
    public void Without_cross_check_every_story_comes_from_the_only_model()
    {
        var stories = Stories(BacklogDraft.FromResult(new PhaseJobResult("A", false, Example("backlog_propuesta_1.json"), ""), Plan));

        Assert.All(stories, h => Assert.Equal("A", h["origen"]!.GetValue<string>()));
    }

    [Fact]
    public void A_reviewed_backlog_from_the_arbiter_is_valid()
    {
        var check = BacklogDraft.Check(Reviewed(FromArbiter()), Context);

        Assert.True(check.Valid, string.Join("\n", check.Errors));
        Assert.Empty(check.Warnings);
    }

    [Fact]
    public void Ids_are_renumbered_after_deleting_a_story()
    {
        var root = JsonNode.Parse(FromArbiter())!;
        root["epicas"]![0]!["historias"]!.AsArray().RemoveAt(0);

        var normalized = BacklogDraft.Normalize(root.ToJsonString());

        Assert.Equal("HU-001", Stories(normalized).First()["id"]!.GetValue<string>());
        Assert.Equal("CA-001", Stories(normalized).First()["criterios"]![0]!["id"]!.GetValue<string>());
        // Deleting the only story of RF-001…RF-004 leaves them without a story: a warning, not an error.
        Assert.Contains(BacklogDraft.Check(Reviewed(normalized), Context).Warnings, w => w.Contains("COB"));
    }

    [Fact]
    public void A_requirement_that_is_not_in_the_catalog_is_an_error()
    {
        var root = JsonNode.Parse(Reviewed(FromArbiter()))!;
        root["epicas"]![0]!["historias"]![0]!["requisitos"]!.AsArray().Add("RF-099");

        Assert.Contains(BacklogDraft.Check(root.ToJsonString(), Context).Errors, e => e.Contains("REF-REQ") && e.Contains("RF-099"));
    }

    [Fact]
    public void An_unreviewed_discrepancy_keeps_it_from_being_validated()
    {
        var root = JsonNode.Parse(Reviewed(FromArbiter()))!;
        var story = root["epicas"]![0]!["historias"]![0]!;
        story["discrepancia"] = true;
        story["revisada"] = false;

        Assert.Equal(["HU-001 DISC: hay una discrepancia sin revisar."], BacklogDraft.Check(root.ToJsonString(), Context).Errors);
    }

    [Fact]
    public void The_mockup_receives_only_the_schema_fields()
    {
        var clean = JsonNode.Parse(BacklogDraft.ForNextPhase(FromArbiter()))!;

        Assert.All(clean["epicas"]!.AsArray().SelectMany(e => e!["historias"]!.AsArray()), h =>
            Assert.Equal(BacklogDraft.StoryFields, h!.AsObject().Select(p => p.Key)));
        Assert.True(PhaseValidator.Validate(PhaseDefinition.For(PhaseKind.Backlog, PhaseRole.Proposal), clean.ToJsonString(), Context).Valid);
    }
}
