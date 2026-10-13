namespace Concilia.Core.Runner;

// Rough guard against context overflow. The 10 MB upload limit does not
// protect the model's context window, so before launching a run we estimate
// its input tokens and refuse it when it exceeds RunRequest.InputTokenBudget.
public static class InputBudget
{
    // Rule of thumb: about 3.5 characters per token. It is only an estimate:
    // the real count depends on the model's tokenizer.
    public const double CharactersPerToken = 3.5;

    public static int EstimateTokens(string text) => EstimateTokens(text.Length);

    public static int EstimateTokens(long characters) =>
        (int)Math.Ceiling(characters / CharactersPerToken);

    // Returns an error message for the user, or null if the input fits.
    public static string? Check(string input, int budgetTokens) => Check(input.Length, budgetTokens);

    // The same, for input made of several parts (the prompt plus the files the
    // model will read), given their total number of characters.
    public static string? Check(long characters, int budgetTokens)
    {
        var estimated = EstimateTokens(characters);
        return estimated > budgetTokens
            ? $"La entrada es demasiado grande: unos {estimated} tokens estimados, y el máximo es {budgetTokens}. Reduce el número o el tamaño de los archivos."
            : null;
    }
}
