namespace ReqArbiter.Core.Runs;

// Queued and Running come first; the rest mirror Core.Runner.RunStatus.
public enum RunState
{
    Queued,
    Running,
    Done,
    Blocked,
    Failed,
    TimedOut,
    HeartbeatLost,
}

// One execution of a task with one model, as stored in the database.
// Secrets and total_cost_usd are never stored here.
public class Run
{
    public Guid Id { get; set; }

    public int ProjectId { get; set; }

    // The job this run belongs to and its side in it: "A" or "B". Retries
    // keep the slot of the run they repeat. Null for the single runs of
    // milestone 3, made before jobs existed.
    public Guid? JobId { get; set; }
    public string? Slot { get; set; }

    // For a retry: the run it repeats. Attempt is 1 for the first run.
    public Guid? RetryOfRunId { get; set; }
    public int Attempt { get; set; } = 1;

    public required string Provider { get; set; }
    public required string Model { get; set; }

    public RunState State { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    // "hecho" or "bloqueada", and the reason if the model gave one.
    public string? VerdictState { get; set; }
    public string? VerdictReason { get; set; }

    public string? ResultText { get; set; }
    public long? InputTokens { get; set; }
    public long? OutputTokens { get; set; }
    public int? NumTurns { get; set; }
    public string? ErrorMessage { get; set; }
}
