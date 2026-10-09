using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Runs;
using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Deliverables;
using ReqArbiter.Core.Flow;
using ReqArbiter.Core.Mockup;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Tests.Mockup;

// Step 60: the checks of the mockup .html, the page FakeProvider writes and a
// mockup job with its repair, with the backlog of the ReservaSalas example.
public class MockupTaskTests
{
    private static string Example(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts-v0", "ejemplo", file));

    // The validated backlog as the mockup receives it.
    private static readonly string Backlog = BacklogDraft.ForNextPhase(Example("backlog_arbitro.json"));

    private static readonly MockupPlan Plan = MockupPlan.From("el sistema ReservaSalas", "es", Backlog);

    // A page that passes every check: one view per story of the plan.
    private static string Valid(string extra = "") =>
        "<!doctype html><html><body>"
        + string.Concat(Plan.StoryIds.Select(id => $"<section data-hu=\"{id}\"><a href=\"#{id}\">{id}</a></section>"))
        + extra
        + "<script>const DATOS = { salas: [] };</script></body></html>";

    [Fact]
    public void The_plan_has_the_stories_of_the_backlog_in_order()
    {
        Assert.NotEmpty(Plan.StoryIds);
        Assert.Equal("HU-001", Plan.StoryIds[0]);
        Assert.Equal(Plan.StoryIds.Count, Plan.StoryIds.Distinct().Count());
    }

    [Fact]
    public void The_backlog_goes_to_a_file_the_model_reads_entirely()
    {
        using var temp = new TempDirectory();

        var request = MockupTask.BuildRequest(Plan, Guid.NewGuid(), "claude-sonnet-5-5", temp.Path, new MockupRunOptions());

        Assert.Contains("<nombre_sistema>el sistema ReservaSalas</nombre_sistema>", request.Prompt);
        Assert.Contains("- `input/backlog.json` (", request.Prompt);
        var firstTitle = JsonNode.Parse(Backlog)!["epicas"]![0]!["historias"]![0]!["titulo"]!.GetValue<string>();
        Assert.DoesNotContain(firstTitle, request.Prompt);
        Assert.DoesNotContain("{{", request.Prompt);
        Assert.Equal(["Read", "Write"], request.AllowedTools);
        Assert.False(request.InputFilesInBudget);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(Backlog), JsonNode.Parse(File.ReadAllText(Path.Combine(temp.Path, MockupTask.BacklogFile)))));
    }

    [Fact]
    public void A_page_with_every_view_is_valid()
    {
        var result = MockupTask.Validate(Valid(), Plan.StoryIds);

        Assert.True(result.Valid, string.Join(" | ", result.Errors));
        Assert.Empty(result.Warnings);
    }

    [Theory]
    [InlineData("<script src=\"https://cdn.invalid/x.js\"></script>")]
    [InlineData("<link rel=\"stylesheet\" href=\"//fonts.invalid/f.css\">")]
    [InlineData("<img src=http://img.invalid/a.png>")]
    [InlineData("<style>body { background: url('https://img.invalid/f.png'); }</style>")]
    [InlineData("<style>@import \"https://fonts.invalid/f.css\";</style>")]
    public void An_external_resource_is_an_error(string extra)
    {
        var result = MockupTask.Validate(Valid(extra), Plan.StoryIds);

        Assert.StartsWith("output/mockup.html EXTERNO:", Assert.Single(result.Errors));
    }

    [Fact]
    public void Each_rule_has_its_code()
    {
        var withoutLastView = Valid().Replace($"data-hu=\"{Plan.StoryIds[^1]}\"", "");

        Assert.Equal(["output/mockup.html FICHERO: no se ha escrito el fichero."], MockupTask.Validate(null, Plan.StoryIds).Errors);
        Assert.Contains("DOCTYPE", Assert.Single(MockupTask.Validate(Valid()[15..], Plan.StoryIds).Errors));
        Assert.Contains("DATOS", Assert.Single(MockupTask.Validate(Valid().Replace("const DATOS", "let datos"), Plan.StoryIds).Errors));
        Assert.Equal([$"{Plan.StoryIds[^1]} HU: ninguna vista lleva data-hu=\"{Plan.StoryIds[^1]}\"."], MockupTask.Validate(withoutLastView, Plan.StoryIds).Errors);
        Assert.Contains("TAMANO", Assert.Single(MockupTask.Validate(Valid(new string('x', MockupTask.MaxBytes)), Plan.StoryIds).Errors));
    }

    [Fact]
    public void A_view_of_a_story_that_is_not_in_the_backlog_is_a_warning()
    {
        var result = MockupTask.Validate(Valid("<section data-hu=\"HU-999\"></section>"), Plan.StoryIds);

        Assert.True(result.Valid);
        Assert.Equal(["HU-999 HU-EXTRA: hay una vista para una historia que no está en el backlog."], result.Warnings);
    }

    [Fact]
    public void The_repair_sends_the_errors_and_leaves_out_a_previous_page_too_big()
    {
        var small = MockupTask.Review(Valid().Replace("const DATOS", "let datos"), Plan.StoryIds, "PROMPT", 1);
        var big = MockupTask.Review("<html>" + new string('x', MockupTask.MaxPreviousOutputChars), Plan.StoryIds, "PROMPT", 1);

        Assert.StartsWith("PROMPT", small.RepairPrompt);
        Assert.Contains("let datos", small.RepairPrompt);
        Assert.Contains("DATOS: falta", small.RepairPrompt);
        Assert.Contains("No se incluye", big.RepairPrompt);
        Assert.DoesNotContain(new string('x', 1000), big.RepairPrompt);
    }

    [Fact]
    public void The_fake_page_is_valid_and_escapes_the_texts()
    {
        using var temp = new TempDirectory();
        var withTags = Backlog.Replace("\"titulo\": \"", "\"titulo\": \"<b>");
        MockupTask.BuildRequest(MockupPlan.From("s", "es", withTags), Guid.NewGuid(), "m", temp.Path, new MockupRunOptions());

        var html = FakeMockup.For(temp.Path, broken: false);

        Assert.True(MockupTask.Validate(html, Plan.StoryIds).Valid);
        Assert.Contains("&lt;b&gt;", html);
        Assert.DoesNotContain("<b>", html);
    }

    [Fact]
    public void Generating_the_mockup_outdates_nothing_and_a_new_backlog_outdates_it()
    {
        var mockup = new Deliverable { Kind = DeliverableKind.Mockup, Content = "{}" };

        var (_, change) = DeliverableRules.Generated(null, 1, DeliverableKind.Mockup, Guid.NewGuid(), "{}", DateTimeOffset.UtcNow);
        DeliverableRules.MarkOutdated([mockup], change);
        Assert.Equal(FlowChange.MockupGenerated, change);
        Assert.False(mockup.Outdated);

        DeliverableRules.MarkOutdated([mockup], FlowChange.BacklogReopened);
        Assert.True(mockup.Outdated);
    }

    // The job without the database: one run with FakeProvider, which leaves
    // out the last view in its first attempt, so the repair completes it.
    [Fact]
    public async Task A_mockup_job_runs_once_and_is_repaired_once()
    {
        using var temp = new TempDirectory();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = temp.Path })
            .Build();
        var storage = new FileStorage(configuration, new HostingEnvironment { ContentRootPath = temp.Path });
        var provider = new FakeProvider(simulateRepair: true);
        var events = new RunEventStore();
        var runner = new JobRunner(
            new RunExecutor(provider, events, storage, new RunnerOptions(), new CatalogOptions()), new CatalogOptions(),
            mockupExecutor: new MockupExecutor(provider, events, storage, new MockupOptions()));
        var run = new Run { Id = Guid.NewGuid(), Slot = "MOCK", Provider = "Fake", Model = "claude-sonnet-5-5", State = RunState.Queued };

        var outcome = await runner.RunMockupAsync(run, Plan, _ => Task.CompletedTask, _ => Task.CompletedTask, CancellationToken.None);

        Assert.True(outcome.Succeeded);
        var attempts = outcome.Attempts!;
        Assert.Equal([RunStatus.Failed, RunStatus.Done], attempts.Select(a => a.Result.Status));
        Assert.Contains($"{Plan.StoryIds[^1]} HU:", attempts[0].Result.ErrorMessage);
        Assert.Equal("claude-sonnet-5-5", attempts[0].Request.ModelId);
        var html = File.ReadAllText(MockupExecutor.OutputPath(attempts[^1]));
        Assert.True(MockupTask.Validate(html, Plan.StoryIds).Valid);
    }
}
