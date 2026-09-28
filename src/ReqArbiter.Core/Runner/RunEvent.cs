namespace ReqArbiter.Core.Runner;

public enum RunEventKind
{
    Started,
    ModelInfo,     // The model and tools reported at start-up.
    AssistantText, // Text written by the model.
    ToolUse,       // The model calls a tool (Read, Write...).
    ToolResult,    // What the tool returned.
    ApiRetry,      // Claude Code retries a call to the API: progress, not failure.
    Warning,       // stderr line or a stdout line that is not JSON.
    Finished,
}

// A progress event, meant to be shown to the user. The text must already be
// short and free of secrets.
public record RunEvent(RunEventKind Kind, string Text);
