using ReqArbiter.Core.Phases;
using ReqArbiter.Core.Questions;

namespace ReqArbiter.Tests.Questions;

// Step 46: a round becomes questions of the project (renumbered, with their
// origin and the arbiter's note), and the next round sees all of them.
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
        answered.Status = QuestionStatus.Answered;
        answered.Answer = "Menos de 2 segundos.";
        var discarded = Pending("P-002");
        discarded.Status = QuestionStatus.Discarded;
        var pending = Pending("P-003");

        var block = PhasePrompt.PreviousQuestionsBlock(new[] { answered, discarded, pending }.Select(QuestionRules.ForPrompt));

        Assert.Contains("<pregunta id=\"P-001\" estado=\"respondida\"><texto>¿Cuánto es rápido?</texto><respuesta>Menos de 2 segundos.</respuesta></pregunta>", block);
        Assert.Contains("<pregunta id=\"P-002\" estado=\"descartada\">", block);
        Assert.Contains("<pregunta id=\"P-003\" estado=\"pendiente\">", block);
    }
}
