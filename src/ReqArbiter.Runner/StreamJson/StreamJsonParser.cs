using System.Text.Json;

namespace ReqArbiter.Runner.StreamJson;

// Reads the output of "claude -p --output-format stream-json --verbose", one
// line at a time. Every line is a JSON object; fields are looked up by name
// (never by position) and anything unknown is ignored, because Claude Code
// adds fields between versions.
public static class StreamJsonParser
{
    public static IReadOnlyList<StreamEvent> ParseLine(string line)
    {
        // Tolerate Windows line endings and blank lines.
        var trimmed = line.TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return [];
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(trimmed);
        }
        catch (JsonException e)
        {
            return [new InvalidLineEvent(trimmed, e.Message)];
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return [new InvalidLineEvent(trimmed, "La línea no es un objeto JSON.")];
            }

            var type = GetString(root, "type");
            return type switch
            {
                "system" => [ParseSystem(root)],
                "assistant" => ParseAssistant(root),
                "user" => ParseUser(root),
                "result" => [ParseResult(root)],
                _ => [new UnknownEvent(type)],
            };
        }
    }

    private static StreamEvent ParseSystem(JsonElement root)
    {
        var subtype = GetString(root, "subtype");
        if (subtype != "init")
        {
            return new SystemEvent(subtype, root.GetRawText());
        }

        var tools = root.TryGetProperty("tools", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => t.GetString()!).ToList()
            : [];

        return new InitEvent(GetString(root, "model"), tools, GetString(root, "session_id"));
    }

    // "assistant" lines carry message.content: a list of "text" and "tool_use" blocks.
    private static List<StreamEvent> ParseAssistant(JsonElement root)
    {
        var isApiError = GetBool(root, "is_api_error_message") ?? false;
        var events = new List<StreamEvent>();

        foreach (var block in ContentBlocks(root))
        {
            switch (GetString(block, "type"))
            {
                case "text":
                    events.Add(new AssistantTextEvent(GetString(block, "text") ?? "", isApiError));
                    break;
                case "tool_use":
                    var input = block.TryGetProperty("input", out var raw) ? raw.GetRawText() : "{}";
                    events.Add(new ToolUseEvent(GetString(block, "id") ?? "", GetString(block, "name") ?? "", input));
                    break;
            }
        }

        return events;
    }

    // "user" lines carry the tool results back to the model.
    private static List<StreamEvent> ParseUser(JsonElement root)
    {
        var events = new List<StreamEvent>();

        foreach (var block in ContentBlocks(root))
        {
            if (GetString(block, "type") == "tool_result")
            {
                events.Add(new ToolResultEvent(
                    GetString(block, "tool_use_id") ?? "",
                    ToolResultContent(block),
                    GetBool(block, "is_error") ?? false));
            }
        }

        return events;
    }

    private static ResultEvent ParseResult(JsonElement root)
    {
        ResultUsage? usage = null;
        if (root.TryGetProperty("usage", out var u) && u.ValueKind == JsonValueKind.Object)
        {
            usage = new ResultUsage(
                GetLong(u, "input_tokens") ?? 0,
                GetLong(u, "cache_creation_input_tokens") ?? 0,
                GetLong(u, "cache_read_input_tokens") ?? 0,
                GetLong(u, "output_tokens") ?? 0);
        }

        // "modelUsage" is an object keyed by model id.
        var modelUsage = new List<ModelUsageEntry>();
        if (root.TryGetProperty("modelUsage", out var models) && models.ValueKind == JsonValueKind.Object)
        {
            foreach (var model in models.EnumerateObject())
            {
                modelUsage.Add(new ModelUsageEntry(
                    model.Name,
                    GetLong(model.Value, "inputTokens") ?? 0,
                    GetLong(model.Value, "outputTokens") ?? 0,
                    GetLong(model.Value, "contextWindow"),
                    GetLong(model.Value, "maxOutputTokens")));
            }
        }

        var denials = root.TryGetProperty("permission_denials", out var d) && d.ValueKind == JsonValueKind.Array
            ? d.GetArrayLength()
            : 0;

        return new ResultEvent
        {
            IsError = GetBool(root, "is_error") ?? false,
            Subtype = GetString(root, "subtype"),
            TerminalReason = GetString(root, "terminal_reason"),
            NumTurns = (int?)GetLong(root, "num_turns"),
            Result = GetString(root, "result"),
            Usage = usage,
            ModelUsage = modelUsage,
            DurationMs = GetLong(root, "duration_ms"),
            SessionId = GetString(root, "session_id"),
            PermissionDenials = denials,
            ApiErrorStatus = (int?)GetLong(root, "api_error_status"),
        };
    }

    private static IEnumerable<JsonElement> ContentBlocks(JsonElement root)
    {
        if (root.TryGetProperty("message", out var message)
            && message.ValueKind == JsonValueKind.Object
            && message.TryGetProperty("content", out var content)
            && content.ValueKind == JsonValueKind.Array)
        {
            return content.EnumerateArray().Where(b => b.ValueKind == JsonValueKind.Object).ToList();
        }

        return [];
    }

    // A tool result's content is either a string or a list of text blocks.
    private static string ToolResultContent(JsonElement block)
    {
        if (!block.TryGetProperty("content", out var content))
        {
            return "";
        }

        return content.ValueKind switch
        {
            JsonValueKind.String => content.GetString()!,
            JsonValueKind.Array => string.Join("\n", content.EnumerateArray()
                .Where(b => b.ValueKind == JsonValueKind.Object)
                .Select(b => GetString(b, "text"))
                .Where(t => t is not null)),
            _ => "",
        };
    }

    // Tolerant getters: a missing field, or one with an unexpected JSON type, gives null.
    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool? GetBool(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static long? GetLong(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number)
            ? number
            : null;
}
