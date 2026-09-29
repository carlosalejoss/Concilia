namespace ReqArbiter.Api.Runs;

// A project file whose extracted text goes into a run: the name the user
// uploaded and the path of its .extracted.txt on disk.
public record RunInputFile(string OriginalName, string TextPath);

public static class RunInput
{
    // Copies the extracted texts into the run's input/ folder, as "<name>.txt".
    // The names come from users, so they are cleaned, and repeated names get
    // a suffix ("acta.txt", "acta-2.txt"). Returns the names written.
    public static IReadOnlyList<string> Prepare(string inputFolder, IEnumerable<RunInputFile> files)
    {
        Directory.CreateDirectory(inputFolder);
        var written = new List<string>();

        foreach (var file in files)
        {
            var baseName = SafeName(Path.GetFileNameWithoutExtension(file.OriginalName));
            var name = baseName + ".txt";
            for (var n = 2; written.Contains(name, StringComparer.OrdinalIgnoreCase); n++)
            {
                name = $"{baseName}-{n}.txt";
            }

            File.Copy(file.TextPath, Path.Combine(inputFolder, name));
            written.Add(name);
        }

        return written;
    }

    // Keeps letters, digits, spaces, dots, dashes and underscores.
    private static string SafeName(string name)
    {
        var safe = new string(name.Select(c => char.IsLetterOrDigit(c) || c is ' ' or '.' or '-' or '_' ? c : '_').ToArray()).Trim(' ', '.');
        return safe.Length == 0 ? "documento" : safe;
    }
}
