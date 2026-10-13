using Concilia.Runner.Security;

namespace Concilia.Runner;

// Access through the author's Claude subscription: the long-lived token of
// "claude setup-token" (CLAUDE_CODE_OAUTH_TOKEN). Only the mockup uses it,
// and only for the author's own use (decision of 08-10-2026).
//
// The child receives this variable alone: if ANTHROPIC_AUTH_TOKEN reached it
// as well, Claude Code would use that one first and send the mockup to the
// gateway without any error.
public record SubscriptionSettings(string OAuthToken) : IModelAccess
{
    public const string TokenVariable = "CLAUDE_CODE_OAUTH_TOKEN";

    public IReadOnlyDictionary<string, string> ChildVariables => new Dictionary<string, string>
    {
        [TokenVariable] = OAuthToken,
    };

    public bool TellsContextWindow => false;

    public SecretRedactor Redactor() => new([OAuthToken]);

    // Hide the value if the record is ever printed or logged.
    public override string ToString() => "SubscriptionSettings { OAuthToken = *** }";
}
