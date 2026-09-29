using ReqArbiter.Core.Runner;

namespace ReqArbiter.Runner.Execution;

public static class ClaudeArguments
{
    // Each item is ONE argument. They are passed with ProcessStartInfo.ArgumentList,
    // never joined into a command-line string: the model id has spaces
    // ("SS - GPT-6-SOL") and must arrive as a single argument after --model.
    // The prompt is not here: it goes through standard input.
    //
    // Tools: --tools is the list of tools that EXIST for the model;
    // --allowedTools only says which of them run without asking. With
    // --allowedTools alone, a real run used Bash (read-only commands are
    // approved automatically) and tried a skill. --disable-slash-commands
    // removes the built-in skills, whose instructions the model had followed.
    public static IReadOnlyList<string> Build(RunRequest request)
    {
        var tools = string.Join(",", request.AllowedTools);
        return
        [
            "-p",
            "--model", request.ModelId,
            "--output-format", "stream-json",
            "--verbose",
            "--tools", tools,
            "--allowedTools", tools,
            "--disable-slash-commands",
            "--max-turns", request.MaxTurns.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ];
    }
}
