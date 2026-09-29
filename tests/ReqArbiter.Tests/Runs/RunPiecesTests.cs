using ReqArbiter.Api.Runs;
using ReqArbiter.Core.Runner;

namespace ReqArbiter.Tests.Runs;

// Pieces of step 21 that do not need the database: input preparation,
// the summary prompt and the in-memory event store.
public class RunPiecesTests
{
    [Fact]
    public void Input_copies_texts_with_clean_unique_names()
    {
        using var temp = new TempDirectory();
        string Text(string name, string content)
        {
            var path = Path.Combine(temp.Path, name);
            File.WriteAllText(path, content);
            return path;
        }
        var inputFolder = Path.Combine(temp.Path, "run", "input");

        var names = RunInput.Prepare(inputFolder,
        [
            new RunInputFile("acta.md", Text("a.extracted.txt", "uno")),
            new RunInputFile("acta.docx", Text("b.extracted.txt", "dos")),
            new RunInputFile("../../raro?:*.pdf", Text("c.extracted.txt", "tres")),
            new RunInputFile("...", Text("d.extracted.txt", "cuatro")),
        ]);

        Assert.Equal(["acta.txt", "acta-2.txt", "raro___.txt", "documento.txt"], names);
        Assert.Equal("dos", File.ReadAllText(Path.Combine(inputFolder, "acta-2.txt")));
        Assert.Equal(4, Directory.GetFiles(inputFolder).Length);
    }

    [Fact]
    public void Summary_prompt_uses_absolute_paths_and_asks_for_the_verdict()
    {
        var workDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "run-1"));

        var prompt = SummaryTask.Prompt(workDirectory, ["acta.txt", "requisitos.txt"]);

        Assert.Contains(Path.Combine(workDirectory, "input", "acta.txt"), prompt);
        Assert.Contains(Path.Combine(workDirectory, "input", "requisitos.txt"), prompt);
        Assert.Contains(Path.Combine(workDirectory, "output", "resumen.md"), prompt);
        Assert.Contains("VEREDICTO: {\"estado\":\"hecho\"}", prompt);
        Assert.Contains("VEREDICTO: {\"estado\":\"bloqueada\"", prompt);
    }

    [Fact]
    public async Task Event_store_replays_past_events_then_follows_new_ones()
    {
        var store = new RunEventStore();
        var runId = Guid.NewGuid();
        store.Publish(runId, new RunEvent(RunEventKind.Started, "uno"));

        var received = new List<string>();
        var reader = Task.Run(async () =>
        {
            await foreach (var e in store.Follow(runId, CancellationToken.None))
            {
                received.Add(e.Text);
            }
        });

        await Task.Delay(100);
        store.Publish(runId, new RunEvent(RunEventKind.AssistantText, "dos"));
        store.Complete(runId);
        await reader.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(["uno", "dos"], received);
    }

    [Fact]
    public async Task Event_store_follow_stops_when_the_client_leaves()
    {
        var store = new RunEventStore();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in store.Follow(Guid.NewGuid(), cancellation.Token))
            {
            }
        });
    }

    [Fact]
    public void Event_store_ignores_events_after_completion_and_numbers_them()
    {
        var store = new RunEventStore();
        var runId = Guid.NewGuid();
        store.Publish(runId, new RunEvent(RunEventKind.Started, "a"));
        store.Publish(runId, new RunEvent(RunEventKind.Finished, "b"));
        store.Complete(runId);
        store.Publish(runId, new RunEvent(RunEventKind.Warning, "tarde"));

        var events = store.Follow(runId, CancellationToken.None).ToBlockingEnumerable().ToList();

        Assert.Equal([0, 1], events.Select(e => e.Index));
        Assert.True(store.Exists(runId));
    }
}
