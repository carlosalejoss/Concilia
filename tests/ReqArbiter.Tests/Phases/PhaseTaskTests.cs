using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Phases;
using static ReqArbiter.Tests.Runner.FakeClaudeLauncher;

namespace ReqArbiter.Tests.Phases;

// The generic task of a v0 phase with ReservaSalas: the prompt of each phase is
// filled in one pass with all the documentation, the run is refused when it
// does not fit in InputTokenBudget, input/ keeps what the validator needs, and
// an invalid output leads to one repair with the list of errors.
public class PhaseTaskTests
{
    private const string System = "el sistema ReservaSalas";

    private static string Example(string file) => Path.Combine(AppContext.BaseDirectory, "prompts-v0", "ejemplo", file);

    private static readonly List<Fragment> Fragments = Fragmenter.FragmentDocument(
        "DOC-01", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fuentes", "DOC-01.txt")));

    private static List<PhaseQuestion> Questions() =>
        JsonNode.Parse(File.ReadAllText(Example("respuestas.json")))!["preguntas"]!.AsArray()
            .Select(p => new PhaseQuestion(p!["id"]!.GetValue<string>(), p["pregunta"]!.GetValue<string>(),
                                           p["estado"]!.GetValue<string>(), p["respuesta"]?.GetValue<string>()))
            .ToList();

    private static PhaseInput Input(string documentation) => new()
    {
        SystemName = System,
        Language = "es",
        Glossary = "sala: espacio de reunión reservable",
        Documentation = documentation,
        PreviousQuestions = PhasePrompt.PreviousQuestionsBlock(Questions()),
        Answers = PhasePrompt.AnswersBlock(Questions()),
        Catalog = File.ReadAllText(Example("catalogo_arbitro.json")),
        Proposal1 = File.ReadAllText(Example("catalogo_propuesta_1.json")),
        Proposal2 = File.ReadAllText(Example("catalogo_propuesta_2.json")),
    };

    private static string Documentation => Fragmenter.DocumentBlock(Fragments, new Dictionary<string, string> { ["DOC-01"] = "DOC-01.txt" });

    public static TheoryData<PhaseKind, PhaseRole> AllPhases()
    {
        var data = new TheoryData<PhaseKind, PhaseRole>();
        foreach (var kind in Enum.GetValues<PhaseKind>())
            foreach (var role in Enum.GetValues<PhaseRole>())
                data.Add(kind, role);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllPhases))]
    public void Every_placeholder_of_every_phase_is_filled(PhaseKind kind, PhaseRole role)
    {
        var prompt = PhasePrompt.Build(PhaseDefinition.For(kind, role), Input(Documentation));

        Assert.DoesNotMatch(new Regex(@"\{\{[A-Z_0-9]+\}\}"), prompt);
        Assert.Contains(System, prompt);
        Assert.Contains("output/", prompt);
    }

    [Fact]
    public void The_whole_documentation_goes_in_one_call()
    {
        var prompt = PhasePrompt.Build(PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal), Input(Documentation));

        Assert.All(Fragments, f => Assert.Contains($"<f id=\"{f.Id}\"", prompt));
    }

    [Fact]
    public void A_document_with_placeholders_reaches_the_model_as_it_is()
    {
        var prompt = PhasePrompt.Build(PhaseDefinition.For(PhaseKind.Questions, PhaseRole.Proposal),
            Input("<f id=\"DOC-01#F001\" sec=\"§1\">Plantilla: {{NOMBRE_SISTEMA}} y {{IDIOMA}}</f>"));

        Assert.Contains("Plantilla: {{NOMBRE_SISTEMA}} y {{IDIOMA}}", prompt);
    }

    [Fact]
    public void Previous_questions_keep_their_status_and_answers_only_the_answered_ones()
    {
        var previous = PhasePrompt.PreviousQuestionsBlock(Questions());
        var answers = PhasePrompt.AnswersBlock(Questions());

        Assert.Contains("<pregunta id=\"P-005\" estado=\"descartada\"><texto>", previous);
        Assert.DoesNotContain("<respuesta>", previous.Split('\n').Single(l => l.Contains("P-005")));
        Assert.Contains("<pregunta id=\"P-001\" estado=\"respondida\"><texto>", previous);
        Assert.Equal(4, answers.Split('\n').Length);
        Assert.DoesNotContain("P-005", answers);
    }

    [Fact]
    public void Angle_brackets_of_questions_and_answers_cannot_open_a_tag()
    {
        var block = PhasePrompt.AnswersBlock([new PhaseQuestion("P-001", "¿Vale <script>?", "respondida", "No </respuesta>")]);

        Assert.Equal("<pregunta id=\"P-001\"><texto>¿Vale ＜script＞?</texto><respuesta>No ＜/respuesta＞</respuesta></pregunta>", block);
    }

    [Fact]
    public void The_request_writes_its_input_and_expects_the_output_of_the_phase()
    {
        using var temp = new TempDirectory();
        var phase = PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Arbiter);
        var runId = Guid.NewGuid();

        var result = PhaseTask.BuildRequest(phase, Input(Documentation), Fragments, runId, "SS - GPT-6-SOL", temp.Path, new PhaseOptions());

        Assert.Null(result.Error);
        var request = result.Request!;
        Assert.Equal(runId, request.RunId);
        Assert.Equal(["output/catalogo.json"], request.ExpectedOutputFiles);
        Assert.Equal(["Write"], request.AllowedTools);
        foreach (var file in new[] { PhaseTask.FragmentsFile, PhaseTask.CatalogFile, PhaseTask.Proposal1File, PhaseTask.Proposal2File })
            Assert.True(File.Exists(Path.Combine(temp.Path, file)), file);

        var context = PhaseTask.LoadContext(temp.Path, new HashSet<string> { "P-001" });
        Assert.Equal(Fragments.Select(f => f.Id).ToHashSet(), context.FragmentIds);
        Assert.Equal(File.ReadAllText(Example("catalogo_propuesta_1.json")), context.Proposal1);
    }

    [Fact]
    public void A_proposal_run_does_not_write_the_proposals()
    {
        using var temp = new TempDirectory();

        PhaseTask.BuildRequest(PhaseDefinition.For(PhaseKind.Questions, PhaseRole.Proposal), Input(Documentation) with { Catalog = "" },
            Fragments, Guid.NewGuid(), "SS - GPT-6-LUNA", temp.Path, new PhaseOptions());

        Assert.True(File.Exists(Path.Combine(temp.Path, PhaseTask.FragmentsFile)));
        Assert.False(File.Exists(Path.Combine(temp.Path, PhaseTask.Proposal1File)));
        Assert.False(File.Exists(Path.Combine(temp.Path, PhaseTask.CatalogFile)));
    }

    [Fact]
    public void A_prompt_over_the_input_budget_is_not_launched()
    {
        using var temp = new TempDirectory();

        var result = PhaseTask.BuildRequest(PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal), Input(Documentation),
            Fragments, Guid.NewGuid(), "SS - GPT-6-SOL", temp.Path, new PhaseOptions { InputTokenBudget = 1_000 });

        Assert.Null(result.Request);
        Assert.Contains("demasiado grande", result.Error);
        Assert.Empty(Directory.GetFileSystemEntries(temp.Path));
    }

    [Fact]
    public void A_valid_output_needs_no_repair_even_with_warnings()
    {
        var phase = PhaseDefinition.For(PhaseKind.Backlog, PhaseRole.Proposal);

        var review = PhaseTask.Review(phase, File.ReadAllText(Example("backlog_propuesta_1.json")), Context(), "PROMPT", 1);

        Assert.True(review.Valid);
        Assert.Null(review.RepairPrompt);
        Assert.Equal(["RF-009 COB: el requisito no está en ninguna historia."], review.Validation.Warnings);
    }

    [Fact]
    public void An_invalid_output_is_repaired_with_the_original_prompt_and_one_error_per_line()
    {
        var phase = PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal);
        var output = File.ReadAllText(Example("catalogo_propuesta_1.json")).Replace("\"DOC-01#F002\"", "\"DOC-01#F099\"");

        var review = PhaseTask.Review(phase, output, Context(), "PROMPT ORIGINAL", 1);

        Assert.False(review.Valid);
        var repair = review.RepairPrompt!;
        Assert.StartsWith("PROMPT ORIGINAL\n", repair);
        Assert.Contains("REPARACIÓN (intento 1)", repair);
        Assert.Contains("`output/catalogo.json`", repair);
        Assert.Contains($"<salida_previa>{output}</salida_previa>", repair);
        Assert.Contains("REF-FRAG: el fragmento DOC-01#F099 no existe.", repair);
        Assert.DoesNotMatch(new Regex(@"\{\{[A-Z_0-9]+\}\}"), repair);
    }

    [Fact]
    public void A_missing_output_is_repaired_too()
    {
        var review = PhaseTask.Review(PhaseDefinition.For(PhaseKind.Questions, PhaseRole.Proposal), null, Context(), "PROMPT", 1);

        Assert.False(review.Valid);
        Assert.StartsWith("output/preguntas.json FICHERO", Assert.Single(review.Validation.Errors));
        Assert.NotNull(review.RepairPrompt);
    }

    private static PhaseContext Context() => new()
    {
        FragmentIds = Fragments.Select(f => f.Id).ToHashSet(),
        AnsweredQuestionIds = Questions().Where(q => q.Status == "respondida").Select(q => q.Id).ToHashSet(),
        Catalog = File.ReadAllText(Example("catalogo_arbitro.json")),
    };
}
