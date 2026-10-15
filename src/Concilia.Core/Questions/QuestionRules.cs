using System.Text.Json.Nodes;
using Concilia.Core.Flow;
using Concilia.Core.Phases;

namespace Concilia.Core.Questions;

// The result of an action on a question: an error for the user, or the change
// it makes to the flow (to mark the later steps as outdated).
public record QuestionChange(string? Error, FlowChange? Change)
{
    public static QuestionChange Fail(string error) => new(error, null);
}

// The rules of the questions phase (step 46 and 47): how the result of a round
// becomes questions of the project, and what can be done with each one.
public static class QuestionRules
{
    // Spanish names of the statuses, as the prompts see them.
    public static string StatusName(QuestionStatus status) => status switch
    {
        QuestionStatus.Answered => "respondida",
        QuestionStatus.Discarded => "descartada",
        _ => "pendiente",
    };

    // The questions a round adds to the project. The model numbers them from
    // P-001 in each round; the platform renumbers them after the ones the
    // project already has. Origin comes from the arbiter's `de` ("A", "B" or
    // "ambas"); without cross-check, from the only model that ended well.
    public static List<Question> FromRound(
        PhaseJobResult result, PhasePlan plan, int projectId, Guid jobId, IEnumerable<string> existingCodes, DateTimeOffset now)
    {
        if (result.Source is null || result.Output is null)
            return [];

        var next = existingCodes.Select(Number).DefaultIfEmpty(0).Max() + 1;
        var questions = new List<Question>();
        foreach (var item in JsonNode.Parse(result.Output)!["preguntas"]!.AsArray().Select(x => x!.AsObject()))
        {
            var crossChecked = result.Source == "ARB";
            questions.Add(new Question
            {
                ProjectId = projectId,
                JobId = jobId,
                Code = $"P-{next++:000}",
                Text = item["pregunta"]!.GetValue<string>(),
                Reason = item["motivo"]!.GetValue<string>(),
                Sources = item["fuentes"]!.AsArray().Select(f => f!.GetValue<string>()).ToList(),
                Origin = crossChecked ? plan.OriginOf(item["de"]!.AsObject()) ?? "ambas" : result.Source,
                ArbiterNote = crossChecked
                    ? item["explicacion"]?.GetValue<string>()
                    : $"Sin verificación cruzada: solo terminó bien {result.Source}.",
                Status = QuestionStatus.Pending,
                CreatedAt = now,
            });
        }
        return questions;
    }

    // The question as a previous question of the next round. All of them go,
    // pending ones too: they were already asked, and the next round must not
    // ask them again.
    public static PhaseQuestion ForPrompt(Question q) => new(q.Code, q.Text, StatusName(q.Status), q.Answer);

    // Answers a pending question, or changes the answer of an answered one.
    // fromSuggestion: the user started from the AI's suggestion; the origin is
    // "ia" if it is saved as it was and "ia-editada" if the user changed it.
    public static QuestionChange Answer(Question q, string? answer, DateTimeOffset now, bool fromSuggestion = false)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return QuestionChange.Fail("La respuesta no puede estar vacía.");
        if (q.Status == QuestionStatus.Discarded)
            return QuestionChange.Fail("La pregunta está descartada: reábrela antes de contestarla.");
        q.Status = QuestionStatus.Answered;
        q.Answer = answer.Trim();
        q.AnswerOrigin = !fromSuggestion || q.SuggestedAnswer is null ? "usuario"
            : q.Answer == q.SuggestedAnswer.Trim() ? "ia" : "ia-editada";
        q.DiscardReason = null;
        q.UpdatedAt = now;
        return new QuestionChange(null, FlowChange.AnswerChanged);
    }

    // The suggestions of the AI (output/sugerencias.json) on the pending
    // questions of the project, by code. Nothing is answered: each question
    // only keeps its last suggestion for the user to use, edit or ignore.
    public static void ApplySuggestions(IEnumerable<Question> questions, string output, DateTimeOffset now)
    {
        var byCode = questions.Where(q => q.Status == QuestionStatus.Pending).ToDictionary(q => q.Code);
        foreach (var s in JsonNode.Parse(output)!["sugerencias"]!.AsArray().Select(x => x!.AsObject()))
        {
            if (!byCode.TryGetValue(s["pregunta"]!.GetValue<string>(), out var q))
                continue; // answered or discarded meanwhile: the suggestion is no longer needed
            q.SuggestedAnswer = s["respuesta"]!.GetValue<string>();
            q.SuggestionBasis = s["base"]!.GetValue<string>();
            q.SuggestionSources = s["fuentes"]!.AsArray().Select(f => f!.GetValue<string>()).ToList();
            q.SuggestionNote = s["explicacion"]!.GetValue<string>();
            q.SuggestedAt = now;
        }
    }

    // "Aprobar todas" (15-10-2026, autor): answers every pending question of a
    // round with its suggestion as is (origin "ia"). Only when all of them have
    // one, the same condition under which the web shows the button: approving
    // some and leaving the rest pending would not be "all".
    public static QuestionChange AcceptSuggestions(IEnumerable<Question> roundQuestions, DateTimeOffset now)
    {
        var pending = roundQuestions.Where(q => q.Status == QuestionStatus.Pending).ToList();
        if (pending.Count == 0)
            return QuestionChange.Fail("La ronda no tiene preguntas pendientes.");
        var without = pending.Where(q => q.SuggestedAnswer is null).Select(q => q.Code).ToList();
        if (without.Count > 0)
            return QuestionChange.Fail($"Sin sugerencia: {string.Join(", ", without)}. Pídelas antes de aprobarlas todas.");
        foreach (var q in pending)
            Answer(q, q.SuggestedAnswer, now, fromSuggestion: true);
        return new QuestionChange(null, FlowChange.AnswerChanged);
    }

    // Discards a question (not needed, out of scope…), with an optional reason.
    // A discarded question has no answer.
    public static QuestionChange Discard(Question q, string? reason, DateTimeOffset now)
    {
        if (q.Status == QuestionStatus.Discarded)
            return QuestionChange.Fail("La pregunta ya está descartada.");
        q.Status = QuestionStatus.Discarded;
        q.Answer = null;
        q.AnswerOrigin = null;
        q.DiscardReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        q.UpdatedAt = now;
        return new QuestionChange(null, FlowChange.QuestionDiscarded);
    }

    // Takes an answered or discarded question back to pending. Removing an
    // answer changes what the catalog is built on.
    public static QuestionChange Reopen(Question q, DateTimeOffset now)
    {
        if (q.Status == QuestionStatus.Pending)
            return QuestionChange.Fail("La pregunta ya está pendiente.");
        var hadAnswer = q.Status == QuestionStatus.Answered;
        q.Status = QuestionStatus.Pending;
        q.Answer = null;
        q.AnswerOrigin = null;
        q.DiscardReason = null;
        q.UpdatedAt = now;
        return new QuestionChange(null, hadAnswer ? FlowChange.AnswerChanged : null);
    }

    private static int Number(string code) =>
        int.TryParse(code.AsSpan(code.IndexOf('-') + 1), out var n) ? n : 0;
}
