using System.Text.Json.Nodes;
using ReqArbiter.Core.Phases;

namespace ReqArbiter.Core.Questions;

// The rules of the questions phase (step 46): how the result of a round
// becomes questions of the project.
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

    private static int Number(string code) =>
        int.TryParse(code.AsSpan(code.IndexOf('-') + 1), out var n) ? n : 0;
}
