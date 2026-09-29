using System.Text.Json;
using ReqArbiter.Runner;
using ReqArbiter.Runner.Execution;

namespace ReqArbiter.Tests.Runner;

// Helpers to run the fake "claude" (tests/FakeClaude) with a fake gateway.
public static class FakeClaudeLauncher
{
    // Fake values only: no real key or address ever appears in tests.
    public static readonly GatewaySettings FakeGateway = new("https://gateway.invalid:4000", "fake-token-123456");

    public static string ExecutablePath =>
        Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "FakeClaude.exe" : "FakeClaude");

    // The "#fake {...}" first line of stdin that tells FakeClaude how to behave.
    public static string Directive(
        string? fixture = null,
        int lineDelayMs = 0,
        string[]? stderr = null,
        int exitCode = 0,
        int sleepAfterMs = 0,
        string[]? writeFiles = null)
    {
        var options = new
        {
            fixture = fixture is null ? null : Fixtures.StreamJsonPath(fixture),
            lineDelayMs,
            stderr = stderr ?? [],
            exitCode,
            sleepAfterMs,
            writeFiles = writeFiles ?? [],
        };
        return "#fake " + JsonSerializer.Serialize(options) + "\n";
    }

    public static ProcessLaunch Launch(
        RunFolders folders,
        string stdin,
        IReadOnlyList<string>? arguments = null,
        TimeSpan? heartbeat = null,
        TimeSpan? total = null) => new(
            ExecutablePath,
            arguments ?? [],
            folders.Root,
            ChildEnvironment.Build(FakeGateway, folders, 128_000, 64_000, Environment.GetEnvironmentVariables()),
            stdin,
            heartbeat ?? TimeSpan.FromSeconds(30),
            total ?? TimeSpan.FromSeconds(60));

    // What FakeClaude saved about the call it received.
    public static JsonElement Received(RunFolders folders) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(folders.Root, "fake-received.json"))).RootElement;
}
