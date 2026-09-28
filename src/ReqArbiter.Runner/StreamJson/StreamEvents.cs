namespace ReqArbiter.Runner.StreamJson;

// One meaningful piece of a stream-json line. A single "assistant" line may
// hold several blocks (text and tool calls), so one line can give several events.
public abstract record StreamEvent;

// "system" / "init": first line, with the model and the available tools.
public record InitEvent(string? Model, IReadOnlyList<string> Tools, string? SessionId) : StreamEvent;

// Text written by the model. IsApiError marks the message Claude Code
// synthesises when the API call fails (e.g. a 403 from the gateway).
public record AssistantTextEvent(string Text, bool IsApiError) : StreamEvent;

// The model calls a tool. Input is the raw JSON of the arguments.
public record ToolUseEvent(string ToolUseId, string Name, string Input) : StreamEvent;

// What a tool returned to the model.
public record ToolResultEvent(string ToolUseId, string Content, bool IsError) : StreamEvent;

// Any "system" line other than "init", such as an API retry. It is progress, not failure.
public record SystemEvent(string? Subtype, string RawJson) : StreamEvent;

// The last line of a run.
public record ResultEvent : StreamEvent
{
    public bool IsError { get; init; }

    // Not a reliable success flag: a 403 still comes with subtype "success".
    public string? Subtype { get; init; }

    public string? TerminalReason { get; init; }
    public int? NumTurns { get; init; }

    // Final text of the model; it should end with the VEREDICTO line.
    public string? Result { get; init; }

    public ResultUsage? Usage { get; init; }
    public IReadOnlyList<ModelUsageEntry> ModelUsage { get; init; } = [];
    public long? DurationMs { get; init; }
    public string? SessionId { get; init; }
    public int PermissionDenials { get; init; }
    public int? ApiErrorStatus { get; init; }
}

public record ResultUsage(long InputTokens, long CacheCreationInputTokens, long CacheReadInputTokens, long OutputTokens)
{
    // Tokens the model actually read: new input plus the cached parts. The
    // ~15,000-token base context of every run shows up as cache tokens.
    public long TotalInputTokens => InputTokens + CacheCreationInputTokens + CacheReadInputTokens;
}

// Per-model figures from "modelUsage". ContextWindow tells which context size
// Claude Code assumed for the model.
public record ModelUsageEntry(string Model, long InputTokens, long OutputTokens, long? ContextWindow, long? MaxOutputTokens);

// A line with a "type" this parser does not handle. Ignored by the runner.
public record UnknownEvent(string? Type) : StreamEvent;

// A stdout line that is not a JSON object. Recorded as a warning; never fatal.
public record InvalidLineEvent(string Line, string Error) : StreamEvent;
