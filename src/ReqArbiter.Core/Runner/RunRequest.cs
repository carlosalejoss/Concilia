namespace ReqArbiter.Core.Runner;

// Everything a provider needs to run one task with one model.
// Defaults follow docs/hito-3-runner.md; they are proposals, all configurable.
public record RunRequest
{
    public required Guid RunId { get; init; }

    // Exact id from the configuration, e.g. "SS - GPT-6-SOL" (prefix and spaces included).
    public required string ModelId { get; init; }

    // Sent to the model through standard input.
    public required string Prompt { get; init; }

    // Folder of this run (data/runs/{runId}/), with input/ and output/ inside.
    public required string WorkDirectory { get; init; }

    // Files the model must leave, relative to WorkDirectory (e.g. "output/resumen.md").
    public IReadOnlyList<string> ExpectedOutputFiles { get; init; } = [];

    public IReadOnlyList<string> AllowedTools { get; init; } = ["Read", "Write"];

    public int MaxTurns { get; init; } = 10;

    // Longest time without any output line before the run is considered lost.
    public TimeSpan HeartbeatTimeout { get; init; } = TimeSpan.FromSeconds(120);

    public TimeSpan TotalTimeout { get; init; } = TimeSpan.FromMinutes(10);

    // Provisional until the real context window of the models is confirmed.
    public int MaxContextTokens { get; init; } = 128_000;

    // Estimated input tokens above which the run is not launched.
    public int InputTokenBudget { get; init; } = 80_000;
}
