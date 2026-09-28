// A stand-in for "claude -p", used only by the tests. It never calls any
// model or network. It behaves as told by a first stdin line:
//
//   #fake {"fixture":"...", "lineDelayMs":0, "stderr":["..."], "exitCode":0,
//          "sleepAfterMs":0, "writeFiles":["output/resumen.md"]}
//
// and saves what it received (arguments, environment and stdin) to
// fake-received.json in its working directory, so tests can check it.
//
// The instructions travel through stdin because the runner gives the child an
// explicit environment: there is no other clean way to reach it.

using System.Text;
using System.Text.Json;

var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
Console.OutputEncoding = utf8;

var options = new FakeOptions();

// Read all of stdin: the runner closes it after writing the prompt.
using var buffer = new MemoryStream();
Console.OpenStandardInput().CopyTo(buffer);
var stdinBytes = buffer.ToArray();

var text = utf8.GetString(stdinBytes);
var firstNewLine = text.IndexOf('\n');
var firstLine = firstNewLine >= 0 ? text[..firstNewLine] : text;
var prompt = text;
if (firstLine.StartsWith("#fake ", StringComparison.Ordinal))
{
    options = JsonSerializer.Deserialize<FakeOptions>(firstLine["#fake ".Length..], FakeOptions.Json) ?? options;
    prompt = firstNewLine >= 0 ? text[(firstNewLine + 1)..] : "";
}

var received = new
{
    args,
    environment = Environment.GetEnvironmentVariables()
        .Cast<System.Collections.DictionaryEntry>()
        .ToDictionary(e => (string)e.Key, e => (string?)e.Value),
    stdinHasBom = stdinBytes.AsSpan().StartsWith(Encoding.UTF8.Preamble), // EF BB BF
    prompt,
};
File.WriteAllText("fake-received.json", JsonSerializer.Serialize(received), utf8);

foreach (var line in options.Stderr)
{
    Console.Error.WriteLine(line);
}

foreach (var relativePath in options.WriteFiles)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(relativePath))!);
    File.WriteAllText(relativePath, "# Resumen\n\nEscrito por FakeClaude.\n", utf8);
}

if (options.Fixture is not null)
{
    // ReadAllLines drops the CRLF of the recordings; WriteLine adds "\n".
    foreach (var line in File.ReadAllLines(options.Fixture, utf8))
    {
        Console.Out.WriteLine(line);
        Console.Out.Flush();
        if (options.LineDelayMs > 0)
        {
            Thread.Sleep(options.LineDelayMs);
        }
    }
}

if (options.SleepAfterMs > 0)
{
    Thread.Sleep(options.SleepAfterMs);
}

return options.ExitCode;

internal sealed class FakeOptions
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public string? Fixture { get; set; }
    public int LineDelayMs { get; set; }
    public string[] Stderr { get; set; } = [];
    public int ExitCode { get; set; }
    public int SleepAfterMs { get; set; }
    public string[] WriteFiles { get; set; } = [];
}
