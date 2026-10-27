using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Api.Runs;
using Concilia.Core.Catalog;
using Concilia.Core.Deliverables;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;
using Concilia.Core.Projects;
using Concilia.Core.Questions;

namespace Concilia.Api.Questions;

// What a round of questions centred on some iterations reads, built when it is
// launched (step 112, option C): the documentation of those iterations, the
// earlier fragments related to it and, if the catalog is validated, its
// related requirements (RelatedContext, with embeddings in local), and the
// block of the iterations. The documentation is numbered as always
// (PhaseJobs.LoadDocumentsAsync), so the ids are the same in every mode.
// Step 116 (readAll, ReadAllRound): option A, every earlier fragment is read
// entirely instead of the related ones; the related requirements stay the
// same, so only the reading of the documentation changes.
public static class FocusedRoundInput
{
    public static async Task<(PhaseInput? Input, FocusPlan? Focus, string? Error)> BuildAsync(
        AppDbContext db, ITextEmbedder embedder, int projectId, IReadOnlyList<int> focus,
        IReadOnlyList<CatalogDocument> documents, IReadOnlyList<Question> previous, PhaseInput input, bool readAll = false)
    {
        var iterations = await db.Iterations.Where(i => i.ProjectId == projectId).ToListAsync();
        var byDocument = await ProjectIterations.DocumentsAsync(db, projectId);
        if (IterationRules.ValidateFocus(focus, iterations, byDocument.Values.ToHashSet()) is { } error)
            return (null, null, error);

        var names = documents.ToDictionary(d => d.Id, d => d.Name);
        var fragments = documents.ToDictionary(d => d.Id, d => Fragmenter.FragmentDocument(d.Id, d.Text));
        var newDocuments = documents.Where(d => byDocument.TryGetValue(d.Id, out var n) && focus.Contains(n)).Select(d => d.Id).ToList();
        var fresh = newDocuments.SelectMany(id => fragments[id]).ToList();
        var earlier = documents.Where(d => !newDocuments.Contains(d.Id)).SelectMany(d => fragments[d.Id]).ToList();
        var selected = RelatedContext.Fragments(embedder, fresh, earlier);
        var related = readAll ? earlier : selected;

        // The validated catalog, if there is one: entire to consult, and its related requirements to read.
        var catalog = await db.Deliverables.SingleOrDefaultAsync(d => d.ProjectId == projectId && d.Kind == DeliverableKind.Catalog);
        var validated = catalog is { Status: DeliverableStatus.Validated } ? CatalogDraft.ForNextPhase(catalog.Content) : "";

        var focused = input with
        {
            Iterations = FocusedRound.IterationsBlock(focus, byDocument),
            NewDocumentation = DocumentationFile.Build(fresh, names),
            RelatedFragments = related.Count > 0 ? DocumentationFile.Build(related, names) : "",
            Catalog = validated,
            RelatedRequirements = validated.Length > 0
                ? RelatedContext.Requirements(embedder, validated, fresh, selected.Select(f => f.Id).ToHashSet())
                : "",
        };
        var plan = new FocusPlan(focus, newDocuments,
            previous.Select(q => new FocusQuestion(q.Code, q.Text, QuestionRules.StatusName(q.Status))).ToList());
        return (focused, plan, null);
    }
}
