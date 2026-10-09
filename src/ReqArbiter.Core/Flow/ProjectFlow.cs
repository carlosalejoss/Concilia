namespace ReqArbiter.Core.Flow;

// The steps of the v0 flow, in order, as the project page shows them.
public enum FlowStep { Documents, Questions, Catalog, Backlog, Mockup }

// Blocked → Available → InProgress → Draft → Validated. The mockup is never
// validated: once generated it stays as Draft.
public enum StepState { Blocked, Available, InProgress, Draft, Validated }

// A catalog, backlog or mockup that exists: validated or not, and outdated
// when something it depends on changed after it was generated.
public record DeliverableFacts(bool Validated, bool Outdated);

// What the flow depends on, read from the project: files, question rounds,
// questions, deliverables and the jobs in progress.
public record FlowFacts
{
    public int TextFiles { get; init; }
    public int FinishedQuestionRounds { get; init; }
    public int PendingQuestions { get; init; }
    public int AnsweredQuestions { get; init; }
    public DeliverableFacts? Catalog { get; init; }
    public DeliverableFacts? Backlog { get; init; }
    public DeliverableFacts? Mockup { get; init; }
    public IReadOnlySet<FlowStep> Running { get; init; } = new HashSet<FlowStep>();
}

// The state of one step: can it be launched now (generate or, for the
// questions, a new round), and if not, why. Detail is a short summary for the page.
public record StepStatus(FlowStep Step, StepState State, bool Outdated, bool CanLaunch, string? Reason, string? Detail);

// Something that changes what the later steps were built on.
public enum FlowChange
{
    FileAdded,
    AnswerChanged,      // a new answer, or an answer edited
    QuestionDiscarded,
    CatalogGenerated,   // a new draft replaces the catalog
    CatalogReopened,
    CatalogValidated,
    BacklogGenerated,
    BacklogReopened,
    BacklogValidated,
    MockupGenerated,    // outdates nothing: it is the last step
}

// The rules of the v0 flow (docs/plan-de-construccion.md, "Estado de cada
// fase"): when each step can be launched, and which steps become outdated
// after a change. Outdated is a warning: nothing is deleted, and the step can
// be generated again.
public static class ProjectFlow
{
    public static IReadOnlyList<StepStatus> Compute(FlowFacts facts) =>
    [
        Documents(facts),
        Questions(facts),
        Catalog(facts),
        Backlog(facts),
        Mockup(facts),
    ];

    // The steps that become outdated after a change, if they exist.
    public static IReadOnlyList<FlowStep> OutdatedBy(FlowChange change) => change switch
    {
        FlowChange.FileAdded or FlowChange.AnswerChanged or FlowChange.QuestionDiscarded =>
            [FlowStep.Catalog, FlowStep.Backlog, FlowStep.Mockup],
        FlowChange.CatalogGenerated or FlowChange.CatalogReopened or FlowChange.CatalogValidated =>
            [FlowStep.Backlog, FlowStep.Mockup],
        FlowChange.BacklogGenerated or FlowChange.BacklogReopened or FlowChange.BacklogValidated =>
            [FlowStep.Mockup],
        _ => [],
    };

    private static StepStatus Documents(FlowFacts f) =>
        new(FlowStep.Documents, f.TextFiles > 0 ? StepState.Draft : StepState.Available, false, true, null,
            f.TextFiles == 1 ? "1 archivo con texto" : $"{f.TextFiles} archivos con texto");

    private static StepStatus Questions(FlowFacts f)
    {
        var detail = $"{Plural(f.FinishedQuestionRounds, "ronda terminada", "rondas terminadas")}, {Plural(f.PendingQuestions, "pendiente", "pendientes")}";
        if (f.TextFiles == 0)
            return new(FlowStep.Questions, StepState.Blocked, false, false, "Sube al menos un archivo con texto.", detail);
        if (f.Running.Contains(FlowStep.Questions))
            return new(FlowStep.Questions, StepState.InProgress, false, false, "Hay una ronda en curso.", detail);
        return new(FlowStep.Questions, f.FinishedQuestionRounds > 0 ? StepState.Draft : StepState.Available, false, true, null, detail);
    }

    private static StepStatus Catalog(FlowFacts f)
    {
        string? reason = null;
        if (f.FinishedQuestionRounds == 0)
            reason = "Termina al menos una ronda de preguntas.";
        else if (f.PendingQuestions > 0)
            reason = $"Contesta o descarta {Plural(f.PendingQuestions, "pregunta pendiente", "preguntas pendientes")}.";
        return Deliverable(FlowStep.Catalog, f.Catalog, f.Running, reason);
    }

    private static StepStatus Backlog(FlowFacts f) =>
        Deliverable(FlowStep.Backlog, f.Backlog, f.Running, f.Catalog is { Validated: true } ? null : "Valida el catálogo.");

    private static StepStatus Mockup(FlowFacts f) =>
        Deliverable(FlowStep.Mockup, f.Mockup, f.Running, f.Backlog is { Validated: true } ? null : "Valida el backlog.");

    // A step that produces a deliverable. Its state is that of the deliverable
    // (draft or validated) once it exists; whether it can be launched (again)
    // depends on the earlier steps and on no job of this step being in progress.
    private static StepStatus Deliverable(FlowStep step, DeliverableFacts? deliverable, IReadOnlySet<FlowStep> running, string? reason)
    {
        var outdated = deliverable?.Outdated ?? false;
        if (running.Contains(step))
            return new(step, StepState.InProgress, outdated, false, "Hay un trabajo en curso.", null);
        var state = deliverable switch
        {
            null => reason is null ? StepState.Available : StepState.Blocked,
            { Validated: true } => StepState.Validated,
            _ => StepState.Draft,
        };
        return new(step, state, outdated, reason is null, reason, null);
    }

    private static string Plural(int n, string one, string many) => $"{n} {(n == 1 ? one : many)}";
}
