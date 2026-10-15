using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Concilia.Core.Runner;

namespace Concilia.Api.Runs;

// What the web receives for each event. Index lets the client keep the order.
public record RunEventDto(int Index, RunEventKind Kind, string Text, DateTimeOffset At);

// Keeps the progress events of each run in memory, so the SSE endpoint can
// replay what already happened and then follow new events live. When a run
// ends, its events are also written to a file (savedFile: the path for each
// run, events.json in its folder), so its console survives an API restart
// (15-10-2026: a restart to change the model lost the console of a mockup
// that had just failed). The events of a run still in progress when the API
// stops are lost.
public class RunEventStore(Func<Guid, string>? savedFile = null)
{
    // Safety limit so a very chatty run cannot use unbounded memory.
    public const int MaxEventsPerRun = 2000;

    public const string FileName = "events.json";

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private sealed class Stream
    {
        public readonly List<RunEventDto> Events = [];
        public bool Completed;

        // Completed (and replaced) every time something changes, to wake up
        // the readers waiting in Follow.
        public TaskCompletionSource Changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly ConcurrentDictionary<Guid, Stream> _streams = new();

    // Whether the run has events, in memory or saved.
    public bool Exists(Guid runId) =>
        _streams.ContainsKey(runId) || (savedFile is not null && File.Exists(savedFile(runId)));

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

    // No more events will come for this run: they are saved, if there is a file.
    public void Complete(Guid runId)
    {
        var stream = _streams.GetOrAdd(runId, _ => new Stream());
        List<RunEventDto> events;
        lock (stream)
        {
            stream.Completed = true;
            events = [.. stream.Events];
            Signal(stream);
        }
        Save(runId, events);
    }

    private void Save(Guid runId, List<RunEventDto> events)
    {
        if (savedFile is null)
            return;
        try
        {
            var path = savedFile(runId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(events, Json));
        }
        catch (IOException)
        {
            // Not saved: the console is only lost after a restart, as before.
        }
    }

    // A run that is not in memory: its saved events, as a completed stream.
    private Stream LoadOrNew(Guid runId)
    {
        var stream = new Stream();
        if (savedFile is null || !File.Exists(savedFile(runId)))
            return stream;
        try
        {
            stream.Events.AddRange(JsonSerializer.Deserialize<List<RunEventDto>>(File.ReadAllText(savedFile(runId)), Json) ?? []);
            stream.Completed = true;
        }
        catch (Exception e) when (e is IOException or JsonException)
        {
            // An unreadable file: as if there were no events.
        }
        return stream;
    }

    // All the events of a run: first the ones already published, then the new
    // ones as they arrive, until the run is completed or the client leaves.
    public async IAsyncEnumerable<RunEventDto> Follow(Guid runId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stream = _streams.GetOrAdd(runId, LoadOrNew);
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
