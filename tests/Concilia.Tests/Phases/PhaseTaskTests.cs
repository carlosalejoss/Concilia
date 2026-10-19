using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Concilia.Core.Catalog;
using Concilia.Core.Phases;
using Concilia.Core.Runner;
using static Concilia.Tests.Runner.FakeClaudeLauncher;

namespace Concilia.Tests.Phases;

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

    [Fact]
    public void A_proposal_goes_with_one_element_per_line_and_no_line_is_too_long()
    {
        var proposal = File.ReadAllText(Example("catalogo_propuesta_1.json"));
        var requirements = JsonNode.Parse(proposal)!["requisitos"]!.AsArray().Count;

        var text = ReadingPlan.OnePerLine(proposal);

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(proposal), JsonNode.Parse(text)));
        Assert.True(text.Length < ReadingPlan.Indented(proposal).Length);
        Assert.Equal(requirements, text.Split('\n').Count(l => l.StartsWith("{\"id\":\"", StringComparison.Ordinal)));
        Assert.Equal("no es JSON", ReadingPlan.OnePerLine("no es JSON"));

        // What does not fit in a line opens one more level.
        var big = new JsonObject { ["a"] = new JsonArray(Enumerable.Range(0, 3).Select(_ => (JsonNode)new string('x', 1_000)).ToArray()) };
        var lines = ReadingPlan.OnePerLine(big.ToJsonString()).Split('\n');
        Assert.All(lines, l => Assert.True(l.Length <= ReadingPlan.MaxLineChars));
        Assert.True(JsonNode.DeepEquals(big, JsonNode.Parse(string.Join("\n", lines))));
    }

    [Fact]
    public void Documentation_to_consult_says_where_each_document_starts()
    {
        var documentation = "=== DOC-01 · a.txt ===\n[DOC-01#F001] uno\n=== DOC-02 · b.xlsx ===\n[DOC-02#F001] dos\n";

        var block = ReadingPlan.Block([new InputFile(DocumentationFile.Path, documentation, Required: false)]);

        Assert.Equal("- `input/documentacion.txt` (4 líneas), solo para consultar lo que necesites: no lo leas entero. "
                     + "Dónde empieza cada documento: DOC-01 en la línea 1; DOC-02 en la línea 3.", block);
    }

    // A real run cited the line 830 of the documentation as DOC-06#F830.
    // 15-10-2026: SOL cited "DOC-17#F003 sigue" (the mark of a continuation
    // line) and the catalog went to a repair. The platform removes the mark.
    [Fact]
    public void The_sigue_mark_of_a_cited_fragment_is_removed_before_checking()
    {
        var phase = PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal);
        var output = File.ReadAllText(Example("catalogo_propuesta_1.json")).Replace("\"DOC-01#F002\"", "\"DOC-01#F002 sigue\"");
        Assert.Contains("DOC-01#F002 sigue", output);

        Assert.True(PhaseTask.Review(phase, output, Context(), "PROMPT", 1).Valid);
        var cleaned = FragmentCitations.Clean(output);
        Assert.DoesNotContain("sigue\"", cleaned);
        Assert.Contains("\"DOC-01#F002\"", cleaned);
    }

    // 16-10-2026: LUNA, as the arbiter of a catalog, wrote "propuesta_2" twice
    // in a `de`. JsonNode throws on a repeated key, and the review ended with an
    // internal error; then the repair of the whole catalog did not fit in the
    // context. Two lists are joined by the platform; anything else asks for a repair.
    [Fact]
    public void Repeated_keys_with_lists_are_joined()
    {
        const string output = """{"requisitos":[{"id":"RF-001","de":{"propuesta_2":["2:RF-001"],"propuesta_1":["1:RF-004"],"propuesta_2":["2:RF-002","2:RF-001"]}}]}""";

        var de = JsonNode.Parse(JsonDuplicates.Merge(output)!)!["requisitos"]![0]!["de"]!;

        Assert.Equal(["2:RF-001", "2:RF-002"], de["propuesta_2"]!.AsArray().Select(x => x!.GetValue<string>()));
        Assert.Equal(["1:RF-004"], de["propuesta_1"]!.AsArray().Select(x => x!.GetValue<string>()));
        Assert.Equal(["requisitos[0].de: propuesta_2"], JsonDuplicates.Find(output));
        const string plain = """{"a":[1]}""";
        Assert.Same(plain, JsonDuplicates.Merge(plain)); // nothing repeated: the same text
    }

    [Fact]
    public void An_arbiter_output_with_a_repeated_list_is_accepted_with_a_warning()
    {
        var phase = PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Arbiter);
        // The first `de` of the example, with its propuesta_2 split in two.
        var output = File.ReadAllText(Example("catalogo_arbitro.json"))
            .Replace("\"propuesta_2\":[\"RF-001\"]}", "\"propuesta_2\":[],\"propuesta_2\":[\"RF-001\"]}");
        Assert.Single(JsonDuplicates.Find(output));
        var context = Context() with
        {
            Proposal1 = File.ReadAllText(Example("catalogo_propuesta_1.json")),
            Proposal2 = File.ReadAllText(Example("catalogo_propuesta_2.json")),
        };

        var review = PhaseTask.Review(phase, output, context, "PROMPT", 1);

        Assert.True(review.Valid, string.Join(" | ", review.Validation.Errors));
        Assert.Contains(review.Validation.Warnings, w => w.Contains("CLAVE-DUP: clave repetida unida por la plataforma"));
        Assert.Empty(JsonDuplicates.Find(PhaseTask.Clean(phase, output, context)!)); // what is kept has it joined
    }

    [Theory]
    [InlineData(PhaseRole.Proposal)]
    [InlineData(PhaseRole.Arbiter)]
    public void A_repeated_key_that_is_not_a_list_asks_for_a_repair_instead_of_failing(PhaseRole role)
    {
        var phase = PhaseDefinition.For(PhaseKind.Catalog, role);
        const string output = """{"requisitos":[{"id":"RF-001","texto":"Uno","texto":"Otro"}]}""";

        var review = PhaseTask.Review(phase, output, Context(), "PROMPT", 1);

        Assert.False(review.Valid);
        var error = Assert.Single(review.Validation.Errors);
        Assert.Contains("CLAVE-DUP", error);
        Assert.Contains("requisitos[0]: texto", error);
        Assert.NotNull(review.RepairPrompt);
    }

    [Fact]
    public void Only_a_whole_value_with_the_mark_changes()
    {
        const string output = """{"fuentes":["DOC-01#F002 sigue","DOC-01#F003"],"texto":"El texto sigue igual: DOC-01#F002 sigue"}""";

        var cleaned = JsonNode.Parse(FragmentCitations.Clean(output))!;

        Assert.Equal(["DOC-01#F002", "DOC-01#F003"], cleaned["fuentes"]!.AsArray().Select(f => f!.GetValue<string>()));
        Assert.Equal("El texto sigue igual: DOC-01#F002 sigue", cleaned["texto"]!.GetValue<string>());
        var plain = output.Replace(" sigue\",", "\",");
        Assert.Same(plain, FragmentCitations.Clean(plain)); // nothing to clean: the same text
        Assert.Equal("no es JSON", FragmentCitations.Clean("no es JSON"));
    }

    // Step 89: a line number of the same document is fixed by the platform,
    // with a warning; with another document it is still an error, which says
    // which fragment is on that line.
    [Theory]
    [InlineData("DOC-01", true)]
    [InlineData("DOC-09", false)]
    public void A_fragment_that_is_a_line_number_is_fixed_when_the_line_is_of_its_document(string document, bool fixedByPlatform)
    {
        var phase = PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal);
        var documentation = DocumentationFile.Build(Fragments, new Dictionary<string, string>());
        var lines = DocumentationFile.FragmentAtLine(documentation);
        var line = lines.Keys.Max();
        var cited = $"{document}#F{line:000}";
        Assert.DoesNotContain(cited, Fragments.Select(f => f.Id)); // a line number, not a fragment
        var output = File.ReadAllText(Example("catalogo_propuesta_1.json")).Replace("\"DOC-01#F002\"", $"\"{cited}\"");
        var context = Context() with { FragmentAtLine = lines };

        var review = PhaseTask.Review(phase, output, context, "PROMPT", 1);

        if (fixedByPlatform)
        {
            Assert.True(review.Valid, string.Join(" | ", review.Validation.Errors));
            Assert.Contains(review.Validation.Warnings, w => w.Contains($"REF-LINEA: {cited} no existe") && w.Contains(lines[line]));
            Assert.Contains($"\"{lines[line]}\"", PhaseTask.Clean(phase, output, context)); // what is kept has the fragment
        }
        else
            Assert.Contains(review.Validation.Errors, e => e.Contains($"REF-FRAG: el fragmento {cited} no existe. La línea {line} de input/documentacion.txt es {lines[line]}:"));
    }

    // Step 89: an id that does not match its type (RF-009 of type RNF) is
    // renumbered by type, with a warning; the type is taken as right.
    [Fact]
    public void A_requirement_whose_id_does_not_match_its_type_is_renumbered()
    {
        var phase = PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal);
        var catalog = JsonNode.Parse(File.ReadAllText(Example("catalogo_propuesta_1.json")))!;
        var wrong = catalog["requisitos"]!.AsArray().Single(r => r!["id"]!.GetValue<string>() == "RF-009")!;
        wrong["tipo"] = "RNF";
        var output = catalog.ToJsonString();

        var review = PhaseTask.Review(phase, output, Context(), "PROMPT", 1);

        Assert.True(review.Valid, string.Join(" | ", review.Validation.Errors));
        Assert.Contains(review.Validation.Warnings, w => w.Contains("TIPO") && w.Contains("RF-009"));
        var kept = JsonNode.Parse(PhaseTask.Clean(phase, output, Context())!)!["requisitos"]!.AsArray();
        Assert.Equal(["RF-008", "RNF-001", "RNF-002", "RT-001"], kept.Skip(7).Select(r => r!["id"]!.GetValue<string>()));
    }

    // Step 89: an element the arbiter left out of every `de` and of the
    // discards is added to the element where its pair "mismo" of the
    // matching is; without that pair it is still an error.
    [Theory]
    [InlineData("1:RF-004 2:RF-004 mismo 0.95", true)]
    [InlineData("1:RF-004 2:RF-004 parecido 0.80", false)]
    [InlineData("", false)]
    public void An_element_left_out_by_the_arbiter_joins_its_pair_mismo(string matchingLine, bool fixedByPlatform)
    {
        var phase = PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Arbiter);
        var arbiter = JsonNode.Parse(File.ReadAllText(Example("catalogo_arbitro.json")))!;
        var element = arbiter["requisitos"]!.AsArray().Single(r => r!["id"]!.GetValue<string>() == "RF-004")!;
        element["de"]!["propuesta_1"] = new JsonArray(); // proposal 1's RF-004 is left out
        var output = arbiter.ToJsonString();
        var context = Context() with
        {
            Proposal1 = File.ReadAllText(Example("catalogo_propuesta_1.json")),
            Proposal2 = File.ReadAllText(Example("catalogo_propuesta_2.json")),
            Matching = matchingLine.Length > 0 ? matchingLine + "\n" : null,
        };

        var review = PhaseTask.Review(phase, output, context, "PROMPT", 1);

        if (fixedByPlatform)
        {
            Assert.True(review.Valid, string.Join(" | ", review.Validation.Errors));
            Assert.Contains(review.Validation.Warnings, w => w.StartsWith("1:RF-004 ARB-FALTA") && w.Contains("RF-004"));
            var kept = JsonNode.Parse(ProposalIds.Strip(PhaseTask.Clean(phase, output, context)!))!;
            Assert.Equal(["RF-004"], kept["requisitos"]!.AsArray().Single(r => r!["id"]!.GetValue<string>() == "RF-004")!["de"]!["propuesta_1"]!
                .AsArray().Select(i => i!.GetValue<string>()));
        }
        else
            Assert.Contains(review.Validation.Errors, e => e.StartsWith("RF-004 ARB-FALTA"));
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

    // Prompts v0.3: the arbiter sees "1:" and "2:" before the ids it compares,
    // and the platform removes them from its output before checking it.
    [Fact]
    public void The_arbiter_sees_the_ids_with_the_number_of_their_proposal()
    {
        var proposal = File.ReadAllText(Example("catalogo_propuesta_1.json"));

        var prefixed = JsonNode.Parse(ProposalIds.Prefix(PhaseKind.Catalog, proposal, 2))!;

        var requirements = prefixed["requisitos"]!.AsArray();
        Assert.All(requirements, r => Assert.StartsWith("2:R", r!["id"]!.GetValue<string>()));
        // The answers a requirement cites point elsewhere: they keep their form.
        Assert.All(requirements.SelectMany(r => r!["preguntas"]!.AsArray()), q => Assert.StartsWith("P-", q!.GetValue<string>()));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(proposal), JsonNode.Parse(ProposalIds.Unprefix(PhaseKind.Catalog, prefixed.ToJsonString()))));
    }

    [Fact]
    public void In_a_backlog_only_the_stories_get_the_prefix()
    {
        var prefixed = JsonNode.Parse(ProposalIds.Prefix(PhaseKind.Backlog, File.ReadAllText(Example("backlog_propuesta_1.json")), 1))!;

        var epic = prefixed["epicas"]![0]!;
        Assert.StartsWith("EP-", epic["id"]!.GetValue<string>());
        Assert.StartsWith("1:HU-", epic["historias"]![0]!["id"]!.GetValue<string>());
        Assert.StartsWith("CA-", epic["historias"]![0]!["criterios"]![0]!["id"]!.GetValue<string>());
    }

    [Fact]
    public void The_prefix_of_its_own_proposal_is_removed_and_a_wrong_one_is_reported()
    {
        var phase = PhaseDefinition.For(PhaseKind.Questions, PhaseRole.Arbiter);
        var output = File.ReadAllText(Example("preguntas_arbitro.json"));
        var withPrefixes = JsonNode.Parse(output)!;
        foreach (var q in withPrefixes["preguntas"]!.AsArray())
            for (var n = 1; n <= 2; n++)
            {
                var ids = q!["de"]![$"propuesta_{n}"]!.AsArray();
                for (var i = 0; i < ids.Count; i++)
                    ids[i] = $"{n}:{ids[i]!.GetValue<string>()}";
            }
        foreach (var d in withPrefixes["descartadas"]!.AsArray())
            d!["id"] = $"{d["propuesta"]!.GetValue<int>()}:{d["id"]!.GetValue<string>()}";
        var context = Context() with
        {
            Proposal1 = File.ReadAllText(Example("preguntas_propuesta_1.json")),
            Proposal2 = File.ReadAllText(Example("preguntas_propuesta_2.json")),
        };

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(output), JsonNode.Parse(ProposalIds.Strip(withPrefixes.ToJsonString()))));
        Assert.True(PhaseTask.Review(phase, withPrefixes.ToJsonString(), context, "PROMPT", 1).Valid);

        // An id of proposal 1 cited in propuesta_2 keeps its "1:", so it does not exist there.
        var mixed = withPrefixes.DeepClone();
        var first = mixed["preguntas"]![0]!["de"]!;
        var moved = first["propuesta_1"]![0]!.GetValue<string>();
        first["propuesta_2"]!.AsArray().Add(moved);
        var review = PhaseTask.Review(phase, mixed.ToJsonString(), context, "PROMPT", 1);
        Assert.Equal($"P-001 ARB-DE: {moved} está en `de.propuesta_2`, pero es un id de la propuesta 1.", review.Validation.Errors[0]);
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
        Assert.Contains("REF-FRAG: el fragmento DOC-01#F099 no existe.", repair);
        Assert.DoesNotMatch(new Regex(@"\{\{[A-Z_0-9]+\}\}"), repair);

        // Step 90: the previous output is not pasted; it stays in its file,
        // one element per line, and the error says on which line its element is.
        Assert.DoesNotContain(output, repair);
        var file = review.RepairFile!.Split('\n');
        var line = Array.FindIndex(file, l => l.Contains("DOC-01#F099")) + 1;
        Assert.Contains($"REF-FRAG: el fragmento DOC-01#F099 no existe. (línea {line})", repair);
        Assert.StartsWith("{\"id\":\"RF-", file[line - 1]);
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
