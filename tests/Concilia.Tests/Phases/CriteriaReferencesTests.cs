using System.Text.Json.Nodes;
using Concilia.Core.Phases;

namespace Concilia.Tests.Phases;

// Step 108 (prompts v0.12): the arbiter of the backlog cites by their id the
// criteria and tasks it keeps, and the platform puts their text.
public class CriteriaReferencesTests
{
    private static string Example(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts-v0", "ejemplo", file));

    private static readonly PhaseDefinition Arbiter = PhaseDefinition.For(PhaseKind.Backlog, PhaseRole.Arbiter);

    private static PhaseContext Context() => new()
    {
        Catalog = Example("catalogo_arbitro.json"),
        Proposal1 = Example("backlog_propuesta_1.json"),
        Proposal2 = Example("backlog_propuesta_2.json"),
    };

    // The example of the arbiter with the criteria and tasks of its first story
    // (which joins HU-001 of both proposals) replaced by what the arbiter writes now.
    private static string WithFirstStory(JsonArray criteria, JsonArray tasks)
    {
        var root = JsonNode.Parse(Example("backlog_arbitro.json"))!;
        var story = root["epicas"]![0]!["historias"]![0]!;
        story["criterios"] = criteria;
        story["tareas"] = tasks;
        story["de"] = new JsonObject { ["propuesta_1"] = new JsonArray("1:HU-001"), ["propuesta_2"] = new JsonArray("2:HU-001") };
        return root.ToJsonString();
    }

    // The same, with only that story in the backlog, to count exactly.
    private static string OnlyFirstStory(JsonArray criteria, JsonArray tasks)
    {
        var root = JsonNode.Parse(WithFirstStory(criteria, tasks))!;
        var epic = root["epicas"]![0]!;
        var story = epic["historias"]![0]!.DeepClone();
        epic["historias"] = new JsonArray(story);
        root["epicas"] = new JsonArray(epic.DeepClone());
        return root.ToJsonString();
    }

    private static JsonNode FirstStory(string output) => JsonNode.Parse(output)!["epicas"]![0]!["historias"]![0]!;

    [Fact]
    public void Cited_criteria_and_tasks_get_their_text_and_everything_is_numbered_again()
    {
        var written = new JsonObject { ["escenario"] = "Combinado", ["dado"] = "a", ["cuando"] = "b", ["entonces"] = "c" };
        var output = WithFirstStory(new JsonArray("1:CA-002", "2:CA-001", written), new JsonArray("2:T-003"));

        var expanded = CriteriaReferences.Apply(Arbiter, output, Context());

        Assert.Empty(expanded.Errors);
        var story = FirstStory(expanded.Output);
        var proposal1 = JsonNode.Parse(Example("backlog_propuesta_1.json"))!["epicas"]![0]!["historias"]![0]!;
        var proposal2 = JsonNode.Parse(Example("backlog_propuesta_2.json"))!["epicas"]![0]!["historias"]![0]!;
        Assert.Equal(["CA-001", "CA-002", "CA-003"], story["criterios"]!.AsArray().Select(c => c!["id"]!.GetValue<string>()));
        Assert.Equal(proposal1["criterios"]![1]!["entonces"]!.GetValue<string>(), story["criterios"]![0]!["entonces"]!.GetValue<string>());
        Assert.Equal(proposal2["criterios"]![0]!["entonces"]!.GetValue<string>(), story["criterios"]![1]!["entonces"]!.GetValue<string>());
        Assert.Equal("Combinado", story["criterios"]![2]!["escenario"]!.GetValue<string>());
        Assert.Equal("T-001", story["tareas"]![0]!["id"]!.GetValue<string>());
        Assert.Equal(proposal2["tareas"]![2]!["titulo"]!.GetValue<string>(), story["tareas"]![0]!["titulo"]!.GetValue<string>());
        // The next story goes on with the numbers.
        var second = JsonNode.Parse(expanded.Output)!["epicas"]![0]!["historias"]![1]!;
        Assert.Equal("CA-004", second["criterios"]![0]!["id"]!.GetValue<string>());
    }

    [Fact]
    public void The_warning_counts_what_was_cited_and_what_was_left_out()
    {
        // HU-001 joins 4 criteria of proposal 1 and 2 of proposal 2, and 4 + 3 tasks.
        var output = OnlyFirstStory(new JsonArray("1:CA-001", "1:CA-002"), new JsonArray("1:T-001"));

        var warning = Assert.Single(CriteriaReferences.Apply(Arbiter, output, Context()).Warnings);

        Assert.StartsWith("output/backlog.json ARB-CITA: la plataforma pone el texto de 2 criterios y 1 tareas citados por su id;", warning);
        Assert.Contains("De los 6 criterios y 7 tareas de las historias que une, no cita 4 criterios y 6 tareas", warning);
    }

    // Step 143 (prompts v0.14): a big story shared out among several small
    // ones is in the `de` of each; its criteria count once, and none of them
    // is left out if each one is cited in some story.
    [Fact]
    public void A_story_shared_out_among_several_counts_its_criteria_once()
    {
        var output = SharedOut(onlyThose: true);

        var warning = Assert.Single(CriteriaReferences.Apply(Arbiter, output, Context()).Warnings);

        // 1:HU-001 (4 and 4, all cited) and 2:HU-001 (2 and 3, none cited).
        // Story by story it would have counted 10 criteria and 6 left out.
        Assert.Contains("De los 6 criterios y 7 tareas de las historias que une, no cita 2 criterios y 3 tareas", warning);
    }

    [Fact]
    public void The_review_accepts_a_story_in_the_de_of_several()
    {
        var review = PhaseTask.Review(Arbiter, SharedOut(onlyThose: false), Context(), "PROMPT", 1);

        Assert.DoesNotContain(review.Validation.Errors, e => e.Contains("HU-001") || e.Contains("HU-002"));
    }

    // The example of the arbiter whose first story (1:HU-001, 4 criteria and
    // 4 tasks) is shared out between two stories, HU-001 and a new HU-002;
    // the rest of the stories are numbered again after them. onlyThose: the
    // backlog has only those two stories, to count exactly.
    private static string SharedOut(bool onlyThose)
    {
        var root = JsonNode.Parse(Example("backlog_arbitro.json"))!;
        var stories = root["epicas"]![0]!["historias"]!.AsArray();
        var first = stories[0]!;
        first["de"] = new JsonObject { ["propuesta_1"] = new JsonArray("1:HU-001"), ["propuesta_2"] = new JsonArray("2:HU-001") };
        var second = first.DeepClone();
        first["criterios"] = new JsonArray("1:CA-001", "1:CA-002");
        first["tareas"] = new JsonArray("1:T-001", "1:T-002");
        second["criterios"] = new JsonArray("1:CA-003", "1:CA-004");
        second["tareas"] = new JsonArray("1:T-003", "1:T-004");
        second["de"] = new JsonObject { ["propuesta_1"] = new JsonArray("1:HU-001"), ["propuesta_2"] = new JsonArray() };
        stories.Insert(1, second);
        if (onlyThose)
        {
            while (stories.Count > 2)
                stories.RemoveAt(2);
            root["epicas"] = new JsonArray(root["epicas"]![0]!.DeepClone());
        }
        var number = 0;
        foreach (var story in root["epicas"]!.AsArray().SelectMany(e => e!["historias"]!.AsArray()))
            story!["id"] = $"HU-{++number:000}";
        return root.ToJsonString();
    }

    [Fact]
    public void A_cited_id_that_is_not_in_its_proposal_is_an_error_and_is_taken_out()
    {
        var output = WithFirstStory(new JsonArray("1:CA-001", "1:CA-999", "CA-002"), new JsonArray("1:T-001"));

        var expanded = CriteriaReferences.Apply(Arbiter, output, Context());

        Assert.Equal(2, expanded.Errors.Count);
        Assert.Contains(expanded.Errors, e => e.StartsWith("HU-001 ARB-CITA: 1:CA-999 no es un criterio de la propuesta 1."));
        Assert.Contains(expanded.Errors, e => e.StartsWith("HU-001 ARB-CITA: CA-002 no es un criterio de ninguna propuesta: le falta el número de su propuesta delante."));
        Assert.Single(FirstStory(expanded.Output)["criterios"]!.AsArray());
    }

    [Fact]
    public void The_review_accepts_a_backlog_with_cited_criteria()
    {
        var output = WithFirstStory(new JsonArray("1:CA-001", "1:CA-002", "1:CA-003", "1:CA-004"), new JsonArray("1:T-001", "1:T-002"));

        var review = PhaseTask.Review(Arbiter, output, Context(), "PROMPT", 1);

        Assert.True(review.Valid, string.Join(" | ", review.Validation.Errors));
        Assert.Contains(review.Validation.Warnings, w => w.Contains("ARB-CITA"));
        Assert.Equal(4, FirstStory(PhaseTask.Clean(Arbiter, output, Context())!)["criterios"]!.AsArray().Count);
    }

    [Fact]
    public void Other_phases_are_not_touched()
    {
        var output = Example("catalogo_arbitro.json");

        var expanded = CriteriaReferences.Apply(PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Arbiter), output, Context());

        Assert.Same(output, expanded.Output);
        Assert.Empty(expanded.Warnings);
    }

    [Fact]
    public void The_arbiter_sees_the_criteria_and_tasks_with_the_number_of_their_proposal()
    {
        var prefixed = JsonNode.Parse(ProposalIds.Prefix(PhaseKind.Backlog, Example("backlog_propuesta_2.json"), 2))!;
        var story = prefixed["epicas"]![0]!["historias"]![0]!;

        Assert.Equal("2:HU-001", story["id"]!.GetValue<string>());
        Assert.Equal("2:CA-001", story["criterios"]![0]!["id"]!.GetValue<string>());
        Assert.Equal("2:T-001", story["tareas"]![0]!["id"]!.GetValue<string>());
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(Example("backlog_propuesta_2.json")),
            JsonNode.Parse(ProposalIds.Unprefix(PhaseKind.Backlog, prefixed.ToJsonString()))));
    }
}
