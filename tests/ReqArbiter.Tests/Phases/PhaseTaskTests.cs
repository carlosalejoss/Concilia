using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Phases;
using ReqArbiter.Core.Runner;
using static ReqArbiter.Tests.Runner.FakeClaudeLauncher;

namespace ReqArbiter.Tests.Phases;

// The generic task of a v0 phase with ReservaSalas: the prompt of each phase is
// filled in one pass with the small inputs and the reading plan, the big ones
// go to input/ for the model to read by parts, the run is refused when the
// prompt does not fit in InputTokenBudget, the lines left unread are errors,
// and an invalid output leads to one repair with the list of errors.
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

    private static string Documentation => DocumentationFile.Build(Fragments, new Dictionary<string, string> { ["DOC-01"] = "DOC-01.txt" });

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
    public void The_documentation_goes_to_a_file_with_one_fragment_per_line()
    {
        using var temp = new TempDirectory();

        var result = PhaseTask.BuildRequest(PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal), Input(Documentation),
            Fragments, Guid.NewGuid(), "SS - GPT-6-SOL", temp.Path, new PhaseOptions());

        var lines = File.ReadAllLines(Path.Combine(temp.Path, DocumentationFile.Path));
        Assert.Equal("=== DOC-01 · DOC-01.txt ===", lines[0]);
        Assert.Equal(Fragments.Select(f => f.Id), lines.Skip(1).Select(l => l[1..l.IndexOfAny([' ', ']'])]));
        // The prompt has the reading plan, not the documentation.
        Assert.Contains($"- `input/documentacion.txt` ({lines.Length} líneas), entero, con estas llamadas a Read: `offset` 1, `limit` {lines.Length}.", result.Request!.Prompt);
        Assert.DoesNotContain(Fragments[1].Text, result.Request.Prompt);
        Assert.Equal([DocumentationFile.Path], result.RequiredReads);
        Assert.Equal(["Read", "Write"], result.Request.AllowedTools);
        Assert.Equal(30, result.Request.MaxTurns);
        Assert.False(result.Request.InputFilesInBudget);
    }

    [Fact]
    public void A_fragment_too_long_for_one_line_goes_on_in_sigue_lines()
    {
        var text = string.Join(" ", Enumerable.Repeat("palabra", 500)); // 3,999 characters
        var file = DocumentationFile.Build([new Fragment("DOC-01#F001", "DOC-01", Fragmenter.DocumentKind, "§1", text, null)],
                                           new Dictionary<string, string>());

        var lines = file.TrimEnd('\n').Split('\n');
        Assert.Equal("=== DOC-01 · DOC-01 ===", lines[0]);
        Assert.StartsWith("[DOC-01#F001 §1] palabra", lines[1]);
        Assert.All(lines.Skip(2), l => Assert.StartsWith("[DOC-01#F001 sigue] palabra", l));
        Assert.All(lines, l => Assert.True(l.Length < 2_000, $"{l.Length} caracteres"));
        Assert.Equal(text, string.Join(" ", lines.Skip(1).Select(l => l[(l.IndexOf(']') + 2)..])));
    }

    [Fact]
    public void The_reading_plan_covers_every_line_in_ranges_that_fit_in_one_read()
    {
        var content = string.Concat(Enumerable.Range(1, 3_000).Select(i => $"[DOC-01#F{i:0000}] {new string('x', 100)}\n"));

        var ranges = ReadingPlan.Ranges(content);

        Assert.Equal(1, ranges[0].From);
        Assert.Equal(3_000, ranges[^1].To);
        Assert.All(ranges.Zip(ranges.Skip(1)), pair => Assert.Equal(pair.First.To + 1, pair.Second.From));
        var lines = content.Split('\n');
        Assert.All(ranges, r => Assert.True(lines[(r.From - 1)..r.To].Sum(l => l.Length + 1) <= ReadingPlan.MaxCharsPerRead));
        // In the prompt, as the arguments of each call.
        Assert.StartsWith($"`offset` 1, `limit` {ranges[0].To}; `offset` {ranges[1].From}, `limit` {ranges[1].To - ranges[1].From + 1}", ReadingPlan.Calls(content));
    }

    [Fact]
    public void A_json_input_is_indented_so_no_line_is_cut()
    {
        var oneLine = File.ReadAllText(Example("catalogo_propuesta_1.json")).ReplaceLineEndings("");

        var indented = ReadingPlan.Indented(oneLine);

        Assert.True(indented.Split('\n').Length > 10);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(oneLine), JsonNode.Parse(indented)));
        Assert.Equal("no es JSON", ReadingPlan.Indented("no es JSON"));
    }

    // A and B read their input entirely; the arbiter, the two proposals (and
    // the catalog in the backlog), and may only consult the documentation.
    [Theory]
    [InlineData(PhaseKind.Questions, PhaseRole.Proposal, "input/documentacion.txt", "")]
    [InlineData(PhaseKind.Catalog, PhaseRole.Arbiter, "input/propuesta_1.json input/propuesta_2.json", "input/documentacion.txt")]
    [InlineData(PhaseKind.Backlog, PhaseRole.Proposal, "input/catalogo.json", "")]
    [InlineData(PhaseKind.Backlog, PhaseRole.Arbiter, "input/catalogo.json input/propuesta_1.json input/propuesta_2.json", "")]
    [InlineData(PhaseKind.Suggestions, PhaseRole.Proposal, "input/documentacion.txt", "")]
    public void Each_role_reads_its_files(PhaseKind kind, PhaseRole role, string required, string optional)
    {
        var files = PhaseTask.InputFilesOf(PhaseDefinition.For(kind, role), Input(Documentation));

        Assert.Equal(required, string.Join(" ", files.Where(f => f.Required).Select(f => f.Path)));
        Assert.Equal(optional, string.Join(" ", files.Where(f => !f.Required).Select(f => f.Path)));
    }

    [Fact]
    public void Lines_left_unread_are_an_error_that_asks_for_the_repair()
    {
        using var temp = new TempDirectory();
        var phase = PhaseDefinition.For(PhaseKind.Questions, PhaseRole.Proposal);
        var request = PhaseTask.BuildRequest(phase, Input(Documentation), Fragments, Guid.NewGuid(), "m", temp.Path, new PhaseOptions());
        var total = File.ReadAllLines(Path.Combine(temp.Path, DocumentationFile.Path)).Length;
        FileRead[] partial = [new(DocumentationFile.Path, 1, 5, true)];

        var unread = PhaseTask.UnreadErrors(temp.Path, request.RequiredReads!, partial);
        var review = PhaseTask.Review(phase, File.ReadAllText(Example("preguntas_propuesta_1.json")), Context(), "PROMPT", 1, unread);

        Assert.Equal([$"input/documentacion.txt LECTURA: no has leído las líneas 6–{total} (de {total}). Léelas con Read (offset y limit) y corrige la salida si cambia algo."], unread);
        Assert.False(review.Valid);
        Assert.Contains("LECTURA: no has leído las líneas 6–", review.RepairPrompt);
        Assert.Empty(PhaseTask.UnreadErrors(temp.Path, request.RequiredReads!, [new(DocumentationFile.Path, 1, total, true)]));
    }

    [Fact]
    public void A_document_with_placeholders_reaches_the_model_as_it_is()
    {
        using var temp = new TempDirectory();
        var documentation = "[DOC-01#F001 §1] Plantilla: {{NOMBRE_SISTEMA}} y {{IDIOMA}}\n";

        var result = PhaseTask.BuildRequest(PhaseDefinition.For(PhaseKind.Questions, PhaseRole.Proposal), Input(documentation),
            Fragments, Guid.NewGuid(), "m", temp.Path, new PhaseOptions());

        Assert.Equal(documentation, File.ReadAllText(Path.Combine(temp.Path, DocumentationFile.Path)));
        Assert.DoesNotContain("Plantilla", result.Request!.Prompt);
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
        Assert.Equal(["Read", "Write"], request.AllowedTools);
        foreach (var file in new[] { PhaseTask.FragmentsFile, DocumentationFile.Path, PhaseTask.Proposal1File, PhaseTask.Proposal2File })
            Assert.True(File.Exists(Path.Combine(temp.Path, file)), file);
        Assert.Equal([PhaseTask.Proposal1File, PhaseTask.Proposal2File], result.RequiredReads);

        var context = PhaseTask.LoadContext(temp.Path, new HashSet<string> { "P-001" });
        Assert.Equal(Fragments.Select(f => f.Id).ToHashSet(), context.FragmentIds);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Example("catalogo_propuesta_1.json"))), JsonNode.Parse(context.Proposal1!)));
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
