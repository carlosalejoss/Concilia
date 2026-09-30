using System.Diagnostics;
using System.Text;

namespace ReqArbiter.Runner.Execution;

// What to launch. Environment is the COMPLETE environment of the child
// (see ChildEnvironment): nothing else is inherited.
public record ProcessLaunch(
    string Executable,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment,
    string StandardInput,
    TimeSpan HeartbeatTimeout,
    TimeSpan TotalTimeout);

public enum ProcessEnd
{
    Exited,        // The process finished by itself (any exit code).
    TimedOut,      // Killed: TotalTimeout reached.
    HeartbeatLost, // Killed: no stdout line during HeartbeatTimeout.
    Stopped,       // Killed: the caller asked to stop (see stopRequested).
}

// ExitCode is null when the process was killed.
public record ProcessOutcome(ProcessEnd End, int? ExitCode, IReadOnlyList<string> StandardError);

// Launches one process, feeds it the prompt and reads its output line by line,
// watching the heartbeat and the total time.
public static class ClaudeProcess
{
    // UTF-8 without BOM, explicitly: never the console's code page (that is how
    // "—" became "ÔÇö" in the recorded fixtures).
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    // onStdoutLine receives every stdout line, in order, as soon as it arrives.
    // Cancelling the token kills the process tree and throws OperationCanceledException.
    // stopRequested is different: it kills the process tree too, but the run
    // ends normally with ProcessEnd.Stopped (the caller saw that going on is useless).
    public static async Task<ProcessOutcome> RunAsync(
        ProcessLaunch launch,
        Action<string> onStdoutLine,
        CancellationToken cancellationToken,
        CancellationToken stopRequested = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = launch.Executable,
            WorkingDirectory = launch.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Utf8NoBom,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom,
        };

        // One item per argument: no quoting problems with spaces. (On Windows,
        // "claude.cmd" runs through cmd.exe, which re-parses the arguments; ours
        // come from the configuration, never from users, and the prompt goes
        // through stdin, so this is acceptable.)
        foreach (var argument in launch.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Start from an empty environment instead of a copy of ours.
        startInfo.Environment.Clear();
        foreach (var (name, value) in launch.Environment)
        {
            startInfo.Environment[name] = value;
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Time of the last stdout line, shared with the watchdog loop below.
        long lastLineTimestamp = Stopwatch.GetTimestamp();
        var total = Stopwatch.StartNew();

        // Read stdout and stderr at the same time and separately: if one pipe
        // fills up while nobody reads it, the child blocks.
        var stdoutTask = Task.Run(async () =>
        {
            while (await process.StandardOutput.ReadLineAsync() is { } line)
            {
                Interlocked.Exchange(ref lastLineTimestamp, Stopwatch.GetTimestamp());
                onStdoutLine(line);
            }
        });

        var standardError = new List<string>();
        var stderrTask = Task.Run(async () =>
        {
            while (await process.StandardError.ReadLineAsync() is { } line)
            {
                lock (standardError)
                {
                    standardError.Add(line);
                }
            }
        });

        // Written in parallel with the watchdog: a child that never reads its
        // input must not block us before the timeouts can act.
        var stdinTask = WriteInputAsync(process, launch.StandardInput);

        var exitTask = process.WaitForExitAsync(CancellationToken.None);
        var end = ProcessEnd.Exited;
        var checkInterval = CheckInterval(launch);

        // Watchdog: wake up regularly until the process exits.
        while (!exitTask.IsCompleted)
        {
            await Task.WhenAny(exitTask, Task.Delay(checkInterval, cancellationToken));

            if (cancellationToken.IsCancellationRequested)
            {
                KillTree(process);
                await exitTask;
                await Task.WhenAll(stdoutTask, stderrTask, stdinTask);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (exitTask.IsCompleted)
            {
                break;
            }

            if (stopRequested.IsCancellationRequested)
            {
                end = ProcessEnd.Stopped;
                KillTree(process);
                break;
            }

            if (total.Elapsed >= launch.TotalTimeout)
            {
                end = ProcessEnd.TimedOut;
                KillTree(process);
                break;
            }

            var silence = Stopwatch.GetElapsedTime(Interlocked.Read(ref lastLineTimestamp));
            if (silence >= launch.HeartbeatTimeout)
            {
                end = ProcessEnd.HeartbeatLost;
                KillTree(process);
                break;
            }
        }

        await exitTask;
        // The pipes close when the process (tree) ends, so the readers finish too.
        await Task.WhenAll(stdoutTask, stderrTask, stdinTask);

        return new ProcessOutcome(end, end == ProcessEnd.Exited ? process.ExitCode : null, standardError);
    }

    // Write the prompt and close stdin, so the child knows the input is complete.
    private static async Task WriteInputAsync(Process process, string input)
    {
        try
        {
            await process.StandardInput.WriteAsync(input);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();
        }
        catch (IOException)
        {
            // The child exited before reading its input. Not an error here:
            // its exit code and output will tell what happened.
        }
    }

    // Check often enough to react within ~10% of the shortest limit, but at
    // most once every 50 ms and at least once a second.
    private static TimeSpan CheckInterval(ProcessLaunch launch)
    {
        var shortest = launch.HeartbeatTimeout < launch.TotalTimeout ? launch.HeartbeatTimeout : launch.TotalTimeout;
        var interval = shortest / 10;
        return interval < TimeSpan.FromMilliseconds(50) ? TimeSpan.FromMilliseconds(50)
            : interval > TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1)
            : interval;
    }

    // Kill the process and its children (claude starts node, which may start more).
    private static void KillTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // It had already exited.
        }
    }
}
