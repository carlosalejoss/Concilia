using ReqArbiter.Core.Catalog;

namespace ReqArbiter.Core.Phases;

// A question of the project as the prompts see it. Status: "respondida",
// "descartada" or "pendiente"; Answer only when it is answered.
public record PhaseQuestion(string Id, string Text, string Status, string? Answer);

// The values of the placeholders of the v0 prompts. Each phase uses some of
// them (table in prompts/v0/README.md); the others stay empty.
public record PhaseInput
{
    public required string SystemName { get; init; } // with article: "el sistema ReservaSalas"
    public required string Language { get; init; }   // "es" or "en"
    public string Glossary { get; init; } = "";
    public string Documentation { get; init; } = ""; // Fragmenter.DocumentBlock with ALL the fragments (no lots in v0)
    public string PreviousQuestions { get; init; } = ""; // PhasePrompt.PreviousQuestionsBlock
    public string Answers { get; init; } = "";           // PhasePrompt.AnswersBlock
    public string Catalog { get; init; } = "";           // the validated catalog (backlog phase)
    public string Backlog { get; init; } = "";           // the validated backlog (mockup)
    public string Proposal1 { get; init; } = "";         // arbiter: the two proposals, in random order
    public string Proposal2 { get; init; } = "";
}

// Builds the prompts of the v0 phases from their templates.
public static class PhasePrompt
{
    public static string Template(PhaseDefinition phase) => PromptBuilder.ReadResource(phase.PromptResource);

    public static string RepairAnnexTemplate { get; } = PromptBuilder.ReadResource("v0/anexo_reparacion.md");

    // One pass over the template, as in PromptBuilder: a document that
    // contains "{{...}}" reaches the model as it is.
    public static string Build(PhaseDefinition phase, PhaseInput input) => PromptBuilder.Fill(Template(phase), new Dictionary<string, string>
    {
        ["NOMBRE_SISTEMA"] = input.SystemName,
        ["IDIOMA"] = input.Language,
        ["GLOSARIO"] = input.Glossary,
        ["DOCUMENTACION"] = input.Documentation,
        ["PREGUNTAS_PREVIAS"] = input.PreviousQuestions,
        ["RESPUESTAS"] = input.Answers,
        ["CATALOGO"] = input.Catalog,
        ["BACKLOG"] = input.Backlog,
        ["PROPUESTA_1"] = input.Proposal1,
        ["PROPUESTA_2"] = input.Proposal2,
    });

    // The repair is the ORIGINAL prompt plus the annex with the previous output
    // and the errors, one per line (not the previous prompt, so annexes do not
    // pile up).
    public static string Repair(string originalPrompt, int attempt, string outputFile, string previousOutput, IEnumerable<string> errors) =>
        originalPrompt + "\n" + PromptBuilder.Fill(RepairAnnexTemplate, new Dictionary<string, string>
        {
            ["INTENTO"] = attempt.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["FICHERO"] = outputFile,
            ["SALIDA_PREVIA"] = previousOutput,
            ["ERRORES"] = string.Join("\n", errors),
        });

    // {{PREGUNTAS_PREVIAS}}: every question of the earlier rounds, with its
    // status; the answer only when it is answered.
    // <pregunta id="P-003" estado="respondida"><texto>…</texto><respuesta>…</respuesta></pregunta>
    public static string PreviousQuestionsBlock(IEnumerable<PhaseQuestion> questions) =>
        string.Join("\n", questions.Select(q =>
            $"<pregunta id=\"{q.Id}\" estado=\"{q.Status}\"><texto>{Fragmenter.SafeText(q.Text)}</texto>"
            + (q.Status == "respondida" ? $"<respuesta>{Fragmenter.SafeText(q.Answer ?? "")}</respuesta>" : "")
            + "</pregunta>"));

    // {{RESPUESTAS}}: only the answered questions.
    // <pregunta id="P-003"><texto>…</texto><respuesta>…</respuesta></pregunta>
    public static string AnswersBlock(IEnumerable<PhaseQuestion> questions) =>
        string.Join("\n", questions.Where(q => q.Status == "respondida").Select(q =>
            $"<pregunta id=\"{q.Id}\"><texto>{Fragmenter.SafeText(q.Text)}</texto><respuesta>{Fragmenter.SafeText(q.Answer ?? "")}</respuesta></pregunta>"));
}
