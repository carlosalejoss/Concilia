using Concilia.Core.Deliverables;
using Concilia.Core.Phases;

namespace Concilia.Tests.Deliverables;

// Step 101: each change of a deliverable records a version, numbered for its
// kind and project, unless it leaves it as the last version was.
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
}
