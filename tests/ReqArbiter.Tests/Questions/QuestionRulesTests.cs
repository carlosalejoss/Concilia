using ReqArbiter.Core.Flow;
using ReqArbiter.Core.Phases;
using ReqArbiter.Core.Questions;

namespace ReqArbiter.Tests.Questions;

// Steps 46 and 47: a round becomes questions of the project (renumbered, with
// their origin and the arbiter's note), the next round sees all of them, and
// each question can be answered, edited, discarded and reopened.
public class QuestionRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    private static string Example(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts-v0", "ejemplo", file));

    // The arbiter saw B as "Propuesta 1" and A as "Propuesta 2".
    private static readonly PhasePlan Plan = new(PhaseKind.Questions, new PhaseInput { SystemName = "", Language = "" }, [], [], ["B", "A"]);

    private static Question Pending(string code = "P-001") => new()
    {
        Code = code, Text = "¿Cuánto es rápido?", Reason = "Rendimiento", Origin = "ambas", Status = QuestionStatus.Pending,
    };

    [Fact]
    public void The_arbiter_questions_are_renumbered_after_the_existing_ones()
    {
        var jobId = Guid.NewGuid();

        var questions = QuestionRules.FromRound(new PhaseJobResult("ARB", true, Example("preguntas_arbitro.json"), ""), Plan, 3, jobId, ["P-001", "P-007"], Now);

        Assert.Equal(["P-008", "P-009", "P-010", "P-011", "P-012"], questions.Select(q => q.Code));
        Assert.All(questions, q =>
        {
            Assert.Equal(QuestionStatus.Pending, q.Status);
            Assert.Equal(3, q.ProjectId);
            Assert.Equal(jobId, q.JobId);
            Assert.NotEmpty(q.Sources);
            Assert.False(string.IsNullOrEmpty(q.ArbiterNote));
        });
    }

    [Fact]
    public void The_origin_comes_from_the_arbiter_de_in_the_order_of_the_plan()
    {
        var questions = QuestionRules.FromRound(new PhaseJobResult("ARB", true, Example("preguntas_arbitro.json"), ""), Plan, 1, Guid.NewGuid(), [], Now);

        // P-001 is in both proposals; P-002 only in proposal 1, which was B.
        Assert.Equal("ambas", questions[0].Origin);
        Assert.Equal("B", questions[1].Origin);
        Assert.Equal("P-001", questions[0].Code);
    }

    [Fact]
    public void Without_cross_check_the_origin_is_the_only_model_that_ended_well()
    {
        var questions = QuestionRules.FromRound(new PhaseJobResult("A", false, Example("preguntas_propuesta_1.json"), ""), Plan, 1, Guid.NewGuid(), [], Now);

        Assert.NotEmpty(questions);
        Assert.All(questions, q => Assert.Equal("A", q.Origin));
        Assert.Contains("Sin verificación cruzada", questions[0].ArbiterNote);
    }

    [Fact]
    public void A_failed_round_adds_no_question()
    {
        Assert.Empty(QuestionRules.FromRound(new PhaseJobResult(null, false, null, "falló"), Plan, 1, Guid.NewGuid(), [], Now));
    }

    [Fact]
    public void The_next_round_sees_every_previous_question_with_its_status()
    {
        var answered = Pending();
        QuestionRules.Answer(answered, "Menos de 2 segundos.", Now);
        var discarded = Pending("P-002");
        QuestionRules.Discard(discarded, null, Now);
        var pending = Pending("P-003");

        var block = PhasePrompt.PreviousQuestionsBlock(new[] { answered, discarded, pending }.Select(QuestionRules.ForPrompt));

        Assert.Contains("<pregunta id=\"P-001\" estado=\"respondida\"><texto>¿Cuánto es rápido?</texto><respuesta>Menos de 2 segundos.</respuesta></pregunta>", block);
        Assert.Contains("<pregunta id=\"P-002\" estado=\"descartada\">", block);
        Assert.Contains("<pregunta id=\"P-003\" estado=\"pendiente\">", block);
    }

    [Fact]
    public void Answering_a_pending_question_changes_the_answers()
    {
        var q = Pending();

        var change = QuestionRules.Answer(q, "  Menos de 2 segundos.  ", Now);

        Assert.Null(change.Error);
        Assert.Equal(FlowChange.AnswerChanged, change.Change);
        Assert.Equal(QuestionStatus.Answered, q.Status);
        Assert.Equal("Menos de 2 segundos.", q.Answer);
        Assert.Equal(Now, q.UpdatedAt);
    }

    [Fact]
    public void An_answer_can_be_edited()
    {
        var q = Pending();
        QuestionRules.Answer(q, "Menos de 2 segundos.", Now);

        var change = QuestionRules.Answer(q, "Menos de 1 segundo.", Now);

        Assert.Null(change.Error);
        Assert.Equal("Menos de 1 segundo.", q.Answer);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_answer_is_refused(string? answer)
    {
        var q = Pending();

        Assert.Equal("La respuesta no puede estar vacía.", QuestionRules.Answer(q, answer, Now).Error);
        Assert.Equal(QuestionStatus.Pending, q.Status);
    }

    [Fact]
    public void A_discarded_question_must_be_reopened_before_answering_it()
    {
        var q = Pending();
        QuestionRules.Discard(q, "Fuera de alcance", Now);

        Assert.NotNull(QuestionRules.Answer(q, "Sí", Now).Error);
        Assert.Equal(QuestionStatus.Discarded, q.Status);
    }

    [Fact]
    public void Discarding_keeps_the_optional_reason_and_drops_the_answer()
    {
        var withReason = Pending();
        QuestionRules.Answer(withReason, "Sí", Now);
        var change = QuestionRules.Discard(withReason, " Fuera de alcance ", Now);
        var withoutReason = Pending();
        QuestionRules.Discard(withoutReason, "  ", Now);

        Assert.Equal(FlowChange.QuestionDiscarded, change.Change);
        Assert.Equal(QuestionStatus.Discarded, withReason.Status);
        Assert.Null(withReason.Answer);
        Assert.Equal("Fuera de alcance", withReason.DiscardReason);
        Assert.Null(withoutReason.DiscardReason);
        Assert.NotNull(QuestionRules.Discard(withoutReason, null, Now).Error);
    }

    [Fact]
    public void Reopening_takes_the_question_back_to_pending()
    {
        var answered = Pending();
        QuestionRules.Answer(answered, "Sí", Now);
        var discarded = Pending();
        QuestionRules.Discard(discarded, "No hace falta", Now);

        var fromAnswered = QuestionRules.Reopen(answered, Now);
        var fromDiscarded = QuestionRules.Reopen(discarded, Now);

        Assert.Equal(FlowChange.AnswerChanged, fromAnswered.Change); // an answer disappears
        Assert.Null(fromDiscarded.Change);
        Assert.All(new[] { answered, discarded }, q =>
        {
            Assert.Equal(QuestionStatus.Pending, q.Status);
            Assert.Null(q.Answer);
            Assert.Null(q.DiscardReason);
        });
        Assert.NotNull(QuestionRules.Reopen(answered, Now).Error);
    }

    // AI answer suggestions (09-10-2026): they are kept on the pending
    // questions, never answer them, and the answer records where it came from.

    [Fact]
    public void Suggestions_are_kept_only_on_the_pending_questions()
    {
        var answered = Pending("P-002");
        QuestionRules.Answer(answered, "Ya contestada", Now);
        var pending3 = Pending("P-003");
        var pending4 = Pending("P-004");

        QuestionRules.ApplySuggestions([answered, pending3, pending4], Example("sugerencias.json"), Now);

        Assert.Null(answered.SuggestedAnswer);
        Assert.Equal("Ya contestada", answered.Answer);
        Assert.All([pending3, pending4], q =>
        {
            Assert.Equal(QuestionStatus.Pending, q.Status);
            Assert.Null(q.Answer);
            Assert.Equal("suposicion", q.SuggestionBasis);
            Assert.Equal(Now, q.SuggestedAt);
        });
        Assert.StartsWith("Proponemos que sí", pending3.SuggestedAnswer);
        Assert.Equal(["DOC-01#F008"], pending3.SuggestionSources);
        Assert.Contains("aprobación del administrador", pending3.SuggestionNote);
    }

    [Fact]
    public void The_answer_records_whether_it_is_the_users_the_ais_or_the_ais_edited()
    {
        Question Suggested() => new()
        {
            Code = "P-001", Text = "¿?", Reason = "", Origin = "A", Status = QuestionStatus.Pending, SuggestedAnswer = "Sí, 30 minutos.",
        };
        var asIs = Suggested();
        var edited = Suggested();
        var own = Suggested();
        var withoutSuggestion = Pending();

        QuestionRules.Answer(asIs, "  Sí, 30 minutos. ", Now, fromSuggestion: true);
        QuestionRules.Answer(edited, "Sí, 45 minutos.", Now, fromSuggestion: true);
        QuestionRules.Answer(own, "No.", Now);
        QuestionRules.Answer(withoutSuggestion, "No.", Now, fromSuggestion: true);

        Assert.Equal("ia", asIs.AnswerOrigin);
        Assert.Equal("ia-editada", edited.AnswerOrigin);
        Assert.Equal("usuario", own.AnswerOrigin);
        Assert.Equal("usuario", withoutSuggestion.AnswerOrigin);

        QuestionRules.Reopen(edited, Now);
        Assert.Null(edited.AnswerOrigin);
        Assert.Equal("Sí, 30 minutos.", edited.SuggestedAnswer); // the suggestion stays for the next answer
    }
}
