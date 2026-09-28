using ReqArbiter.Core.Runner;

namespace ReqArbiter.Runner.Execution;

public static class ClaudeArguments
{
    // Each item is ONE argument. They are passed with ProcessStartInfo.ArgumentList,
    // never joined into a command-line string: the model id has spaces
    // ("SS - GPT-6-SOL") and must arrive as a single argument after --model.
    // The prompt is not here: it goes through standard input.
    public static IReadOnlyList<string> Build(RunRequest request) =>
    [
        "-p",
        "--model", request.ModelId,
        "--output-format", "stream-json",
        "--verbose",
        "--allowedTools", string.Join(",", request.AllowedTools),
        "--max-turns", request.MaxTurns.ToString(System.Globalization.CultureInfo.InvariantCulture),
    ];
}
