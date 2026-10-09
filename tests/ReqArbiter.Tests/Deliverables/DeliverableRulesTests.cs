using ReqArbiter.Core.Deliverables;
using ReqArbiter.Core.Flow;
using ReqArbiter.Core.Phases;

namespace ReqArbiter.Tests.Deliverables;

// Step 51: a generated deliverable is a draft; saving replaces its content;
// validating needs a check without errors; a validated one is read-only until
// it is reopened; and each change outdates the later steps.
public class DeliverableRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 10, 0, 0, TimeSpan.Zero);
    private static readonly PhaseValidation Clean = new([], []);

    private static Deliverable NewCatalog() =>
        DeliverableRules.Generated(null, 1, DeliverableKind.Catalog, Guid.NewGuid(), "{\"requisitos\":[]}", Now).Deliverable;

    [Fact]
    public void A_generated_deliverable_is_a_draft_that_outdates_the_later_steps()
    {
        var (d, change) = DeliverableRules.Generated(null, 1, DeliverableKind.Catalog, Guid.NewGuid(), "{}", Now);

        Assert.Equal(DeliverableStatus.Draft, d.Status);
        Assert.False(d.Outdated);
        Assert.Equal(FlowChange.CatalogGenerated, change);
    }

    [Fact]
    public void Generating_again_replaces_the_content_even_of_a_validated_one()
    {
        var d = NewCatalog();
        DeliverableRules.Validate(d, Clean, Now);
        d.Outdated = true;
        var newJob = Guid.NewGuid();

        var (again, _) = DeliverableRules.Generated(d, 1, DeliverableKind.Catalog, newJob, "{\"requisitos\":[1]}", Now);

        Assert.Same(d, again);
        Assert.Equal(DeliverableStatus.Draft, d.Status);
        Assert.False(d.Outdated);
        Assert.Equal(newJob, d.JobId);
        Assert.Equal("{\"requisitos\":[1]}", d.Content);
        Assert.Null(d.ValidatedAt);
    }

    [Fact]
    public void Saving_replaces_the_content_of_a_draft()
    {
        var d = NewCatalog();

        Assert.True(DeliverableRules.Save(d, "{\"requisitos\":[2]}", Now).Done);
        Assert.Equal("{\"requisitos\":[2]}", d.Content);
    }

    [Fact]
    public void A_validated_deliverable_is_read_only()
    {
        var d = NewCatalog();
        DeliverableRules.Validate(d, Clean, Now);

        var change = DeliverableRules.Save(d, "{}", Now);

        Assert.False(change.Done);
        Assert.Equal("Está validado: reábrelo para editarlo.", change.Errors[0]);
        Assert.Equal("{\"requisitos\":[]}", d.Content);
    }

    [Fact]
    public void Validating_needs_a_check_without_errors()
    {
        var d = NewCatalog();

        var change = DeliverableRules.Validate(d, new PhaseValidation(["RF-003 DISC: hay una discrepancia sin revisar."], []), Now);

        Assert.False(change.Done);
        Assert.Equal(DeliverableStatus.Draft, d.Status);
        Assert.Contains("DISC", change.Errors[0]);
    }

    [Fact]
    public void Warnings_do_not_keep_it_from_being_validated()
    {
        var d = NewCatalog();

        var change = DeliverableRules.Validate(d, new PhaseValidation([], ["RF-009 COB: aviso"]), Now);

        Assert.True(change.Done);
        Assert.Equal(DeliverableStatus.Validated, d.Status);
        Assert.Equal(FlowChange.CatalogValidated, change.Change);
        Assert.Equal(Now, d.ValidatedAt);
    }

    [Fact]
    public void Reopening_takes_it_back_to_draft()
    {
        var d = NewCatalog();
        Assert.False(DeliverableRules.Reopen(d, Now).Done); // a draft cannot be reopened
        DeliverableRules.Validate(d, Clean, Now);

        var change = DeliverableRules.Reopen(d, Now);

        Assert.True(change.Done);
        Assert.Equal(DeliverableStatus.Draft, d.Status);
        Assert.Equal(FlowChange.CatalogReopened, change.Change);
    }

    [Fact]
    public void A_change_outdates_only_the_deliverables_it_affects()
    {
        var catalog = NewCatalog();
        var backlog = DeliverableRules.Generated(null, 1, DeliverableKind.Backlog, Guid.NewGuid(), "{}", Now).Deliverable;

        DeliverableRules.MarkOutdated([catalog, backlog], FlowChange.CatalogReopened);
        Assert.False(catalog.Outdated);
        Assert.True(backlog.Outdated);

        DeliverableRules.MarkOutdated([catalog, backlog], FlowChange.AnswerChanged);
        Assert.True(catalog.Outdated);
    }

    [Fact]
    public void The_flow_sees_whether_it_is_validated_and_outdated()
    {
        var d = NewCatalog();
        d.Outdated = true;
        DeliverableRules.Validate(d, Clean, Now);

        Assert.Equal(new DeliverableFacts(true, true), DeliverableRules.Facts(d));
        Assert.Null(DeliverableRules.Facts(null));
    }
}
