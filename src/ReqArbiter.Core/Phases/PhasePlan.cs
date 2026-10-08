using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ReqArbiter.Core.Catalog;

namespace ReqArbiter.Core.Phases;

// What a job of a v0 phase works on, saved as plan.json in the job folder when
// the job is created: the input of the phase (the proposals are added for the
// arbiter when A and B finish), all the fragments, the answered questions (the
// catalog validator needs them) and the random order of the proposals.
public record PhasePlan(
    PhaseKind Kind,
    PhaseInput Input,
    IReadOnlyList<Fragment> Fragments,
    IReadOnlyList<string> AnsweredQuestionIds,
    // The slots that the arbiter sees as "Propuesta 1" and "Propuesta 2", e.g.
    // ["B", "A"]. Drawn at random so the arbiter (SOL, which is also A) cannot
    // tell which one is its own.
    IReadOnlyList<string> ProposalOrder)
{
    public const string FileName = "plan.json";

    // The job kinds of the v0 phases (Job.Kind).
    public static string JobKind(PhaseKind kind) => kind switch
    {
        PhaseKind.Questions => "questions",
        PhaseKind.Catalog => "catalog-v0",
        PhaseKind.Backlog => "backlog",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static PhaseKind? KindOfJob(string jobKind) =>
        Enum.GetValues<PhaseKind>().Cast<PhaseKind?>().FirstOrDefault(k => JobKind(k!.Value) == jobKind);

    public static IReadOnlyList<string> RandomOrder(Random random) =>
        random.Next(2) == 0 ? ["A", "B"] : ["B", "A"];

    // The context of the validator for this plan (the arbiter adds its proposals).
    public PhaseContext Context(string? proposal1 = null, string? proposal2 = null) => new()
    {
        FragmentIds = Fragments.Select(f => f.Id).ToHashSet(),
        AnsweredQuestionIds = AnsweredQuestionIds.ToHashSet(),
        Catalog = Input.Catalog.Length > 0 ? Input.Catalog : null,
        Proposal1 = proposal1,
        Proposal2 = proposal2,
    };

    // Where an element of the arbiter comes from, from its `de`: "A", "B" or
    // "ambas" (or null if `de` cites no id, which the validator does not let through).
    public string? OriginOf(JsonObject de)
    {
        var slots = new List<string>();
        for (var n = 1; n <= 2; n++)
        {
            if (de[$"propuesta_{n}"] is JsonArray ids && ids.Count > 0)
                slots.Add(ProposalOrder[n - 1]);
        }
        return slots.Count switch { 2 => "ambas", 1 => slots[0], _ => null };
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public void Save(string jobFolder)
    {
        Directory.CreateDirectory(jobFolder);
        File.WriteAllText(Path.Combine(jobFolder, FileName), JsonSerializer.Serialize(this, Json));
    }

    public static PhasePlan Load(string jobFolder) =>
        JsonSerializer.Deserialize<PhasePlan>(File.ReadAllText(Path.Combine(jobFolder, FileName)), Json)
            ?? throw new InvalidDataException($"Plan vacío: {jobFolder}");
}
