using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Api.Runs;
using Concilia.Core.Phases;
using Concilia.Core.Runs;

namespace Concilia.Api.Reports;

// The proposal of A or of B of a v0 job: in a new arbitration, the copy in its
// folder (it has no runs of A and B); otherwise, the output of the last
// attempt of that slot, if it ended well. Null when there is none.
public static class JobProposals
{
    public static string? Read(FileStorage storage, Job job, IReadOnlyList<Run> runs, PhaseKind phase, string slot)
    {
        if (job.Kind == Rearbitration.JobKind)
        {
            var copy = Path.Combine(storage.JobFolder(job.Id), Rearbitration.ProposalFile(slot));
            return File.Exists(copy) ? File.ReadAllText(copy) : null;
        }
        var last = runs.Where(r => r.Slot == slot).OrderBy(r => r.Attempt).LastOrDefault();
        if (last?.State != RunState.Done)
            return null;
        var path = Path.Combine(storage.RunFolder(last.Id), PhaseDefinition.For(phase, PhaseRole.Proposal).OutputFile);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
