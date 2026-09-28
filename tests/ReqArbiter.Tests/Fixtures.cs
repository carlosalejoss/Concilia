using ReqArbiter.Runner.StreamJson;

namespace ReqArbiter.Tests;

// Access to the files in tests/fixtures/, copied next to the test DLL.
public static class Fixtures
{
    public static string StreamJsonPath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "stream-json", name);

    // Raw bytes: the tests split lines themselves, so CRLF reaches the parser.
    public static string ReadStreamJson(string name) =>
        File.ReadAllText(StreamJsonPath(name), System.Text.Encoding.UTF8);

    // Every event of a fixture, splitting only on "\n" so "\r" stays at the end of each line.
    public static List<StreamEvent> ParseStreamJson(string name) =>
        ReadStreamJson(name).Split('\n').SelectMany(StreamJsonParser.ParseLine).ToList();
}
