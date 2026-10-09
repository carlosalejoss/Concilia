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

    // Output cap per model response (CLAUDE_CODE_MAX_OUTPUT_TOKENS). Without it
    // Claude Code assumes 32k for these models, and the reasoning of a model
    // counts towards it: in milestone 5 LUNA spent the 32k before finishing
    // the catalog, so its Write call arrived cut and could not be parsed.
    public int MaxOutputTokens { get; init; } = 64_000;

    // Estimated input tokens above which the run is not launched.
    public int InputTokenBudget { get; init; } = 80_000;

    // Whether the files in input/ count towards InputTokenBudget along with
    // the prompt. True when the model reads them whole (catalog v1.x). The v0
    // phases and the mockup set it to false: the model reads them by parts
    // with Read, so they need not fit at once, and the platform limits the
    // documentation when the job is created instead.
    public bool InputFilesInBudget { get; init; } = true;
}
