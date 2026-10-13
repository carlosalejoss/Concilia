using Concilia.Runner.Security;

namespace Concilia.Runner;

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

    // The gateway models have a 128k window (confirmed by the team leader),
    // below the 200k Claude Code assumes for a model it does not recognize,
    // so it is told (CLAUDE_CODE_MAX_CONTEXT_TOKENS). The subscription is not
    // limited by the gateway: Claude Code keeps its own value. (Claude Code
    // 2.1.283 does not recognize claude-sonnet-5-5 either and assumes 200k;
    // smoke test of 09-10-2026.)
    bool TellsContextWindow { get; }

    SecretRedactor Redactor();
}
