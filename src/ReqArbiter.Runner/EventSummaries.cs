using ReqArbiter.Core.Runner;
using ReqArbiter.Runner.StreamJson;

namespace ReqArbiter.Runner;

// Turns stream-json events into short progress events for the user.
// The input is already redacted; this only shortens it.
public static class EventSummaries
{
    public const int MaxTextLength = 500;
    public const int MaxToolTextLength = 200;

    public static RunEvent? From(StreamEvent streamEvent) => streamEvent switch
    {
        InitEvent init => new RunEvent(RunEventKind.ModelInfo, $"Modelo: {init.Model}"),
        AssistantTextEvent text => new RunEvent(RunEventKind.AssistantText, Truncate(text.Text, MaxTextLength)),
        ToolUseEvent tool => new RunEvent(RunEventKind.ToolUse, $"{tool.Name} {Truncate(tool.Input, MaxToolTextLength)}"),
        ToolResultEvent result => new RunEvent(
            RunEventKind.ToolResult,
            (result.IsError ? "Error: " : "") + Truncate(result.Content, MaxToolTextLength)),
        SystemEvent system => new RunEvent(RunEventKind.ApiRetry, $"Aviso del sistema ({system.Subtype}): {Truncate(system.RawJson, MaxToolTextLength)}"),
        InvalidLineEvent invalid => new RunEvent(RunEventKind.Warning, $"Línea no JSON: {Truncate(invalid.Line, MaxToolTextLength)}"),
        _ => null, // result (reported at the end) and unknown types
    };

    public static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
