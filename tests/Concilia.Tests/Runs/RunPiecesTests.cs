using Concilia.Api.Runs;
using Concilia.Core.Runner;

namespace Concilia.Tests.Runs;

// The in-memory event store behind the SSE endpoint.
public class RunPiecesTests
{
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
