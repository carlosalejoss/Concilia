namespace Concilia.Core.Runner;

public static class RunPaths
{
    // Turns a path relative to the work directory into a full path, and refuses
    // anything that would end up outside it (e.g. "../../x" or an absolute path).
    public static string ResolveInside(string workDirectory, string relativePath)
    {
        var root = Path.GetFullPath(workDirectory);
        var full = Path.GetFullPath(Path.Combine(root, relativePath));
        var rootWithSeparator = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;

        if (!full.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new ArgumentException($"La ruta '{relativePath}' queda fuera de la carpeta de la ejecución.", nameof(relativePath));
        }

        return full;
    }
}
