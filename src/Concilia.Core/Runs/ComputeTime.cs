using System.Diagnostics;

namespace Concilia.Core.Runs;

// How long something the platform computes by itself takes (step 95, the
// cost of the embeddings): the clock time and the processor time of the API
// process. The model of embeddings uses several cores, so the processor time
// can be higher than the clock time. Only one task runs at a time in the
// platform, so the processor time is almost all of that computation.
public record ComputeTime(double Seconds, double CpuSeconds)
{
    public static (T Result, ComputeTime Time) Measure<T>(Func<T> compute)
    {
        var process = Process.GetCurrentProcess();
        var cpuBefore = process.TotalProcessorTime;
        var clock = Stopwatch.StartNew();
        var result = compute();
        clock.Stop();
        process.Refresh();
        return (result, new ComputeTime(clock.Elapsed.TotalSeconds, (process.TotalProcessorTime - cpuBefore).TotalSeconds));
    }
}
