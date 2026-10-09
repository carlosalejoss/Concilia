using System.Text.Encodings.Web;
using System.Text.Json;
using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Runner;

namespace ReqArbiter.Core.Phases;

// Limits of a phase run; they come from the configuration (step 43).
public record PhaseOptions
{
    public int InputTokenBudget { get; init; } = 80_000;
    public int MaxContextTokens { get; init; } = 128_000;
    public int MaxOutputTokens { get; init; } = 64_000;
    public TimeSpan HeartbeatTimeout { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan TotalTimeout { get; init; } = TimeSpan.FromMinutes(30);
}

// The request of one run, or why it cannot be launched (the input does not fit).
public record PhaseRequest(RunRequest? Request, string? Error);

// After an attempt: is the output valid, its errors and warnings and, when it
// is not valid, the prompt of the repair.
public record PhaseReview(PhaseValidation Validation, string? RepairPrompt)
{
    public bool Valid => Validation.Valid;
}

// The generic task of a v0 phase (step 42): one run with all the documentation
// (no lots), its validation and one repair with the list of errors. Joining the
// runs of A, B and the arbiter in a job is step 43.
public static class PhaseTask
{
    public const string PhaseFile = "input/fase.json"; // which phase and role (for FakeProvider and whoever reads the folder)
    public const string FragmentsFile = "input/fragmentos.json";
    public const string CatalogFile = "input/catalogo.json";
    public const string Proposal1File = "input/propuesta_1.json";
    public const string Proposal2File = "input/propuesta_2.json";
    public const string PendingFile = "input/pendientes.json";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    // Builds the prompt and checks it fits in InputTokenBudget BEFORE writing
    // anything. The prompt carries the whole input (the model only writes its
    // output), and input/ keeps copies for the validator and FakeProvider: the
    // fragments and, when the phase uses them, the validated catalog and the
    // two proposals.
    // pendingQuestionIds: the questions of a suggestions run (input/pendientes.json).
    public static PhaseRequest BuildRequest(
        PhaseDefinition phase, PhaseInput input, IReadOnlyList<Fragment> fragments,
        Guid runId, string modelId, string workDirectory, PhaseOptions options,
        IReadOnlyList<string>? pendingQuestionIds = null)
    {
        var prompt = PhasePrompt.Build(phase, input);
        var budgetError = InputBudget.Check(prompt, options.InputTokenBudget);
        if (budgetError is not null)
            return new PhaseRequest(null, budgetError);

        Write(workDirectory, PhaseFile, JsonSerializer.Serialize(new { Kind = phase.Kind.ToString(), Role = phase.Role.ToString(), phase.OutputFile }, Json));
        Write(workDirectory, FragmentsFile, JsonSerializer.Serialize(new { input.SystemName, input.Language, Fragments = fragments }, Json));
        if (input.Catalog.Length > 0)
            Write(workDirectory, CatalogFile, input.Catalog);
        if (pendingQuestionIds is not null)
            Write(workDirectory, PendingFile, JsonSerializer.Serialize(pendingQuestionIds, Json));
        if (phase.IsArbiter)
        {
            Write(workDirectory, Proposal1File, input.Proposal1);
            Write(workDirectory, Proposal2File, input.Proposal2);
        }

        return new PhaseRequest(new RunRequest
        {
            RunId = runId,
            ModelId = modelId,
            Prompt = prompt,
            WorkDirectory = workDirectory,
            ExpectedOutputFiles = [phase.OutputFile],
            AllowedTools = ["Write"],
            HeartbeatTimeout = options.HeartbeatTimeout,
            TotalTimeout = options.TotalTimeout,
            MaxContextTokens = options.MaxContextTokens,
            MaxOutputTokens = options.MaxOutputTokens,
            InputTokenBudget = options.InputTokenBudget,
        }, null);
    }

    // Validates the output of an attempt (null if the model did not write it).
    // attempt: the number of the repair it may ask for (1 after the first attempt).
    public static PhaseReview Review(
        PhaseDefinition phase, string? output, PhaseContext context, string originalPrompt, int attempt)
    {
        var validation = output is null
            ? new PhaseValidation([$"{phase.OutputFile} FICHERO: no se ha escrito el fichero."], [])
            : PhaseValidator.Validate(phase, output, context);
        var repair = validation.Valid
            ? null
            : PhasePrompt.Repair(originalPrompt, attempt, phase.OutputFile, output ?? "", validation.Errors);
        return new PhaseReview(validation, repair);
    }

    // The context of the validator from the run folder (what BuildRequest wrote).
    public static PhaseContext LoadContext(string workDirectory, IReadOnlySet<string> answeredQuestionIds)
    {
        var fragments = JsonDocument.Parse(File.ReadAllText(Path.Combine(workDirectory, FragmentsFile)))
            .RootElement.GetProperty("fragments").EnumerateArray()
            .Select(f => f.GetProperty("id").GetString()!).ToHashSet();
        return new PhaseContext
        {
            FragmentIds = fragments,
            AnsweredQuestionIds = answeredQuestionIds,
            Catalog = ReadIfExists(workDirectory, CatalogFile),
            Proposal1 = ReadIfExists(workDirectory, Proposal1File),
            Proposal2 = ReadIfExists(workDirectory, Proposal2File),
            PendingQuestionIds = ReadIfExists(workDirectory, PendingFile) is { } pending
                ? JsonSerializer.Deserialize<List<string>>(pending)!
                : [],
        };
    }

    private static void Write(string workDirectory, string relativePath, string content)
    {
        var path = Path.Combine(workDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static string? ReadIfExists(string workDirectory, string relativePath)
    {
        var path = Path.Combine(workDirectory, relativePath);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
