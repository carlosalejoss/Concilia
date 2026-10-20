using Concilia.Runner.StreamJson;

namespace Concilia.Tests.Runner;

public class StreamJsonParserTests
{
    private const string VerdictDone = """VEREDICTO: {"estado":"hecho"}""";

    // --- Real recordings ---

    [Fact]
    public void Sol_with_tools_reads_init_tools_and_result()
    {
        var events = Fixtures.ParseStreamJson("tools-sol.jsonl");

        var init = Assert.IsType<InitEvent>(events[0]);
        Assert.Equal("SS - GPT-6-SOL", init.Model);
        Assert.Equal("2.1.283", init.ClaudeCodeVersion);
        Assert.Contains("Read", init.Tools);
        Assert.Contains("Write", init.Tools);

        Assert.Equal(["Read", "Write"], events.OfType<ToolUseEvent>().Select(t => t.Name));
        Assert.Equal(2, events.OfType<ToolResultEvent>().Count());
        Assert.Equal(VerdictDone, events.OfType<AssistantTextEvent>().Single().Text);

        var result = Assert.IsType<ResultEvent>(events[^1]);
        Assert.False(result.IsError);
        Assert.Equal("completed", result.TerminalReason);
        Assert.Equal(3, result.NumTurns);
        Assert.Equal(VerdictDone, result.Result);
        Assert.Equal(8488, result.DurationMs);
        Assert.Null(result.ApiErrorStatus);
        Assert.Equal(0, result.PermissionDenials);
    }

    [Fact]
    public void Sol_usage_counts_cached_input_tokens()
    {
        var result = Fixtures.ParseStreamJson("tools-sol.jsonl").OfType<ResultEvent>().Single();

        Assert.Equal(new ResultUsage(1772, 15442, 28904, 148), result.Usage);
        Assert.Equal(1772 + 15442 + 28904, result.Usage!.TotalInputTokens);

        var model = Assert.Single(result.ModelUsage);
        Assert.Equal("SS - GPT-6-SOL", model.Model);
        Assert.Equal(200_000, model.ContextWindow);
        Assert.Equal(32_000, model.MaxOutputTokens);
    }

    [Fact]
    public void Luna_with_tools_reads_the_extra_glob_call()
    {
        var events = Fixtures.ParseStreamJson("tools-luna.jsonl");

        Assert.Equal("SS - GPT-6-LUNA", events.OfType<InitEvent>().Single().Model);
        Assert.Equal(["Read", "Glob", "Write"], events.OfType<ToolUseEvent>().Select(t => t.Name));
        Assert.Equal("No files found", events.OfType<ToolResultEvent>().ElementAt(1).Content);

        var result = events.OfType<ResultEvent>().Single();
        Assert.Equal(4, result.NumTurns);
        Assert.Equal(VerdictDone, result.Result);
    }

    [Fact]
    public void Tool_use_keeps_its_arguments_and_links_to_its_result()
    {
        var events = Fixtures.ParseStreamJson("tools-sol.jsonl");

        var read = events.OfType<ToolUseEvent>().First();
        Assert.Contains("/work/entrada.txt", read.Input);

        var readResult = events.OfType<ToolResultEvent>().First();
        Assert.Equal(read.ToolUseId, readResult.ToolUseId);
        Assert.Contains("ReservaSalas", readResult.Content);
        Assert.False(readResult.IsError);
    }

    [Fact]
    public void Without_tools_keeps_utf8_accents_and_dash()
    {
        var events = Fixtures.ParseStreamJson("sin-herramientas.jsonl");

        Assert.Empty(events.OfType<ToolUseEvent>());
        var result = events.OfType<ResultEvent>().Single();
        Assert.Equal(1, result.NumTurns);
        Assert.Contains("reservación de sala — prueba", result.Result);
        Assert.EndsWith(VerdictDone, result.Result);
    }

    [Fact]
    public void Unknown_model_error_is_read_from_result_not_from_subtype()
    {
        var events = Fixtures.ParseStreamJson("error-modelo-inexistente.jsonl");

        var message = events.OfType<AssistantTextEvent>().Single();
        Assert.True(message.IsApiError);
        Assert.Contains("403", message.Text);

        var result = events.OfType<ResultEvent>().Single();
        Assert.True(result.IsError);
        Assert.Equal("success", result.Subtype); // Misleading on purpose: never use it as a success flag.
        Assert.Equal("api_error", result.TerminalReason);
        Assert.Equal(403, result.ApiErrorStatus);
        Assert.Empty(result.ModelUsage);
    }

    // --- Synthetic fixtures ---

    [Fact]
    public void Cut_output_has_no_result_event()
    {
        var events = Fixtures.ParseStreamJson("synthetic-cortada.jsonl");

        Assert.NotEmpty(events.OfType<ToolUseEvent>());
        Assert.Empty(events.OfType<ResultEvent>());
    }

    [Fact]
    public void Api_retry_is_a_system_event_not_an_error()
    {
        var events = Fixtures.ParseStreamJson("synthetic-reintento-api.jsonl");

        var retry = Assert.IsType<SystemEvent>(events[1]);
        Assert.Equal("api_retry", retry.Subtype);
        Assert.False(events.OfType<ResultEvent>().Single().IsError);
    }

    [Theory]
    [InlineData("synthetic-sin-veredicto.jsonl", "He escrito el archivo salida.json.")]
    [InlineData("synthetic-veredicto-invalido.jsonl", "VEREDICTO: {estado: hecho")]
    [InlineData("synthetic-bloqueada.jsonl", """VEREDICTO: {"estado":"bloqueada","motivo":"El archivo de entrada no describe ningún requisito."}""")]
    public void Synthetic_verdict_variants_reach_the_result_text(string fixture, string expected)
    {
        var result = Fixtures.ParseStreamJson(fixture).OfType<ResultEvent>().Single();

        Assert.Equal(expected, result.Result);
    }

    // --- Robustness ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r")]
    public void Blank_lines_give_no_events(string line)
    {
        Assert.Empty(StreamJsonParser.ParseLine(line));
    }

    [Theory]
    [InlineData("[claude-code] texto suelto")]
    [InlineData("{\"type\":\"result\",")]
    [InlineData("[1,2,3]")]
    public void Lines_that_are_not_json_objects_become_warnings(string line)
    {
        var invalid = Assert.IsType<InvalidLineEvent>(Assert.Single(StreamJsonParser.ParseLine(line)));

        Assert.Equal(line, invalid.Line);
    }

    [Fact]
    public void Unknown_types_and_fields_are_ignored()
    {
        var events = StreamJsonParser.ParseLine("""{"type":"rate_limit_event","algo_nuevo":{"x":1}}""");

        Assert.Equal("rate_limit_event", Assert.IsType<UnknownEvent>(Assert.Single(events)).Type);
    }

    [Fact]
    public void Key_order_does_not_matter_and_crlf_is_tolerated()
    {
        const string line = "{\"result\":\"ok\",\"num_turns\":2,\"is_error\":false,\"type\":\"result\"}\r";

        var result = Assert.IsType<ResultEvent>(Assert.Single(StreamJsonParser.ParseLine(line)));

        Assert.Equal("ok", result.Result);
        Assert.Equal(2, result.NumTurns);
    }

    [Fact]
    public void Tool_result_content_as_list_of_blocks_is_joined()
    {
        const string line = """{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"t1","is_error":true,"content":[{"type":"text","text":"uno"},{"type":"text","text":"dos"}]}]}}""";

        var toolResult = Assert.IsType<ToolResultEvent>(Assert.Single(StreamJsonParser.ParseLine(line)));

        Assert.Equal("uno\ndos", toolResult.Content);
        Assert.True(toolResult.IsError);
    }

    [Fact]
    public void Fields_with_unexpected_types_are_treated_as_missing()
    {
        const string line = """{"type":"result","is_error":"no","num_turns":"tres","result":42}""";

        var result = Assert.IsType<ResultEvent>(Assert.Single(StreamJsonParser.ParseLine(line)));

        Assert.False(result.IsError);
        Assert.Null(result.NumTurns);
        Assert.Null(result.Result);
    }

    [Fact]
    public void Every_line_of_every_fixture_is_valid_json()
    {
        var files = Directory.GetFiles(Path.GetDirectoryName(Fixtures.StreamJsonPath("x"))!, "*.jsonl");
        Assert.Equal(14, files.Length);

        foreach (var file in files)
        {
            var events = Fixtures.ParseStreamJson(Path.GetFileName(file));
            Assert.Empty(events.OfType<InvalidLineEvent>());
            Assert.Empty(events.OfType<UnknownEvent>());
        }
    }
}
