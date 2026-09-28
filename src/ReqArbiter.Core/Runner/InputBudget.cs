namespace ReqArbiter.Core.Runner;

// Rough guard against context overflow. The 10 MB upload limit does not
// protect the model's context window, so before launching a run we estimate
// its input tokens and refuse it when it exceeds RunRequest.InputTokenBudget.
public static class InputBudget
{
    // Rule of thumb: about 3.5 characters per token. It is only an estimate:
    // the real count depends on the model's tokenizer.
    public const double CharactersPerToken = 3.5;

    public static int EstimateTokens(string text) =>
        (int)Math.Ceiling(text.Length / CharactersPerToken);

    // Returns an error message for the user, or null if the input fits.
    public static string? Check(string input, int budgetTokens)
    {
        var estimated = EstimateTokens(input);
        return estimated > budgetTokens
            ? $"La entrada es demasiado grande: unos {estimated} tokens estimados, y el máximo es {budgetTokens}. Reduce el número o el tamaño de los archivos."
            : null;
    }
}
