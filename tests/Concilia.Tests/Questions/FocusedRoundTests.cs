using System.Text.Json.Nodes;
using Concilia.Core.Catalog;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;
using Concilia.Core.Projects;
using Concilia.Core.Questions;

namespace Concilia.Tests.Questions;

// Step 112 (prompts v0.13): a round of questions centred on some iterations.
// Its input files, its checks, what it reads besides the new documentation,
// the matching against the questions already asked, and how its revisions
// change the questions of the project.
public class FocusedRoundTests
{
    private static readonly PhaseDefinition Proposal = PhaseDefinition.For(PhaseKind.Questions, PhaseRole.Proposal, focused: true);
    private static readonly PhaseDefinition Arbiter = PhaseDefinition.For(PhaseKind.Questions, PhaseRole.Arbiter, focused: true);

    // DOC-01 is from the first iteration; DOC-02, the new one.
    private static readonly PhaseContext Context = new()
    {
        FragmentIds = new HashSet<string> { "DOC-01#F001", "DOC-01#F002", "DOC-02#F001", "DOC-02#F002" },
        AnsweredQuestionIds = new HashSet<string> { "P-001" },
        RevisableQuestionIds = new HashSet<string> { "P-002", "P-003" },
        NewDocumentIds = new HashSet<string> { "DOC-02" },
        Catalog = """{"requisitos": [{"id": "RF-001"}]}""",
    };

    private const string ValidProposal = """
        {"preguntas": [
           {"id": "P-001", "pregunta": "¿Cuántos recordatorios se envían?", "motivo": "RF de recordatorios", "fuentes": ["DOC-02#F001"]},
           {"id": "P-002", "pregunta": "La respuesta a P-001 decía uno, pero DOC-02 dice dos: ¿cuál vale?", "motivo": "RF de recordatorios",
            "fuentes": ["DOC-02#F002", "DOC-01#F001"], "contradice": {"pregunta": "P-001"}}],
         "revisiones": [
           {"pregunta": "P-002", "accion": "reformular", "texto": "¿Hay límite de duración por reserva de varios días?", "motivo": "DOC-02 añade los varios días", "fuentes": ["DOC-02#F001"]},
           {"pregunta": "P-003", "accion": "resuelta", "motivo": "DOC-02 lo dice", "fuentes": ["DOC-02#F002"]}]}
        """;

    [Fact]
    public void The_centred_round_has_its_own_prompts_and_schemas_with_the_same_output()
    {
        Assert.Equal(("v0/preguntas_centradas.md", "v0/esquemas/preguntas_centradas.schema.json", "output/preguntas.json"),
            (Proposal.PromptResource, Proposal.SchemaResource, Proposal.OutputFile));
        Assert.Equal("v0/arbitro_preguntas_centradas.md", Arbiter.PromptResource);
        Assert.True(Proposal.Focused);
        Assert.False(PhaseDefinition.For(PhaseKind.Questions, PhaseRole.Proposal).Focused);
        Assert.Equal("v0/catalogo.md", PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal, focused: true).PromptResource); // only the questions
    }

    [Fact]
    public void A_and_B_read_the_new_documentation_the_related_fragments_and_requirements_and_consult_the_rest()
    {
        var input = new PhaseInput
        {
            SystemName = "", Language = "es", Documentation = "=== DOC-01 · a ===\n[DOC-01#F001 §1] a\n",
            NewDocumentation = "=== DOC-02 · b ===\n[DOC-02#F001 §1] b\n", RelatedFragments = "=== DOC-01 · a ===\n[DOC-01#F001 §1] a\n",
            Catalog = """{"requisitos": [{"id": "RF-001"}]}""", RelatedRequirements = """{"requisitos": []}""",
        };

        var files = PhaseTask.InputFilesOf(Proposal, input).Select(f => (f.Path, f.Required)).ToList();

        Assert.Equal([
            (FocusedRound.NewDocumentationFile, true), (FocusedRound.RelatedFragmentsFile, true), (DocumentationFile.Path, false),
            (FocusedRound.RelatedRequirementsFile, true), (PhaseTask.CatalogFile, false)], files);
        // Without related fragments or a validated catalog, those files are not there.
        Assert.Equal([FocusedRound.NewDocumentationFile, DocumentationFile.Path],
            PhaseTask.InputFilesOf(Proposal, input with { RelatedFragments = "", Catalog = "" }).Select(f => f.Path));
    }

    [Fact]
    public void The_block_of_the_iterations_says_the_focus_and_the_iteration_of_each_document()
    {
        Assert.Equal("<foco>2, 3</foco>\n<documento id=\"DOC-01\" iteracion=\"1\"/>\n<documento id=\"DOC-02\" iteracion=\"2\"/>",
            FocusedRound.IterationsBlock([2, 3], new Dictionary<string, int> { ["DOC-02"] = 2, ["DOC-01"] = 1 }));
    }

    [Fact]
    public void A_valid_proposal_passes_and_each_rule_has_its_code()
    {
        Assert.True(PhaseValidator.Validate(Proposal, ValidProposal, Context).Valid,
            string.Join("\n", PhaseValidator.Validate(Proposal, ValidProposal, Context).Errors));

        var root = JsonNode.Parse(ValidProposal)!;
        root["preguntas"]![0]!["fuentes"] = new JsonArray("DOC-01#F002");                     // nothing of the new documentation
        root["preguntas"]![1]!["contradice"] = new JsonObject { ["pregunta"] = "P-003" };     // a pending question, not an answer
        root["revisiones"]!.AsArray().Add(new JsonObject { ["pregunta"] = "P-001", ["accion"] = "obsoleta", ["motivo"] = "x", ["fuentes"] = new JsonArray("DOC-02#F001") }); // answered
        root["revisiones"]!.AsArray().Add(new JsonObject { ["pregunta"] = "P-003", ["accion"] = "obsoleta", ["texto"] = "x", ["motivo"] = "x", ["fuentes"] = new JsonArray("DOC-02#F001") });
        var errors = PhaseValidator.Validate(Proposal, root.ToJsonString(), Context).Errors;

        Assert.Contains(errors, e => e.StartsWith("P-001 SIN-NUEVA"));
        Assert.Contains(errors, e => e.StartsWith("P-002 REF-PREG"));
        Assert.Contains(errors, e => e.StartsWith("P-001 REF-PREG: solo se revisan las preguntas pendientes"));
        Assert.Contains(errors, e => e.StartsWith("P-003 REV-DUP"));
        Assert.Contains(errors, e => e.StartsWith("P-003 REV-TEXTO"));

        root = JsonNode.Parse(ValidProposal)!;
        root["preguntas"]![1]!["contradice"] = new JsonObject { ["requisito"] = "RF-099" };
        Assert.Contains(PhaseValidator.Validate(Proposal, root.ToJsonString(), Context).Errors, e => e.StartsWith("P-002 REF-REQ"));
    }

    private const string Proposal2 = """
        {"preguntas": [{"id": "P-001", "pregunta": "¿Cuántos recordatorios?", "motivo": "x", "fuentes": ["DOC-02#F001"]}],
         "revisiones": [{"pregunta": "P-002", "accion": "resuelta", "motivo": "x", "fuentes": ["DOC-02#F001"]}]}
        """;

    private static PhaseContext ArbiterContext => Context with { Proposal1 = ValidProposal, Proposal2 = Proposal2 };

    private static string ArbiterOutput(string revisions) => $$"""
        {"preguntas": [{"id": "P-001", "pregunta": "¿Cuántos recordatorios se envían?", "motivo": "x", "fuentes": ["DOC-02#F001"],
                        "de": {"propuesta_1": ["P-001"], "propuesta_2": ["P-001"]}, "explicacion": "x"},
                       {"id": "P-002", "pregunta": "¿Cuál vale?", "motivo": "x", "fuentes": ["DOC-02#F002"], "contradice": {"pregunta": "P-001"},
                        "de": {"propuesta_1": ["P-002"], "propuesta_2": []}, "explicacion": "x"}],
         "descartadas": [],
         "revisiones": [{{revisions}}]}
        """;

    [Fact]
    public void The_arbiter_decides_each_revised_question_once_with_the_proposals_that_propose_it()
    {
        // P-002: proposal 1 reformulates it and 2 says it is solved; P-003: only proposal 1.
        var valid = ArbiterOutput("""
            {"pregunta": "P-002", "accion": "reformular", "texto": "¿Y para varios días?", "motivo": "x", "fuentes": ["DOC-02#F001"], "de": [1], "discrepancia": true, "explicacion": "x"},
            {"pregunta": "P-003", "accion": "ninguna", "motivo": "", "fuentes": [], "de": [], "discrepancia": false, "explicacion": "x"}
            """);
        Assert.True(PhaseValidator.Validate(Arbiter, valid, ArbiterContext).Valid, string.Join("\n", PhaseValidator.Validate(Arbiter, valid, ArbiterContext).Errors));

        var errors = PhaseValidator.Validate(Arbiter, ArbiterOutput("""
            {"pregunta": "P-002", "accion": "reformular", "texto": "¿Y?", "motivo": "x", "fuentes": ["DOC-02#F001"], "de": [1, 2], "discrepancia": false, "explicacion": "x"}
            """), ArbiterContext).Errors;
        Assert.Contains(errors, e => e.StartsWith("P-002 ARB-REV: la propuesta 2 no propone `reformular`"));
        Assert.Contains(errors, e => e.StartsWith("P-003 ARB-REV: falta la decisión"));
        Assert.Contains(PhaseValidator.Validate(Arbiter, ArbiterOutput("""
            {"pregunta": "P-002", "accion": "ninguna", "motivo": "", "fuentes": [], "de": [2], "discrepancia": false, "explicacion": "x"},
            {"pregunta": "P-003", "accion": "ninguna", "motivo": "", "fuentes": [], "de": [], "discrepancia": false, "explicacion": "x"}
            """), ArbiterContext).Errors, e => e.StartsWith("P-002 ARB-REV: con `ninguna`"));
    }

    [Fact]
    public void The_revisions_are_grouped_by_question_without_AI()
    {
        var questions = new List<FocusQuestion> { new("P-002", "¿Hay límite de duración?", "pendiente"), new("P-003", "¿Se avisa?", "pendiente") };

        Assert.Equal(["P-002", "P-003"], FocusedRound.Targets(ValidProposal, Proposal2));
        Assert.Equal("""
            P-002 «¿Hay límite de duración?»
              1: reformular → «¿Hay límite de duración por reserva de varios días?» · motivo: DOC-02 añade los varios días · fuentes: DOC-02#F001
              2: resuelta · motivo: x · fuentes: DOC-02#F001
            P-003 «¿Se avisa?»
              1: resuelta · motivo: DOC-02 lo dice · fuentes: DOC-02#F002
              2: —

            """.ReplaceLineEndings("\n"), FocusedRound.Revisions(ValidProposal, Proposal2, questions));
    }

    private static Fragment F(string id, string text) => new(id, id.Split('#')[0], Fragmenter.DocumentKind, "§1", text, null);

    // With the Fake embedder, the same words give similarity 1 and none in common, 0.
    [Fact]
    public void The_related_fragments_are_those_like_the_new_documentation_within_the_cap()
    {
        var embedder = new FakeTextEmbedder();
        IReadOnlyList<Fragment> fresh = [F("DOC-02#F001", "recordatorio por correo de la reserva")];
        IReadOnlyList<Fragment> earlier = [F("DOC-01#F001", "factura mensual del cliente"), F("DOC-01#F002", "recordatorio por correo de la reserva"), F("DOC-01#F003", "recordatorio por correo de cada reserva nueva")];

        Assert.Equal(["DOC-01#F002", "DOC-01#F003"], RelatedContext.Fragments(embedder, fresh, earlier).Select(f => f.Id));
        // With a cap that only fits one, the most similar.
        Assert.Equal(["DOC-01#F002"], RelatedContext.Fragments(embedder, fresh, earlier, maxTokens: 20).Select(f => f.Id));

        var catalog = """
            {"requisitos": [
              {"id": "RF-001", "texto": "El sistema deberá emitir la factura mensual.", "fuentes": ["DOC-01#F001"]},
              {"id": "RF-002", "texto": "El sistema deberá permitir anular reservas.", "fuentes": ["DOC-01#F002"]},
              {"id": "RF-003", "texto": "recordatorio por correo de la reserva", "fuentes": ["DOC-01#F009"]}
            ]}
            """;
        var related = JsonNode.Parse(RelatedContext.Requirements(embedder, catalog, fresh, new HashSet<string> { "DOC-01#F002" }))!["requisitos"]!.AsArray();
        Assert.Equal(["RF-002", "RF-003"], related.Select(r => r!["id"]!.GetValue<string>())); // by its sources, by its meaning
    }

    [Fact]
    public void A_new_question_like_one_already_asked_gets_a_line_previa()
    {
        var matching = new ArbiterMatching(new FakeTextEmbedder(), new MatchThresholds(Same: 0.9, Similar: 0.7));
        var previous = matching.AgainstPrevious(Proposal2, """{"preguntas": [{"id": "P-001", "pregunta": "¿Qué color tiene el logotipo?"}]}""",
            [new FocusQuestion("P-007", "¿Cuántos recordatorios?", "pendiente")]);

        Assert.Equal("1:P-001 2:P-001 mismo 1.00\n1:P-001 previa:P-007 mismo 1.00\n",
            MatchingFile.WithPrevious("1:P-001 2:P-001 mismo 1.00\n", previous));
    }

    private static readonly PhasePlan Plan = new(PhaseKind.Questions, new PhaseInput { SystemName = "", Language = "" }, [], [], ["B", "A"])
    {
        Focus = new FocusPlan([2], ["DOC-02"], []),
    };

    private static Question Pending(string code, string text) => new()
    {
        Code = code, Text = text, Reason = "x", Origin = "ambas", Status = QuestionStatus.Pending,
        SuggestedAnswer = "Una sugerencia", SuggestionBasis = "documentacion",
    };

    [Fact]
    public void A_reformulation_changes_the_text_and_keeps_the_earlier_one_and_solved_or_obsolete_are_proposals()
    {
        var reformulated = Pending("P-002", "¿Hay límite de duración?");
        var solved = Pending("P-003", "¿Se avisa?");
        var answered = Pending("P-004", "¿Quién anula?");
        answered.Status = QuestionStatus.Answered;
        var output = """
            {"preguntas": [], "descartadas": [], "revisiones": [
              {"pregunta": "P-002", "accion": "reformular", "texto": "¿Y para varios días?", "motivo": "DOC-02 añade los varios días", "fuentes": ["DOC-02#F001"], "de": [1, 2], "discrepancia": false, "explicacion": "x"},
              {"pregunta": "P-003", "accion": "resuelta", "motivo": "DOC-02 lo dice", "fuentes": ["DOC-02#F002"], "de": [2], "discrepancia": true, "explicacion": "y"},
              {"pregunta": "P-004", "accion": "obsoleta", "motivo": "z", "fuentes": ["DOC-02#F002"], "de": [1], "discrepancia": false, "explicacion": "z"}]}
            """;
        var job = Guid.NewGuid();

        QuestionRules.ApplyRevisions([reformulated, solved, answered], new PhaseJobResult("ARB", true, output, ""), Plan, job, DateTimeOffset.UnixEpoch);

        Assert.Equal("¿Y para varios días?", reformulated.Text);
        Assert.Null(reformulated.SuggestedAnswer); // it was for the earlier text
        var change = reformulated.History.Single();
        Assert.Equal(("reformulada", "¿Hay límite de duración?", "ambas", job), (change.Action, change.PreviousText, change.Origin, change.JobId));
        Assert.Equal([2], change.Iterations);

        Assert.Equal("¿Se avisa?", solved.Text);
        Assert.Equal(("resuelta", "DOC-02 lo dice", QuestionStatus.Pending), (solved.ProposedAction, solved.ProposedReason, solved.Status)); // a proposal
        Assert.Equal(("propuesta-resuelta", "A", true), (solved.History.Single().Action, solved.History.Single().Origin, solved.History.Single().Discrepancy)); // proposal 2 is A

        Assert.Null(answered.ProposedAction); // answered meanwhile: left alone
        Assert.Empty(answered.History);
    }

    [Fact]
    public void A_new_question_keeps_what_it_contradicts()
    {
        var output = """
            {"preguntas": [
              {"id": "P-001", "pregunta": "¿Cuál vale?", "motivo": "x", "fuentes": ["DOC-02#F001"], "contradice": {"pregunta": "P-004"}, "de": {"propuesta_1": ["P-001"], "propuesta_2": []}, "explicacion": "x"},
              {"id": "P-002", "pregunta": "¿Y el requisito?", "motivo": "x", "fuentes": ["DOC-02#F001"], "contradice": {"requisito": "RF-012"}, "de": {"propuesta_1": [], "propuesta_2": ["P-001"]}, "explicacion": "x"}],
             "descartadas": [], "revisiones": []}
            """;

        var questions = QuestionRules.FromRound(new PhaseJobResult("ARB", true, output, ""), Plan, 1, Guid.NewGuid(), ["P-007"], DateTimeOffset.UnixEpoch);

        Assert.Equal(("P-008", "P-004", null), (questions[0].Code, questions[0].ContradictsQuestion, questions[0].ContradictsRequirement));
        Assert.Equal(("P-009", null, "RF-012"), (questions[1].Code, questions[1].ContradictsQuestion, questions[1].ContradictsRequirement));
    }

    [Fact]
    public void The_focus_is_one_or_more_closed_iterations_with_documents()
    {
        Iteration I(int n, IterationStatus status) => new() { Number = n, Name = $"Iteración {n}", Status = status };
        var all = new List<Iteration> { I(1, IterationStatus.Closed), I(2, IterationStatus.Closed), I(3, IterationStatus.Open) };
        var withDocuments = new HashSet<int> { 1, 3 };

        Assert.Null(IterationRules.ValidateFocus([1], all, withDocuments));
        Assert.Equal("Elige al menos una iteración en la que centrarse.", IterationRules.ValidateFocus([], all, withDocuments));
        Assert.Equal("La iteración 2 no tiene documentos con texto.", IterationRules.ValidateFocus([1, 2], all, withDocuments));
        Assert.Equal("Cierra la documentación de la iteración 3.", IterationRules.ValidateFocus([3], all, withDocuments));
        Assert.Equal("La iteración 9 no existe.", IterationRules.ValidateFocus([9], all, withDocuments));
    }
}
