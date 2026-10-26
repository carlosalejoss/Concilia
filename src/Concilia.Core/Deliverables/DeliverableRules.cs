using Concilia.Core.Flow;
using Concilia.Core.Phases;

namespace Concilia.Core.Deliverables;

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
    // iterations: those whose documentation the job read (step 111).
    public static (Deliverable Deliverable, FlowChange Change) Generated(
        Deliverable? current, int projectId, DeliverableKind kind, Guid jobId, string content, DateTimeOffset now, IReadOnlyList<int>? iterations = null)
    {
        var d = current ?? new Deliverable { ProjectId = projectId, Kind = kind, Content = content, CreatedAt = now };
        d.JobId = jobId;
        d.Content = content;
        d.Iterations = iterations is null ? null : [.. iterations];
        d.Status = DeliverableStatus.Draft;
        d.Outdated = false;
        d.ValidatedAt = null;
        d.UpdatedAt = current is null ? null : now;
        return (d, kind switch
        {
            DeliverableKind.Catalog => FlowChange.CatalogGenerated,
            DeliverableKind.Backlog => FlowChange.BacklogGenerated,
            _ => FlowChange.MockupGenerated,
        });
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

    // Back to an earlier version (step 102, decision of the author of
    // 23-10-2026): its content and its job come back, always as a draft (the
    // answers it rests on may have changed since it was validated) and with
    // its outdated warning. The later steps become outdated, as when it is
    // generated. The version it has now is refused: nothing would change.
    public static DeliverableChange Restore(Deliverable d, DeliverableVersion version, DateTimeOffset now)
    {
        if (version.Content == d.Content && version.JobId == d.JobId)
            return DeliverableChange.Fail("Es la versión que tiene ahora.");
        d.Content = version.Content;
        d.JobId = version.JobId;
        d.Iterations = version.Iterations is null ? null : [.. version.Iterations];
        d.Status = DeliverableStatus.Draft;
        d.Outdated = version.Outdated;
        d.ValidatedAt = null;
        d.UpdatedAt = now;
        return new DeliverableChange([], d.Kind switch
        {
            DeliverableKind.Catalog => FlowChange.CatalogGenerated,
            DeliverableKind.Backlog => FlowChange.BacklogGenerated,
            _ => FlowChange.MockupGenerated,
        });
    }

    // The kinds of deliverable that the change outdates (ProjectFlow.OutdatedBy).
    public static IReadOnlyList<DeliverableKind> OutdatedKinds(FlowChange change)
    {
        var steps = ProjectFlow.OutdatedBy(change);
        return Enum.GetValues<DeliverableKind>().Where(k => steps.Contains(StepOf(k))).ToList();
    }

    // Marks as outdated the deliverables that the change affects, and their
    // versions (step 102), which are all from before the change.
    public static void MarkOutdated(IEnumerable<Deliverable> deliverables, FlowChange change, IEnumerable<DeliverableVersion>? versions = null)
    {
        var kinds = OutdatedKinds(change);
        foreach (var d in deliverables.Where(d => kinds.Contains(d.Kind)))
            d.Outdated = true;
        foreach (var v in (versions ?? []).Where(v => kinds.Contains(v.Kind)))
            v.Outdated = true;
    }

    public static DeliverableFacts? Facts(Deliverable? d) => d is null ? null : new DeliverableFacts(d.Status == DeliverableStatus.Validated, d.Outdated);
}
