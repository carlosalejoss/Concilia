using System.Text.Json.Nodes;
using Concilia.Core.Phases;
using Concilia.Core.Reports;

namespace Concilia.Core.Deliverables;

// The history of a deliverable (step 101, decisions of the author of
// 23-10-2026): each time it is generated, edited or validated, a copy of it
// is kept, with its date and origin, and all of them are kept. A save that
// leaves it as the last version was (same content, state and job) records
// nothing; validating always does, since the state changes. Reopening does not:
// the content is the same one that was validated.
public static class DeliverableHistory
{
    // The version to record after a change of d, or null if it would be the
    // same as the last one. last: the last version of its kind and project.
    public static DeliverableVersion? Next(Deliverable d, DeliverableVersion? last, VersionOrigin origin, DateTimeOffset now)
    {
        if (last is not null && last.Content == d.Content && last.Status == d.Status && last.JobId == d.JobId)
            return null;
        return new DeliverableVersion
        {
            ProjectId = d.ProjectId,
            Kind = d.Kind,
            Number = (last?.Number ?? 0) + 1,
            Origin = origin,
            JobId = d.JobId,
            Status = d.Status,
            Content = d.Content,
            Elements = Elements(d.Kind, d.Content),
            CreatedAt = now,
        };
    }

    // How many elements a version has, for the list: requirements of a
    // catalog, stories of a backlog and screens of a mockup.
    public static int? Elements(DeliverableKind kind, string content) => kind switch
    {
        DeliverableKind.Catalog => JobReports.Count(PhaseKind.Catalog, content),
        DeliverableKind.Backlog => JobReports.Count(PhaseKind.Backlog, content),
        _ => JsonNode.Parse(content)?["pantallas"]?.GetValue<int>(),
    };
}
