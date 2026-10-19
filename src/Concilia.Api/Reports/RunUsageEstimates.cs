using System.Text.Json.Nodes;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Api.Runs;
using Concilia.Core.Runner;
using Concilia.Core.Runs;

namespace Concilia.Api.Reports;

// Step 95: the estimated tokens of the runs of a job that started but have no
// usage, from their saved console (UsageEstimator). A run without a saved
// console (before 15-10-2026) has no estimate either.
public static class RunUsageEstimates
{
    public static IReadOnlyDictionary<Guid, UsageEstimate> For(IReadOnlyList<Run> runs, string jobKind, FileStorage storage)
    {
        var estimates = new Dictionary<Guid, UsageEstimate>();
        var byId = runs.ToDictionary(r => r.Id);
        foreach (var run in runs.Where(r => r.StartedAt is not null && r.InputTokens is null))
        {
            // The repairs publish in the console of the first attempt.
            var first = run;
            while (first.RetryOfRunId is { } previous && byId.TryGetValue(previous, out var earlier))
                first = earlier;
            var events = Load(Path.Combine(storage.RunFolder(first.Id), RunEventStore.FileName));
            if (events is null || !UsageEstimator.SplitAttempts(first.Id, events).TryGetValue(run.Id, out var attempt))
                continue;
            var simulation = UsageEstimator.Simulate(attempt, path =>
                UsageEstimator.InRunFolder(path) is { } p && File.Exists(Path.Combine(storage.RunFolder(p.RunId), p.Relative))
                    ? File.ReadAllText(Path.Combine(storage.RunFolder(p.RunId), p.Relative))
                    : null);
            if (simulation.Turns > 0)
                estimates[run.Id] = UsageEstimator.Estimate(simulation, UsageEstimator.For(jobKind));
        }
        return estimates;
    }

    private static List<RunEvent>? Load(string path)
    {
        if (!File.Exists(path))
            return null;
        return JsonNode.Parse(File.ReadAllText(path))!.AsArray()
            .Select(e => new RunEvent(Enum.Parse<RunEventKind>(e!["Kind"]!.GetValue<string>()), e["Text"]!.GetValue<string>()))
            .ToList();
    }
}
