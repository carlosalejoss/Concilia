namespace ReqArbiter.Runner;

// Where and how the child process reaches the LiteLLM gateway.
// BaseUrl is the gateway address WITHOUT the final "/v1"; AuthToken is the
// gateway key. Both are secrets: never log them (see SecretRedactor).
public record GatewaySettings(string BaseUrl, string AuthToken)
{
    // Hide the values if the record is ever printed or logged.
    public override string ToString() => "GatewaySettings { BaseUrl = ***, AuthToken = *** }";
}
