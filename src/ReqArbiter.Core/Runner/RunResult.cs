namespace ReqArbiter.Core.Runner;

public enum RunStatus
{
    Done,          // Correct: verdict "hecho" and every expected output file exists.
    Blocked,       // The model answered "bloqueada" and said why.
    Failed,        // Process error, API error, invalid verdict or missing outputs.
    TimedOut,      // TotalTimeout reached.
    HeartbeatLost, // No output line during HeartbeatTimeout.
}

public enum VerdictState
{
    Done,    // "hecho"
    Blocked, // "bloqueada"
}

// Parsed from the final "VEREDICTO: {...}" line of the model.
public record Verdict(VerdictState State, string? Reason);

public record TokenUsage(long InputTokens, long OutputTokens);

public record RunResult
{
    public required RunStatus Status { get; init; }

    // Null when the verdict is missing or invalid.
    public Verdict? Verdict { get; init; }

    // Final text of the model (the "result" field of the last event).
    public string? ResultText { get; init; }

    // Expected output files that were found, relative to the work directory.
    public IReadOnlyList<string> OutputFiles { get; init; } = [];

    // Copied from the final "result" event, when there is one.
    public int? NumTurns { get; init; }
    public long? DurationMs { get; init; }
    public string? TerminalReason { get; init; }
    public bool? IsError { get; init; }
    public TokenUsage? Usage { get; init; }

    // stderr lines and stdout lines that were not JSON. Not errors by themselves.
    public IReadOnlyList<string> Warnings { get; init; } = [];

    // Short explanation for the user when Status is not Done.
    public string? ErrorMessage { get; init; }

    // Set only when the run failed because the verdict was missing or invalid.
    // A verdict problem is worth retrying: the process and the API worked (the
    // catalog task also repairs invalid catalogs, see CatalogReview).
    public string? VerdictError { get; init; }

    // Version of Claude Code that ran the model (from the "init" event), to
    // know which version produced each result.
    public string? ClaudeCodeVersion { get; init; }

    // The Read calls of the model, in order: which lines of which files it
    // read (see ReadCoverage).
    public IReadOnlyList<FileRead> Reads { get; init; } = [];

    // How many times Claude Code compacted the context (it summarizes what
    // came before when the window fills up): detail may have been lost.
    public int Compactions { get; init; }

    // total_cost_usd is deliberately not here: Claude Code computes it for a
    // model it does not know, so the value is not reliable.
}
