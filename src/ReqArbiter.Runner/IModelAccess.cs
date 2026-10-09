using ReqArbiter.Runner.Security;

namespace ReqArbiter.Runner;

// How the claude child process reaches its model: the variables that give it
// access and the secrets that must never leave it (logs, events, database).
// Two implementations: GatewaySettings (the LiteLLM gateway) and
// SubscriptionSettings (the author's Claude subscription, mockup only). Each
// child receives the variables of ONE of them, never both.
public interface IModelAccess
{
    // Added to the child environment, which is otherwise built from scratch
    // (see ChildEnvironment).
    IReadOnlyDictionary<string, string> ChildVariables { get; }

    // Claude Code does not know the gateway models, so it is told their
    // context window (CLAUDE_CODE_MAX_CONTEXT_TOKENS). It knows the Claude
    // models of the subscription, so it is not told theirs.
    bool TellsContextWindow { get; }

    SecretRedactor Redactor();
}
