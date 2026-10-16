using System.Text.Encodings.Web;
using System.Text.Json;
using Concilia.Core.Catalog;
using Concilia.Core.Runner;

namespace Concilia.Core.Phases;

// Limits of a phase run; they come from the configuration (step 43).
public record PhaseOptions
{
    public int InputTokenBudget { get; init; } = 80_000;
    public int MaxContextTokens { get; init; } = 128_000;
    public int MaxOutputTokens { get; init; } = 64_000;
    public TimeSpan HeartbeatTimeout { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan TotalTimeout { get; init; } = TimeSpan.FromMinutes(30);

    // Reading by parts takes one turn per range, so more than the default 10.
    public int MaxTurns { get; init; } = 30;
}

// The request of one run, or why it cannot be launched (the prompt does not
// fit). RequiredReads: the files of input/ the model must read entirely.
public record PhaseRequest(RunRequest? Request, string? Error, IReadOnlyList<string>? RequiredReads = null);

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

    // The files of input/ the model reads by parts (decision of 09-10-2026):
    // A and B read the documentation entirely (the backlog, the validated
    // catalog instead); the arbiter reads the two proposals entirely and may
    // consult the documentation or, in the backlog, must read the catalog.
    public static IReadOnlyList<InputFile> InputFilesOf(PhaseDefinition phase, PhaseInput input)
    {
        var files = new List<InputFile>();
        if (phase.Kind == PhaseKind.Backlog)
            files.Add(new InputFile(CatalogFile, ReadingPlan.OnePerLine(input.Catalog), Required: true));
        else
            files.Add(new InputFile(DocumentationFile.Path, input.Documentation, Required: !phase.IsArbiter));
        if (phase.IsArbiter)
        {
            // With "1:" and "2:" before the ids, so the arbiter cannot mix them (ProposalIds).
            files.Add(new InputFile(Proposal1File, ProposalIds.Prefix(phase.Kind, input.Proposal1, 1), Required: true));
            files.Add(new InputFile(Proposal2File, ProposalIds.Prefix(phase.Kind, input.Proposal2, 2), Required: true));
        }
        return files;
    }

    // Builds the prompt and checks it fits in InputTokenBudget BEFORE writing
    // anything. The prompt carries the small inputs and the reading plan; the
    // big ones go to input/ and the model reads them by parts with Read, so
    // they do not count in the budget (the documentation is limited when the
    // job is created). input/ also keeps copies for the validator and
    // FakeProvider: the phase and the fragments.
    // pendingQuestionIds: the questions of a suggestions run (input/pendientes.json).
    public static PhaseRequest BuildRequest(
        PhaseDefinition phase, PhaseInput input, IReadOnlyList<Fragment> fragments,
        Guid runId, string modelId, string workDirectory, PhaseOptions options,
        IReadOnlyList<string>? pendingQuestionIds = null)
    {
        var files = InputFilesOf(phase, input);
        var prompt = PhasePrompt.Build(phase, input with { Reading = ReadingPlan.Block(files) });
        var budgetError = InputBudget.Check(prompt, options.InputTokenBudget);
        if (budgetError is not null)
            return new PhaseRequest(null, budgetError);

        Write(workDirectory, PhaseFile, JsonSerializer.Serialize(new { Kind = phase.Kind.ToString(), Role = phase.Role.ToString(), phase.OutputFile }, Json));
        Write(workDirectory, FragmentsFile, JsonSerializer.Serialize(new { input.SystemName, input.Language, Fragments = fragments }, Json));
        if (pendingQuestionIds is not null)
            Write(workDirectory, PendingFile, JsonSerializer.Serialize(pendingQuestionIds, Json));
        foreach (var file in files)
            Write(workDirectory, file.Path, file.Content);

        return new PhaseRequest(new RunRequest
        {
            RunId = runId,
            ModelId = modelId,
            Prompt = prompt,
            WorkDirectory = workDirectory,
            ExpectedOutputFiles = [phase.OutputFile],
            AllowedTools = ["Read", "Write"],
            MaxTurns = options.MaxTurns,
            HeartbeatTimeout = options.HeartbeatTimeout,
            TotalTimeout = options.TotalTimeout,
            MaxContextTokens = options.MaxContextTokens,
            MaxOutputTokens = options.MaxOutputTokens,
            InputTokenBudget = options.InputTokenBudget,
            InputFilesInBudget = false,
        }, null, files.Where(f => f.Required).Select(f => f.Path).ToList());
    }

    // One error per required file the model did not read entirely, with the
    // lines it is missing (reads: those of every attempt of the run so far).
    public static IReadOnlyList<string> UnreadErrors(string workDirectory, IEnumerable<string> requiredReads, IEnumerable<FileRead> reads)
    {
        var errors = new List<string>();
        foreach (var path in requiredReads)
        {
            var full = Path.Combine(workDirectory, path);
            var lines = File.Exists(full) ? ReadCoverage.LineCount(File.ReadAllText(full)) : 0;
            var missing = ReadCoverage.Missing(path, lines, reads);
            if (missing.Count > 0)
                errors.Add($"{path} LECTURA: no has leído las líneas {ReadCoverage.Describe(missing)} (de {lines}). " +
                           "Léelas con Read (offset y limit) y corrige la salida si cambia algo.");
        }
        return errors;
    }

    // Validates the output of an attempt (null if the model did not write it).
    // attempt: the number of the repair it may ask for (1 after the first attempt).
    // extraErrors: errors found outside the output (the lines left unread).
    public static PhaseReview Review(
        PhaseDefinition phase, string? output, PhaseContext context, string originalPrompt, int attempt,
        IReadOnlyList<string>? extraErrors = null)
    {
        // A key written twice in the same object cannot be checked (see
        // JsonDuplicates): it is reported as such, and nothing else is checked.
        var duplicates = output is null ? [] : JsonDuplicates.Find(output);
        if (duplicates.Count > 0)
        {
            var errors = duplicates
                .Select(d => $"{phase.OutputFile} CLAVE-DUP: la clave se repite en el mismo objeto ({d}). Escribe cada clave una sola vez y junta sus valores.")
                .ToList();
            return new PhaseReview(new PhaseValidation(errors, []), PhasePrompt.Repair(originalPrompt, attempt, phase.OutputFile, output!, errors));
        }

        // Checked without the "sigue" mark of the cited fragments
        // (FragmentCitations) and, in the arbiter, without the prefix of the
        // proposal in its ids (ProposalIds): the platform removes both.
        var cleaned = output is null ? null : FragmentCitations.Clean(output);
        var checkedOutput = cleaned is null
            ? new PhaseValidation([$"{phase.OutputFile} FICHERO: no se ha escrito el fichero."], [])
            : PhaseValidator.Validate(phase, phase.IsArbiter ? ProposalIds.Strip(cleaned) : cleaned, context);
        var wrongPrefixes = phase.IsArbiter && cleaned is not null ? ProposalIds.WrongPrefixes(cleaned) : [];
        var validation = new PhaseValidation([.. wrongPrefixes, .. checkedOutput.Errors, .. extraErrors ?? []], checkedOutput.Warnings);
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
        var kind = Enum.Parse<PhaseKind>(JsonDocument.Parse(File.ReadAllText(Path.Combine(workDirectory, PhaseFile))).RootElement.GetProperty("kind").GetString()!);
        string? Proposal(string file) => ReadIfExists(workDirectory, file) is { } p ? ProposalIds.Unprefix(kind, p) : null;
        return new PhaseContext
        {
            FragmentIds = fragments,
            FragmentAtLine = DocumentationFile.FragmentAtLine(ReadIfExists(workDirectory, DocumentationFile.Path) ?? ""),
            AnsweredQuestionIds = answeredQuestionIds,
            Catalog = ReadIfExists(workDirectory, CatalogFile),
            Proposal1 = Proposal(Proposal1File),
            Proposal2 = Proposal(Proposal2File),
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
