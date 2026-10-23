using Concilia.Core.Deliverables;
using Concilia.Core.Flow;
using Concilia.Core.Phases;

namespace Concilia.Tests.Deliverables;

// Step 101: each change of a deliverable records a version, numbered for its
// kind and project, unless it leaves it as the last version was. Step 102:
// going back to one of them.
public class DeliverableHistoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 10, 0, 0, TimeSpan.Zero);
    private static readonly PhaseValidation Clean = new([], []);

    private const string TwoRequirements = """{"requisitos":[{"id":"RF-001"},{"id":"RF-002"}]}""";

    private static Deliverable NewCatalog(Guid job) =>
        DeliverableRules.Generated(null, 7, DeliverableKind.Catalog, job, TwoRequirements, Now).Deliverable;

    [Fact]
    public void The_first_version_is_number_one_with_the_content_state_and_job()
    {
        var job = Guid.NewGuid();
        var d = NewCatalog(job);

        var v = DeliverableHistory.Next(d, null, VersionOrigin.Generated, Now)!;

        Assert.Equal(1, v.Number);
        Assert.Equal((7, DeliverableKind.Catalog, VersionOrigin.Generated, job), (v.ProjectId, v.Kind, v.Origin, v.JobId));
        Assert.Equal(DeliverableStatus.Draft, v.Status);
        Assert.Equal(TwoRequirements, v.Content);
        Assert.Equal(2, v.Elements);
        Assert.Equal(Now, v.CreatedAt);
    }

    [Fact]
    public void A_save_without_changes_records_nothing_but_validating_does()
    {
        var d = NewCatalog(Guid.NewGuid());
        var first = DeliverableHistory.Next(d, null, VersionOrigin.Generated, Now)!;

        DeliverableRules.Save(d, TwoRequirements, Now);
        Assert.Null(DeliverableHistory.Next(d, first, VersionOrigin.Edited, Now));

        DeliverableRules.Validate(d, Clean, Now);
        var validated = DeliverableHistory.Next(d, first, VersionOrigin.Validated, Now)!;
        Assert.Equal(2, validated.Number);
        Assert.Equal(DeliverableStatus.Validated, validated.Status);
        Assert.Equal(first.Content, validated.Content);
    }

    [Fact]
    public void An_edit_or_a_new_job_with_the_same_content_is_a_new_version()
    {
        var d = NewCatalog(Guid.NewGuid());
        var first = DeliverableHistory.Next(d, null, VersionOrigin.Generated, Now)!;

        DeliverableRules.Save(d, """{"requisitos":[{"id":"RF-001"}]}""", Now);
        var edited = DeliverableHistory.Next(d, first, VersionOrigin.Edited, Now)!;
        Assert.Equal((2, VersionOrigin.Edited, 1), (edited.Number, edited.Origin, edited.Elements));

        // The same catalog from another job: its checks use another plan.
        var newJob = Guid.NewGuid();
        DeliverableRules.Generated(d, 7, DeliverableKind.Catalog, newJob, d.Content, Now);
        var again = DeliverableHistory.Next(d, edited, VersionOrigin.Generated, Now)!;
        Assert.Equal((3, newJob), (again.Number, again.JobId));
    }

    [Fact]
    public void The_elements_are_stories_in_a_backlog_and_screens_in_a_mockup()
    {
        Assert.Equal(3, DeliverableHistory.Elements(DeliverableKind.Backlog,
            """{"epicas":[{"historias":[{"id":"HU-001"},{"id":"HU-002"}]},{"historias":[{"id":"HU-003"}]}]}"""));
        Assert.Equal(12, DeliverableHistory.Elements(DeliverableKind.Mockup, """{"historias":5,"pantallas":12,"bytes":1,"avisos":[]}"""));
        // A mockup of the prompts v0.4, a view per epic, has no screens.
        Assert.Null(DeliverableHistory.Elements(DeliverableKind.Mockup, """{"historias":5,"bytes":1,"avisos":[]}"""));
    }

    // Step 102: going back to a version brings back its content and job as a
    // draft, with its outdated warning, and outdates the later steps.
    [Fact]
    public void Going_back_to_a_version_brings_back_its_content_and_job_as_a_draft()
    {
        var oldJob = Guid.NewGuid();
        var d = NewCatalog(oldJob);
        DeliverableRules.Validate(d, Clean, Now);
        var validated = DeliverableHistory.Next(d, null, VersionOrigin.Validated, Now)!;
        DeliverableRules.Generated(d, 7, DeliverableKind.Catalog, Guid.NewGuid(), """{"requisitos":[]}""", Now);

        var later = Now.AddHours(1);
        var change = DeliverableRules.Restore(d, validated, later);

        Assert.True(change.Done);
        Assert.Equal(FlowChange.CatalogGenerated, change.Change);
        Assert.Equal((TwoRequirements, oldJob, DeliverableStatus.Draft), (d.Content, d.JobId, d.Status));
        Assert.Null(d.ValidatedAt);
        Assert.Equal(later, d.UpdatedAt);
        var restored = DeliverableHistory.Next(d, validated, VersionOrigin.Restored, later, validated.Number)!;
        Assert.Equal((2, VersionOrigin.Restored, 1), (restored.Number, restored.Origin, restored.RestoredFrom));

        // The version it has now cannot be brought back: nothing would change.
        Assert.False(DeliverableRules.Restore(d, restored, later).Done);
    }

    [Fact]
    public void A_change_of_the_flow_outdates_the_versions_and_going_back_brings_it_back()
    {
        var catalog = NewCatalog(Guid.NewGuid());
        var backlog = DeliverableRules.Generated(null, 7, DeliverableKind.Backlog, Guid.NewGuid(), """{"epicas":[]}""", Now).Deliverable;
        var catalogVersion = DeliverableHistory.Next(catalog, null, VersionOrigin.Generated, Now)!;
        var backlogVersion = DeliverableHistory.Next(backlog, null, VersionOrigin.Generated, Now)!;

        DeliverableRules.MarkOutdated([catalog, backlog], FlowChange.CatalogValidated, [catalogVersion, backlogVersion]);

        Assert.False(catalogVersion.Outdated); // validating the catalog does not outdate it
        Assert.True(backlogVersion.Outdated);

        // A new backlog is up to date; going back to the old one brings its warning back.
        DeliverableRules.Generated(backlog, 7, DeliverableKind.Backlog, Guid.NewGuid(), """{"epicas":[{"historias":[]}]}""", Now);
        Assert.False(backlog.Outdated);
        DeliverableRules.Restore(backlog, backlogVersion, Now);
        Assert.True(backlog.Outdated);
    }
}
