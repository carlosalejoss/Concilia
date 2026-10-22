using System.Text.Encodings.Web;
using System.Text.Json;
using Concilia.Core.Catalog;
using Concilia.Core.Matching;
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
// RepairFile (step 90): the previous output as the repair finds it in its
// file, to correct it with Edit (null if there was none or nothing to repair).
public record PhaseReview(PhaseValidation Validation, string? RepairPrompt)
{
    public bool Valid => Validation.Valid;
    public string? RepairFile { get; init; }
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
            files.Add(DocumentationOf(phase, input));
        if (phase.IsArbiter)
        {
            // With "1:" and "2:" before the ids, so the arbiter cannot mix them (ProposalIds).
            files.Add(new InputFile(Proposal1File, ProposalIds.Prefix(phase.Kind, input.Proposal1, 1), Required: true));
            files.Add(new InputFile(Proposal2File, ProposalIds.Prefix(phase.Kind, input.Proposal2, 2), Required: true));
            // The matching computed by the platform (step 88), read entirely too.
            if (input.Matching.Length > 0)
                files.Add(new InputFile(MatchingFile.Path, input.Matching, Required: true));
        }
        return files;
    }

    // The documentation a phase sees (not the backlog, which reads the catalog):
    // A and B read it entirely and the arbiter of the questions consults it.
    // Step 107 (prompts v0.12): the arbiter of the catalog consults only the
    // fragments cited by the requirements it has to decide (SourcesFile).
    public static InputFile DocumentationOf(PhaseDefinition phase, PhaseInput input) =>
        phase.IsArbiter && phase.Kind == PhaseKind.Catalog
            ? new InputFile(SourcesFile.Path, SourcesFile.Build(input.Documentation, input.Proposal1, input.Proposal2, input.Matching), Required: false)
            : new InputFile(DocumentationFile.Path, input.Documentation, Required: !phase.IsArbiter);

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
        // A key written twice in the same object (see JsonDuplicates): when its
        // values are lists the platform joins them, with a warning; otherwise
        // it cannot be checked, it is reported as such and nothing else is checked.
        var duplicates = output is null ? [] : JsonDuplicates.Find(output);
        var joined = output is null ? null : JsonDuplicates.Merge(output);
        if (duplicates.Count > 0 && joined is null)
        {
            var errors = duplicates
                .Select(d => $"{phase.OutputFile} CLAVE-DUP: la clave se repite en el mismo objeto ({d}). Escribe cada clave una sola vez y junta sus valores.")
                .ToList();
            return Repair(phase, output, new PhaseValidation(errors, []), originalPrompt, attempt);
        }
        var joinedWarnings = duplicates.Select(d => $"{phase.OutputFile} CLAVE-DUP: clave repetida unida por la plataforma ({d}).").ToList();

        // Checked without the "sigue" mark of the cited fragments
        // (FragmentCitations), with the fixes of the platform (PlatformFixes)
        // and, in the arbiter, without the prefix of the proposal in its ids
        // (ProposalIds): the platform does all of it by itself.
        // The backlog arbiter's criteria and tasks cited by their id get their text (CriteriaReferences, step 108).
        var references = joined is null ? null : CriteriaReferences.Apply(phase, FragmentCitations.Clean(joined), context);
        var fixes = references is null ? null : PlatformFixes.Apply(phase, references.Output, context);
        var cleaned = fixes?.Output;
        var checkedOutput = cleaned is null
            ? new PhaseValidation([$"{phase.OutputFile} FICHERO: no se ha escrito el fichero."], [])
            : PhaseValidator.Validate(phase, phase.IsArbiter ? ProposalIds.Strip(cleaned) : cleaned, context);
        var wrongPrefixes = phase.IsArbiter && cleaned is not null ? ProposalIds.WrongPrefixes(cleaned) : [];
        var validation = new PhaseValidation([.. wrongPrefixes, .. references?.Errors ?? [], .. checkedOutput.Errors, .. extraErrors ?? []],
            [.. joinedWarnings, .. references?.Warnings ?? [], .. fixes?.Warnings ?? [], .. checkedOutput.Warnings]);
        return validation.Valid
            ? new PhaseReview(validation, null)
            : Repair(phase, cleaned ?? output, validation, originalPrompt, attempt);
    }

    // Step 90 (decision of the author, 19-10-2026; prompts v0.11): the repair
    // does not get the previous output pasted in its prompt. It stays in its
    // file, revised by the platform (with its fixes) and with one element per
    // line, and each error says on which line its element is, so the model
    // reads only those lines and corrects them with Edit. Before, the whole
    // output went in the prompt and had to be written again entirely: an
    // arbiter's catalog (about 30,000 tokens) did not fit in 128k with the
    // proposals, and LUNA's rewriting of 191 KB was cut by the output cap.
    private static PhaseReview Repair(PhaseDefinition phase, string? output, PhaseValidation validation, string originalPrompt, int attempt)
    {
        if (output is null)
            return new PhaseReview(validation, PhasePrompt.Repair(originalPrompt, attempt, phase.OutputFile,
                $"No llegaste a escribir `{phase.OutputFile}`: escríbelo entero con Write.", validation.Errors));
        var file = ReadingPlan.OnePerLine(output);
        var note = $"Tu salida anterior sigue en `{phase.OutputFile}`, ya revisada por la plataforma y con un elemento por línea. " +
                   "Cada error dice en qué línea está su elemento: lee solo esas líneas con Read (offset y limit) y corrígelas con Edit.";
        return new PhaseReview(validation, PhasePrompt.Repair(originalPrompt, attempt, phase.OutputFile, note, Locate(validation.Errors, file)))
        {
            RepairFile = file,
        };
    }

    // Each error that starts with the id of an element of the output gets the
    // line of that element ("RF-007 TIPO: … (línea 9)"). ARB-FALTA starts with
    // an id of a proposal, which is not in the output: it stays as it is.
    private static IReadOnlyList<string> Locate(IReadOnlyList<string> errors, string file)
    {
        var lines = file.Split('\n');
        return errors.Select(error =>
        {
            var id = error.Split(' ')[0];
            var line = error.Contains(" ARB-FALTA:", StringComparison.Ordinal)
                ? -1
                : Array.FindIndex(lines, l => l.Contains($"\"id\":\"{id}\"", StringComparison.Ordinal));
            return line >= 0 ? $"{error} (línea {line + 1})" : error;
        }).ToList();
    }

    // What the platform fixes by itself in an output before checking it and
    // keeping it: repeated keys whose values are lists (JsonDuplicates), the
    // "sigue" mark of the cited fragments (FragmentCitations), the criteria and
    // tasks the backlog arbiter cites by their id (CriteriaReferences, step 108)
    // and the mistakes of format with only one fix (PlatformFixes, step 89).
    public static string? Clean(PhaseDefinition phase, string? output, PhaseContext context) =>
        output is null ? null : PlatformFixes.Apply(phase,
            CriteriaReferences.Apply(phase, FragmentCitations.Clean(JsonDuplicates.Merge(output) ?? output), context).Output, context).Output;

    // The context of the validator from the run folder (what BuildRequest wrote).
    public static PhaseContext LoadContext(string workDirectory, IReadOnlySet<string> answeredQuestionIds)
    {
        var fragments = JsonDocument.Parse(File.ReadAllText(Path.Combine(workDirectory, FragmentsFile)))
            .RootElement.GetProperty("fragments").EnumerateArray()
            .Select(f => f.GetProperty("id").GetString()!).ToHashSet();
        var kind = Enum.Parse<PhaseKind>(JsonDocument.Parse(File.ReadAllText(Path.Combine(workDirectory, PhaseFile))).RootElement.GetProperty("kind").GetString()!);
        string? Proposal(string file) => ReadIfExists(workDirectory, file) is { } p ? ProposalIds.Unprefix(kind, p) : null;
        // The arbiter of the catalog has input/fuentes.txt instead (step 107).
        var linesFile = File.Exists(Path.Combine(workDirectory, SourcesFile.Path)) ? SourcesFile.Path : DocumentationFile.Path;
        return new PhaseContext
        {
            FragmentIds = fragments,
            FragmentAtLine = DocumentationFile.FragmentAtLine(ReadIfExists(workDirectory, linesFile) ?? ""),
            LinesFile = linesFile,
            AnsweredQuestionIds = answeredQuestionIds,
            Catalog = ReadIfExists(workDirectory, CatalogFile),
            Proposal1 = Proposal(Proposal1File),
            Proposal2 = Proposal(Proposal2File),
            Matching = ReadIfExists(workDirectory, MatchingFile.Path),
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
