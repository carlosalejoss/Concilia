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
        // Only "api_retry" is a retry; other system lines (e.g. a tool call
        // denied with "permission_denied") are warnings.
        // Claude Code reports every few tokens of reasoning (seen with Sonnet
        // 5.5: hundreds of lines in a mockup run). It is not a warning and it
        // says nothing the console needs; the line still counts as activity.
        SystemEvent { Subtype: "thinking_tokens" } => null,
        SystemEvent { Subtype: "api_retry" } retry => new RunEvent(RunEventKind.ApiRetry, $"Reintento de la API: {Truncate(retry.RawJson, MaxToolTextLength)}"),
        SystemEvent { Subtype: ClaudeCliRunner.CompactBoundary } compact => new RunEvent(RunEventKind.Warning,
            $"Claude Code compactó el contexto (resumió lo anterior para seguir; puede perderse detalle): {Truncate(compact.RawJson, MaxToolTextLength)}"),
        SystemEvent system => new RunEvent(RunEventKind.Warning, $"Aviso del sistema ({system.Subtype}): {Truncate(system.RawJson, MaxToolTextLength)}"),
        InvalidLineEvent invalid => new RunEvent(RunEventKind.Warning, $"Línea no JSON: {Truncate(invalid.Line, MaxToolTextLength)}"),
        _ => null, // result (reported at the end) and unknown types
    };

    public static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
