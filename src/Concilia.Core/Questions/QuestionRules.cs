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
                // Step 112: the answer or the requirement a new question contradicts.
                ContradictsQuestion = item["contradice"]?["pregunta"]?.GetValue<string>(),
                ContradictsRequirement = item["contradice"]?["requisito"]?.GetValue<string>(),
            });
        }
        return questions;
    }

    // Step 112: the revisions the arbiter of a centred round kept, applied to
    // the pending questions of the project. A reformulation changes the text
    // (the earlier one stays in History, and the suggestion of the AI, made for
    // it, is dropped); "resuelta" and "obsoleta" are only proposals. A question
    // that is no longer pending (answered while the round ran) is left alone.
    public static void ApplyRevisions(IEnumerable<Question> questions, PhaseJobResult result, PhasePlan plan, Guid jobId, DateTimeOffset now)
    {
        if (result.Output is null || plan.Focus is null || JsonNode.Parse(result.Output)?["revisiones"] is not JsonArray revisions)
            return;
        var byCode = questions.ToDictionary(q => q.Code);
        foreach (var r in revisions.OfType<JsonObject>())
        {
            var action = r["accion"]!.GetValue<string>();
            if (action == "ninguna" || !byCode.TryGetValue(r["pregunta"]!.GetValue<string>(), out var q) || q.Status != QuestionStatus.Pending)
                continue;
            var from = r["de"]!.AsArray().Select(n => plan.ProposalOrder[n!.GetValue<int>() - 1]).ToList();
            var revision = new QuestionRevision(
                action == "reformular" ? "reformulada" : $"propuesta-{action}", plan.Focus.Iterations,
                action == "reformular" ? q.Text : null, r["motivo"]!.GetValue<string>(),
                r["fuentes"]!.AsArray().Select(f => f!.GetValue<string>()).ToList(),
                from.Count == 2 ? "ambas" : from.Single(), r["discrepancia"]!.GetValue<bool>(), r["explicacion"]?.GetValue<string>(), jobId, now);
            q.History = [.. q.History, revision];
            if (action == "reformular")
            {
                q.Text = r["texto"]!.GetValue<string>();
                q.SuggestedAnswer = null;
                q.SuggestionBasis = null;
                q.SuggestionNote = null;
                q.SuggestionSources = [];
                q.SuggestedAt = null;
            }
            else
            {
                q.ProposedAction = action;
                q.ProposedReason = revision.Reason;
                q.ProposedSources = [.. revision.Sources];
            }
            q.UpdatedAt = now;
        }
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

    // "Aprobar todas" (15-10-2026, autor): answers the pending questions of a
    // round with their suggestion as is (origin "ia"). Only when all of them
    // have one, the same condition under which the web shows the button.
    // Step 93 (decision of the author, 19-10-2026): only the suggestions that
    // come from the documentation; the guesses ("suposicion") stay pending, to
    // be reviewed one by one. In the tests almost every suggestion was a guess,
    // they were all approved without a look, and the catalog filled with data
    // the documentation does not have.
    public static QuestionChange AcceptSuggestions(IEnumerable<Question> roundQuestions, DateTimeOffset now)
    {
        var pending = roundQuestions.Where(q => q.Status == QuestionStatus.Pending).ToList();
        if (pending.Count == 0)
            return QuestionChange.Fail("La ronda no tiene preguntas pendientes.");
        var without = pending.Where(q => q.SuggestedAnswer is null).Select(q => q.Code).ToList();
        if (without.Count > 0)
            return QuestionChange.Fail($"Sin sugerencia: {string.Join(", ", without)}. Pídelas antes de aprobarlas todas.");
        var fromDocumentation = pending.Where(q => q.SuggestionBasis == DocumentationBasis).ToList();
        if (fromDocumentation.Count == 0)
            return QuestionChange.Fail("Todas las sugerencias son suposiciones: revísalas una a una.");
        foreach (var q in fromDocumentation)
            Answer(q, q.SuggestedAnswer, now, fromSuggestion: true);
        return new QuestionChange(null, FlowChange.AnswerChanged);
    }

    // The basis of a suggestion: the documentation or a guess of the AI.
    public const string DocumentationBasis = "documentacion";
    public const string GuessBasis = "suposicion";

    // An answer taken as it was from a guess of the AI (step 93): what the
    // catalog marks. One the user edited ("ia-editada") was reviewed.
    public static bool IsAcceptedGuess(Question q) =>
        q.Status == QuestionStatus.Answered && q.AnswerOrigin == "ia" && q.SuggestionBasis == GuessBasis;

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
