using Concilia.Runner.Security;

namespace Concilia.Runner;

// Where and how the child process reaches the LiteLLM gateway.
// BaseUrl is the gateway address WITHOUT the final "/v1"; AuthToken is the
// gateway key. Both are secrets: never log them (see SecretRedactor).
public record GatewaySettings(string BaseUrl, string AuthToken) : IModelAccess
{
    public IReadOnlyDictionary<string, string> ChildVariables => new Dictionary<string, string>
    {
        ["ANTHROPIC_BASE_URL"] = BaseUrl,
        ["ANTHROPIC_AUTH_TOKEN"] = AuthToken,
    };

    public bool TellsContextWindow => true;

    public SecretRedactor Redactor() => SecretRedactor.ForGateway(this);

    // Hide the values if the record is ever printed or logged.
    public override string ToString() => "GatewaySettings { BaseUrl = ***, AuthToken = *** }";
}
