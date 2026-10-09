using System.Text.Json;
using System.Text.Json.Nodes;
using ReqArbiter.Core.Phases;

namespace ReqArbiter.Tests.Phases;

// The C# validator of the v0 phases must agree with the reference
// prompts/v0/validar_v0.py: for every case exported by test_v0.py (the nine
// examples as they are and one mutation per code), the same error and warning
// lines in the same order. SCHEMA lines are summed up in one "SCHEMA" line on
// both sides, because the two JSON Schema libraries word them differently.
public class PhaseValidatorTests
{
    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static string Example(string file) => Path.Combine(AppContext.BaseDirectory, "prompts-v0", "ejemplo", file);

    private static string Mutation(string file) => Path.Combine(AppContext.BaseDirectory, "fixtures", "v0-mutations", file);

    // The context of test_v0.py: the ReservaSalas fragments, the answered
    // questions of respuestas.json, the arbiter's catalog (for the backlog), the
    // two proposals of the phase (for the arbiter) and the questions the example
    // suggestions answer, as if they were pending.
    private static PhaseContext ContextFor(string phase)
    {
        var fragments = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fragmentos.json")))!
            ["fragments"]!.AsArray().Select(f => f!["id"]!.GetValue<string>()).ToHashSet();
        var answered = JsonNode.Parse(File.ReadAllText(Example("respuestas.json")))!["preguntas"]!.AsArray()
            .Where(p => p!["estado"]!.GetValue<string>() == "respondida").Select(p => p!["id"]!.GetValue<string>()).ToHashSet();
        return new PhaseContext
        {
            FragmentIds = fragments,
            AnsweredQuestionIds = answered,
            Catalog = File.ReadAllText(Example("catalogo_arbitro.json")),
            Proposal1 = phase == "sugerencias" ? null : File.ReadAllText(Example($"{phase}_propuesta_1.json")),
            Proposal2 = phase == "sugerencias" ? null : File.ReadAllText(Example($"{phase}_propuesta_2.json")),
            PendingQuestionIds = ["P-002", "P-003", "P-004"],
        };
    }

    private static PhaseDefinition DefinitionFor(string phase, bool arbiter) => PhaseDefinition.For(
        phase switch
        {
            "preguntas" => PhaseKind.Questions,
            "catalogo" => PhaseKind.Catalog,
            "sugerencias" => PhaseKind.Suggestions,
            _ => PhaseKind.Backlog,
        },
        arbiter ? PhaseRole.Arbiter : PhaseRole.Proposal);

    // Same as resumir() in test_v0.py.
    private static List<string> Summed(IEnumerable<string> lines)
    {
        var all = lines.ToList();
        var others = all.Where(l => !l.StartsWith("SCHEMA ", StringComparison.Ordinal)).ToList();
        return others.Count < all.Count ? ["SCHEMA", .. others] : others;
    }

    public record Case(string File, string Name, string Base, string Phase, bool Arbiter, string Kind, string? Code);

    private static List<Case> LoadManifest() =>
        JsonSerializer.Deserialize<List<Case>>(File.ReadAllText(Mutation("manifest.json")), SnakeCase)!;

    public static TheoryData<string> CaseFiles()
    {
        var data = new TheoryData<string>();
        foreach (var c in LoadManifest())
            data.Add(c.File);
        return data;
    }

    [Fact]
    public void Manifest_has_the_46_cases_of_the_reference()
    {
        Assert.Equal(46, LoadManifest().Count);
    }

    [Theory]
    [MemberData(nameof(CaseFiles))]
    public void Each_case_gives_the_lines_of_the_reference(string file)
    {
        var c = LoadManifest().Single(x => x.File == file);
        var expected = JsonNode.Parse(File.ReadAllText(Mutation("expected-outcomes.json")))![file]!;

        var result = PhaseValidator.Validate(DefinitionFor(c.Phase, c.Arbiter), File.ReadAllText(Mutation(file)), ContextFor(c.Phase));

        Assert.Equal(expected["errors"]!.AsArray().Select(x => x!.GetValue<string>()), Summed(result.Errors));
        Assert.Equal(expected["warnings"]!.AsArray().Select(x => x!.GetValue<string>()), Summed(result.Warnings));
    }

    [Fact]
    public void Text_that_is_not_json_is_an_error_not_an_exception()
    {
        var result = PhaseValidator.Validate(PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal), "{ esto no es JSON", ContextFor("catalogo"));

        Assert.False(result.Valid);
        Assert.StartsWith("JSON no válido", Assert.Single(result.Errors));
    }
}
