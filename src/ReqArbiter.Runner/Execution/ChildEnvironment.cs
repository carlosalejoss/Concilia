using System.Collections;
using System.Globalization;

namespace ReqArbiter.Runner.Execution;

// Builds the environment of the claude process from scratch. Nothing is
// inherited except a short list of variables the operating system needs to
// start a program; in particular ANTHROPIC_API_KEY and any user credentials
// never reach the child.
public static class ChildEnvironment
{
    // Copied from the parent only if present. PATH finds node/claude; the
    // Windows ones are needed by Windows programs (Node included) to start.
    public static readonly IReadOnlyList<string> InheritedVariables =
        ["PATH", "PATHEXT", "SystemRoot", "WINDIR", "ComSpec", "TEMP", "TMP"];

    public static Dictionary<string, string> Build(
        GatewaySettings gateway,
        RunFolders folders,
        int maxContextTokens,
        int maxOutputTokens,
        IDictionary parentEnvironment)
    {
        // Variable names are case-insensitive on Windows and case-sensitive elsewhere.
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var environment = new Dictionary<string, string>(comparer);

        foreach (var name in InheritedVariables)
        {
            var value = Lookup(parentEnvironment, name, comparer);
            if (value is not null)
            {
                environment[name] = value;
            }
        }

        // HOME (USERPROFILE on Windows) points inside the run folder, so the
        // child sees no configuration of the user running the API.
        environment["HOME"] = folders.Home;
        environment["USERPROFILE"] = folders.Home;

        environment["CLAUDE_CONFIG_DIR"] = folders.ClaudeConfig;
        environment["CLAUDE_CODE_MAX_CONTEXT_TOKENS"] = maxContextTokens.ToString(CultureInfo.InvariantCulture);
        environment["CLAUDE_CODE_MAX_OUTPUT_TOKENS"] = maxOutputTokens.ToString(CultureInfo.InvariantCulture);
        environment["ANTHROPIC_BASE_URL"] = gateway.BaseUrl;
        environment["ANTHROPIC_AUTH_TOKEN"] = gateway.AuthToken;

        return environment;
    }

    private static string? Lookup(IDictionary parent, string name, StringComparer comparer)
    {
        foreach (DictionaryEntry entry in parent)
        {
            if (entry.Key is string key && comparer.Equals(key, name))
            {
                return entry.Value as string;
            }
        }
        return null;
    }
}
