using System.Text.Encodings.Web;
using System.Text.Json;
using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Projects;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Api.Runs;

// "Catalog" section of the configuration (defaults of the milestone 5 specification).
public class CatalogOptions
{
    // Estimated catalog characters per source character (the example measures 10.7).
    public double OutputRatio { get; set; } = 15;

    // Estimated output tokens per lot. Kept at 20k although the cap per answer
    // is now 64k (Runner:MaxOutputTokens): the reasoning of the model counts
    // towards that cap too.
    public int MaxOutputTokensPerLot { get; set; } = 20_000;

    // Lots of the same model that run at the same time (1 = one after another).
    public int MaxParallelLots { get; set; } = 5;

    // stream-json only writes a line per complete message, so writing a large
    // catalog can take minutes without output.
    public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromMinutes(30);
}

// A project document with its extracted text. Id: "DOC-01", "DOC-02"... by upload order.
public record CatalogDocument(string Id, string Name, string Text);

// The fragment ids of one lot.
public record LotPlan(int Number, int Total, IReadOnlyList<string> FragmentIds);

// What a job works on: all the fragments of the project and how they are split
// into lots. Saved in data/jobs/{jobId}/plan.json when the job is created, so
// every run and retry of the job uses exactly the same input.
public record CatalogPlan(
    IReadOnlyDictionary<string, string> DocumentNames,
    IReadOnlyList<Fragment> Fragments,
    IReadOnlyList<LotPlan> Lots);

// The catalog task: from the project documents to the RunRequest of each lot.
public static class CatalogTask
{
    public const string Kind = "catalog";
    public const string OutputFile = "output/catalogo.json";
    public const string FragmentsFile = "input/fragmentos.json";
    public const string PlanFile = "plan.json";

    // snake_case like the reference scripts (fragmentos.json has "source_ref"),
    // and accents written as they are, not as \u escapes.
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    public static CatalogPlan Plan(IReadOnlyList<CatalogDocument> documents, CatalogOptions options)
    {
        var fragments = documents.SelectMany(d => Fragmenter.FragmentDocument(d.Id, d.Text)).ToList();
        var lots = LotPlanner.Plan(fragments, options.OutputRatio, options.MaxOutputTokensPerLot)
            .Select(l => new LotPlan(l.Number, l.Total, l.Fragments.Select(f => f.Id).ToList()))
            .ToList();
        return new CatalogPlan(documents.ToDictionary(d => d.Id, d => d.Name), fragments, lots);
    }

    public static void SavePlan(string path, CatalogPlan plan)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(plan, Json));
    }

    public static CatalogPlan LoadPlan(string path) =>
        JsonSerializer.Deserialize<CatalogPlan>(File.ReadAllText(path), Json)
            ?? throw new InvalidDataException($"Plan vacío: {path}");

    // Prepares the run folder (input/fragmentos.json with the lot's fragments,
    // for the validator) and returns the request for that lot.
    public static RunRequest BuildRequest(
        Run run, Project project, CatalogPlan plan, string workDirectory,
        CatalogOptions options, RunnerOptions runner)
    {
        var lot = plan.Lots.Single(l => l.Number == run.LotNumber);
        var ids = lot.FragmentIds.ToHashSet();
        var fragments = plan.Fragments.Where(f => ids.Contains(f.Id)).ToList();

        var fragmentsPath = Path.Combine(workDirectory, FragmentsFile);
        Directory.CreateDirectory(Path.GetDirectoryName(fragmentsPath)!);
        // System and language travel with the fragments so that whoever reads the
        // folder alone (FakeProvider, a person) knows what the catalog is about.
        File.WriteAllText(fragmentsPath, JsonSerializer.Serialize(
            new { Version = "1.2", System = project.SystemName, project.Language, Fragments = fragments }, Json));

        var prompt = PromptBuilder.Build(new CatalogPromptInput(
            SystemName: project.SystemName,
            Language: project.Language,
            LotNumber: lot.Number,
            LotTotal: lot.Total,
            Glossary: project.Glossary ?? "",
            ResolvedDoubts: "", // the doubts phase does not exist yet
            Documentation: Fragmenter.DocumentBlock(fragments, plan.DocumentNames)));

        return new RunRequest
        {
            RunId = run.Id,
            ModelId = run.Model,
            Prompt = prompt,
            WorkDirectory = workDirectory,
            ExpectedOutputFiles = [OutputFile],
            // The prompt has all the input: the model only writes the catalog.
            // (If a model fails without Read, go back to Read,Write and note it.)
            AllowedTools = ["Write"],
            HeartbeatTimeout = options.HeartbeatTimeout,
            TotalTimeout = options.TotalTimeout,
            MaxContextTokens = runner.MaxContextTokens,
            MaxOutputTokens = runner.MaxOutputTokens,
        };
    }
}
