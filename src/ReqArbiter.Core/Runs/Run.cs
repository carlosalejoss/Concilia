namespace ReqArbiter.Core.Runs;

// Queued and Running come first; the rest mirror Core.Runner.RunStatus, plus
// Skipped: a run that was not launched (the arbiter, when A or B did not finish well).
public enum RunState
{
    Queued,
    Running,
    Done,
    Blocked,
    Failed,
    TimedOut,
    HeartbeatLost,
    Skipped,
}

// One execution of a task with one model, as stored in the database.
// Secrets and total_cost_usd are never stored here.
public class Run
{
    public Guid Id { get; set; }

    public int ProjectId { get; set; }

    // The job this run belongs to and its side in it: "A", "B" or "ARB" (the
    // arbiter of a v0 phase). Retries
    // keep the slot of the run they repeat. Null for the single runs of
    // milestone 3, made before jobs existed.
    public Guid? JobId { get; set; }
    public string? Slot { get; set; }

    // Which lot of the job's documentation this run processes (1-based), and
    // how many lots there are. Retries keep the lot of the run they repeat.
    public int? LotNumber { get; set; }
    public int? LotTotal { get; set; }

    // What exactly was sent: prompt version ("req-1.2") and SHA-256 of the
    // assembled prompt, and the Claude Code version that ran it.
    public string? PromptVersion { get; set; }
    public string? PromptSha256 { get; set; }
    public string? ClaudeCodeVersion { get; set; }

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
