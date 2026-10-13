namespace Concilia.Core.Runner;

// One Read call of the model, taken from the stream: the file (relative to the
// run folder, with "/", when it is inside it), the first line it asked for and
// how many lines (null: Claude Code's default), and whether the tool answered
// without error. A file too big for one call comes back as an error, and lines
// are only counted as read when the call succeeded.
public record FileRead(string Path, int Offset, int? Limit, bool Succeeded)
{
    // Lines Claude Code's Read returns when the call gives no limit.
    public const int DefaultLimit = 2000;

    public int FirstLine => Math.Max(1, Offset);

    public int LastLine => FirstLine + (Limit ?? DefaultLimit) - 1;
}

// Which lines of a file the model has read (point 5 of the reading by parts,
// 09-10-2026): the platform does not trust the model to read everything; it
// checks the Read calls in the stream.
public static class ReadCoverage
{
    // The ranges of lines [From, To] of a file of totalLines lines that no
    // successful Read of that path covered, in order. Empty: read entirely.
    public static IReadOnlyList<(int From, int To)> Missing(string path, int totalLines, IEnumerable<FileRead> reads)
    {
        var read = new bool[totalLines + 1];
        foreach (var r in reads.Where(r => r.Succeeded && SamePath(r.Path, path)))
            for (var line = r.FirstLine; line <= Math.Min(r.LastLine, totalLines); line++)
                read[line] = true;

        var missing = new List<(int From, int To)>();
        for (var line = 1; line <= totalLines; line++)
        {
            if (read[line])
                continue;
            var from = line;
            while (line < totalLines && !read[line + 1])
                line++;
            missing.Add((from, line));
        }
        return missing;
    }

    // "1–10, 25–40": the ranges for a message.
    public static string Describe(IEnumerable<(int From, int To)> ranges) =>
        string.Join(", ", ranges.Select(r => r.From == r.To ? $"{r.From}" : $"{r.From}–{r.To}"));

    public static int LineCount(string text) =>
        text.Length == 0 ? 0 : text.Split('\n').Length - (text.EndsWith('\n') ? 1 : 0);

    private static bool SamePath(string a, string b) =>
        string.Equals(a.Replace('\\', '/').TrimStart('.', '/'), b.Replace('\\', '/').TrimStart('.', '/'), StringComparison.Ordinal);
}
