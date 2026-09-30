using System.Text.Json;
using System.Text.Json.Nodes;
using ReqArbiter.Api.Files;
using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Api.Runs;

// The catalog of one model (slot A or B) in a job: its valid lots merged into
// one catalog, with the text of each requirement rendered by the platform and
// the validator's report of the merged catalog. Lots that failed or have not
// finished are listed apart and left out.
public record SlotCatalog(
    string Slot,
    string Model,
    int LotTotal,
    IReadOnlyList<int> IncludedLots,
    IReadOnlyList<int> FailedLots,
    IReadOnlyList<int> PendingLots,
    CatalogValidationReport? Report,
    JsonNode? Catalog);

public static class JobCatalog
{
    // Saved in data/jobs/{jobId}/ once every lot of the slot has finished:
    // the input of the arbiter (a later milestone).
    public static string CatalogFile(string slot) => $"catalogo-{slot}.json";
    public static string ReportFile(string slot) => $"informe-{slot}.json";

    // runs: all the runs of the job (every slot, lot and attempt).
    public static SlotCatalog Build(string slot, IReadOnlyList<Run> runs, string systemName, CatalogPlan plan, FileStorage storage, Guid jobId)
    {
        // The attempt that counts for each lot is its last one.
        var lots = runs
            .Where(r => r.Slot == slot)
            .GroupBy(r => r.LotNumber ?? 1)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(r => r.Attempt).Last())
            .ToList();

        var included = lots.Where(r => r.State == RunState.Done && File.Exists(CatalogPath(storage, r))).ToList();
        var pending = lots.Where(r => r.State is RunState.Queued or RunState.Running).Select(r => r.LotNumber ?? 1).ToList();
        var failed = lots.Except(included).Select(r => r.LotNumber ?? 1).Except(pending).ToList();
        var model = lots.FirstOrDefault()?.Model ?? "";

        if (included.Count == 0)
        {
            return new SlotCatalog(slot, model, plan.Lots.Count, [], failed, pending, null, null);
        }

        var merged = CatalogMerger.Merge(included.Select(r => File.ReadAllText(CatalogPath(storage, r))).ToList());

        // Validated against the fragments of the lots it covers: the missing
        // lots are reported apart, not as uncovered fragments.
        var ids = plan.Lots
            .Where(l => included.Any(r => r.LotNumber == l.Number))
            .SelectMany(l => l.FragmentIds)
            .ToHashSet();
        var report = CatalogValidator.Validate(merged, systemName, plan.Fragments.Where(f => ids.Contains(f.Id)).ToList());
        var enriched = CatalogValidator.Enrich(merged, report.Texts);

        if (pending.Count == 0)
        {
            var folder = storage.JobFolder(jobId);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, CatalogFile(slot)), enriched);
            File.WriteAllText(Path.Combine(folder, ReportFile(slot)), JsonSerializer.Serialize(report, CatalogTask.Json));
        }

        return new SlotCatalog(
            slot, model, plan.Lots.Count,
            included.Select(r => r.LotNumber ?? 1).ToList(), failed, pending,
            report, JsonNode.Parse(enriched));
    }

    private static string CatalogPath(FileStorage storage, Run run) =>
        Path.Combine(storage.RunFolder(run.Id), CatalogTask.OutputFile);
}
