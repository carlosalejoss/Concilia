using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ReqArbiter.Core.Runner;

namespace ReqArbiter.Api.Runs;

// What the web receives for each event. Index lets the client keep the order.
public record RunEventDto(int Index, RunEventKind Kind, string Text, DateTimeOffset At);

// Keeps the progress events of each run in memory, so the SSE endpoint can
// replay what already happened and then follow new events live.
// Only in memory: after an API restart the events are gone, but the result
// of each run stays in the database.
public class RunEventStore
{
    // Safety limit so a very chatty run cannot use unbounded memory.
    public const int MaxEventsPerRun = 2000;

    private sealed class Stream
    {
        public readonly List<RunEventDto> Events = [];
        public bool Completed;

        // Completed (and replaced) every time something changes, to wake up
        // the readers waiting in Follow.
        public TaskCompletionSource Changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly ConcurrentDictionary<Guid, Stream> _streams = new();

    public bool Exists(Guid runId) => _streams.ContainsKey(runId);

    public void Publish(Guid runId, RunEvent runEvent)
    {
        var stream = _streams.GetOrAdd(runId, _ => new Stream());
        lock (stream)
        {
            if (stream.Completed || stream.Events.Count >= MaxEventsPerRun)
            {
                return;
            }
            stream.Events.Add(new RunEventDto(stream.Events.Count, runEvent.Kind, runEvent.Text, DateTimeOffset.UtcNow));
            Signal(stream);
        }
    }

    // No more events will come for this run.
    public void Complete(Guid runId)
    {
        var stream = _streams.GetOrAdd(runId, _ => new Stream());
        lock (stream)
        {
            stream.Completed = true;
            Signal(stream);
        }
    }

    // All the events of a run: first the ones already published, then the new
    // ones as they arrive, until the run is completed or the client leaves.
    public async IAsyncEnumerable<RunEventDto> Follow(Guid runId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stream = _streams.GetOrAdd(runId, _ => new Stream());
        var next = 0;

        while (true)
        {
            List<RunEventDto> batch;
            bool completed;
            Task changed;
            lock (stream)
            {
                batch = stream.Events.Skip(next).ToList();
                completed = stream.Completed;
                changed = stream.Changed.Task;
            }

            foreach (var runEvent in batch)
            {
                yield return runEvent;
            }
            next += batch.Count;

            if (completed)
            {
                yield break;
            }

            await changed.WaitAsync(cancellationToken);
        }
    }

    private static void Signal(Stream stream)
    {
        var previous = stream.Changed;
        stream.Changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        previous.SetResult();
    }
}
