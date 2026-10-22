using Concilia.Core.Deliverables.Export;

namespace Concilia.Core.Deliverables;

// A requirement with what traces it: the fragments of the documentation and
// the questions it rests on, and the stories of the backlog that cover it.
public record TraceRequirement(
    string Id, string Type, string Text, IReadOnlyList<string> Sources, IReadOnlyList<string> Questions,
    IReadOnlyList<string> Stories, bool AiGuess, bool PendingDiscrepancy);

// A story with its epic and the requirements it covers.
public record TraceStory(
    string Id, string Title, string Narrative, string EpicId, string EpicTitle,
    IReadOnlyList<string> Requirements, bool PendingDiscrepancy);

// The traceability of a project (step 99): from a requirement to its
// fragments (with their text), its questions and its stories, and from a
// story to its requirements and, through them, to the documentation. The web
// shows it in a panel where one can go from one element to the next.
//
// It is built from the same content as the exports (CatalogExport and
// BacklogExport), so the panel and the Word say the same: each cited fragment
// and question appears once, in Fragments and Questions, and the elements
// point to them by id. What is cited but does not exist is reported:
// a fragment without Text (a citation typed by hand that is not in the
// documentation), MissingQuestions and MissingRequirements (a story that
// cites a requirement the catalog no longer has).
public record Traceability(
    IReadOnlyList<TraceRequirement> Requirements, IReadOnlyList<TraceStory> Stories,
    IReadOnlyList<ExportSource> Fragments, IReadOnlyList<ExportQuestion> Questions,
    IReadOnlyList<string> MissingQuestions, IReadOnlyList<string> MissingRequirements)
{
    // backlog: null while the project has no backlog.
    public static Traceability Build(CatalogExport catalog, BacklogExport? backlog)
    {
        var stories = (backlog?.Epics ?? [])
            .SelectMany(e => e.Stories.Select(h => new TraceStory(h.Id, h.Title, h.Narrative, e.Id, e.Title, h.Requirements, h.PendingDiscrepancy)))
            .ToList();

        var requirements = catalog.Sections
            .SelectMany(s => s.Requirements.Select(r => new TraceRequirement(
                r.Id, s.Type, r.Text, r.Sources, r.Questions,
                stories.Where(h => h.Requirements.Contains(r.Id)).Select(h => h.Id).ToList(),
                r.AiGuess, r.PendingDiscrepancy)))
            .ToList();

        var known = requirements.Select(r => r.Id).ToHashSet();
        var missingRequirements = stories.SelectMany(h => h.Requirements).Where(id => !known.Contains(id))
            .Distinct().Order(StringComparer.Ordinal).ToList();
        var found = catalog.Questions.Select(q => q.Id).ToHashSet();
        var missingQuestions = requirements.SelectMany(r => r.Questions).Where(id => !found.Contains(id))
            .Distinct().Order(StringComparer.Ordinal).ToList();

        return new Traceability(requirements, stories, catalog.Sources, catalog.Questions, missingQuestions, missingRequirements);
    }
}
