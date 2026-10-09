using ReqArbiter.Core.Flow;
using ReqArbiter.Core.Phases;

namespace ReqArbiter.Core.Deliverables;

// The result of an action on a deliverable: the errors that stopped it (none
// if it was done), and the change it makes to the flow.
public record DeliverableChange(IReadOnlyList<string> Errors, FlowChange? Change)
{
    public bool Done => Errors.Count == 0;

    public static DeliverableChange Fail(params string[] errors) => new(errors, null);
}

// The rules of a deliverable (step 51): generate, save, validate and reopen,
// and which deliverables a change of the flow leaves outdated.
public static class DeliverableRules
{
    public static FlowStep StepOf(DeliverableKind kind) => kind switch
    {
        DeliverableKind.Catalog => FlowStep.Catalog,
        DeliverableKind.Backlog => FlowStep.Backlog,
        _ => FlowStep.Mockup,
    };

    // A job generated it: a new deliverable or, if there is one, its draft is
    // replaced (also a validated one: generating again starts a new draft).
    public static (Deliverable Deliverable, FlowChange Change) Generated(
        Deliverable? current, int projectId, DeliverableKind kind, Guid jobId, string content, DateTimeOffset now)
    {
        var d = current ?? new Deliverable { ProjectId = projectId, Kind = kind, Content = content, CreatedAt = now };
        d.JobId = jobId;
        d.Content = content;
        d.Status = DeliverableStatus.Draft;
        d.Outdated = false;
        d.ValidatedAt = null;
        d.UpdatedAt = current is null ? null : now;
        return (d, kind == DeliverableKind.Catalog ? FlowChange.CatalogGenerated : FlowChange.BacklogGenerated);
    }

    // The user's changes replace the content. A validated one is read-only.
    public static DeliverableChange Save(Deliverable d, string content, DateTimeOffset now)
    {
        if (d.Status == DeliverableStatus.Validated)
            return DeliverableChange.Fail("Está validado: reábrelo para editarlo.");
        d.Content = content;
        d.UpdatedAt = now;
        return new DeliverableChange([], null);
    }

    // Validates it if its check has no errors (the schema, the references and
    // every discrepancy reviewed). The later steps become outdated.
    public static DeliverableChange Validate(Deliverable d, PhaseValidation check, DateTimeOffset now)
    {
        if (d.Status == DeliverableStatus.Validated)
            return DeliverableChange.Fail("Ya está validado.");
        if (!check.Valid)
            return new DeliverableChange(check.Errors, null);
        d.Status = DeliverableStatus.Validated;
        d.ValidatedAt = now;
        d.UpdatedAt = now;
        return new DeliverableChange([], d.Kind == DeliverableKind.Catalog ? FlowChange.CatalogValidated : FlowChange.BacklogValidated);
    }

    // Back to draft, to edit it again. The later steps become outdated.
    public static DeliverableChange Reopen(Deliverable d, DateTimeOffset now)
    {
        if (d.Status != DeliverableStatus.Validated)
            return DeliverableChange.Fail("No está validado.");
        d.Status = DeliverableStatus.Draft;
        d.ValidatedAt = null;
        d.UpdatedAt = now;
        return new DeliverableChange([], d.Kind == DeliverableKind.Catalog ? FlowChange.CatalogReopened : FlowChange.BacklogReopened);
    }

    // Marks as outdated the deliverables that the change affects (ProjectFlow.OutdatedBy).
    public static void MarkOutdated(IEnumerable<Deliverable> deliverables, FlowChange change)
    {
        var steps = ProjectFlow.OutdatedBy(change);
        foreach (var d in deliverables.Where(d => steps.Contains(StepOf(d.Kind))))
            d.Outdated = true;
    }

    public static DeliverableFacts? Facts(Deliverable? d) => d is null ? null : new DeliverableFacts(d.Status == DeliverableStatus.Validated, d.Outdated);
}
