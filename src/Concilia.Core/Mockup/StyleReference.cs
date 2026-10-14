using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Concilia.Core.Files;

namespace Concilia.Core.Mockup;

// A mockup the client already has, uploaded as a .zip, that our mockup must
// look like (decision of 14-10-2026, step 78). It is not documentation: only
// the mockup reads it. The model gets its .html and .css files in
// input/estilo/ and works the style out of them by itself. Its images and
// fonts can be used by path ("input/estilo/marca/img/logo.png"); the platform
// puts them inside the final .html as data: URIs (Inline), so the mockup is
// still a single file that opens offline.
public static class StyleReference
{
    public const string Folder = "input/estilo";

    // Limits of a .zip, against archives that unpack into huge files.
    public const int MaxEntries = 500;
    public const long MaxUnpackedBytes = 50 * 1024 * 1024;

    // What the model may read: at most this much text in all (about 34,000
    // tokens), so the reference does not take the room of the backlog.
    public const int MaxTextChars = 120_000;
    // Images and fonts larger than this are left out: they would make the
    // mockup heavy.
    public const int MaxAssetBytes = 512 * 1024;
    public const int MaxAssetsBytes = 4 * 1024 * 1024;

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase) { ".html", ".htm", ".css" };

    // The media type of each kind of image or font, for its data: URI.
    private static readonly Dictionary<string, string> AssetTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".svg"] = "image/svg+xml",
        [".ico"] = "image/x-icon",
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
        [".ttf"] = "font/ttf",
        [".otf"] = "font/otf",
    };

    // A path of the reference in the parts the model wrote, between quotes
    // or in url(...): "input/estilo/marca/img/logo.png".
    private static readonly Regex AssetPath = new(@"(?<=[""'(])\s*input/estilo/[^""')\s]+", RegexOptions.Compiled);

    // Whether an uploaded file is a style reference: every .zip is one.
    public static bool IsStyleFile(string fileName) =>
        Path.GetExtension(fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase);

    // Returns an error for the user, or null if the .zip can be used.
    public static string? Validate(byte[] zip)
    {
        try
        {
            using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
            if (archive.Entries.Count > MaxEntries)
                return $"El ZIP tiene {archive.Entries.Count} archivos; el máximo es {MaxEntries}.";
            if (archive.Entries.Sum(e => e.Length) > MaxUnpackedBytes)
                return $"El ZIP ocupa más de {MaxUnpackedBytes / (1024 * 1024)} MB descomprimido.";
            if (!archive.Entries.Any(e => SafePath(e.FullName) is { } path && TextExtensions.Contains(Path.GetExtension(path))))
                return "El ZIP no contiene ningún .html ni .css: como referencia de estilo, debe traer las páginas o las hojas de estilo del mockup.";
            return null;
        }
        catch (InvalidDataException)
        {
            return "No se ha podido leer el ZIP: el archivo está dañado o no es un ZIP.";
        }
    }

    // Unpacks the .html, .css, images and fonts of a .zip into folder/name/,
    // keeping its subfolders. Everything else (scripts, documents...) is left out.
    public static void Extract(byte[] zip, string folder, string name)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (SafePath(entry.FullName) is not { } path)
                continue;
            var extension = Path.GetExtension(path);
            if (!TextExtensions.Contains(extension) && !AssetTypes.ContainsKey(extension))
                continue;

            var target = Path.Combine(folder, name, path);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var input = entry.Open();
            using var buffer = new MemoryStream();
            input.CopyTo(buffer);
            var bytes = buffer.ToArray();
            if (TextExtensions.Contains(extension))
                File.WriteAllText(target, Readable(TextExtractor.Extract(bytes, ".txt") ?? "", extension));
            else
                File.WriteAllBytes(target, bytes);
        }
    }

    // A name for the folder of a .zip, from its file name: letters, digits,
    // "-" and "_" only ("Mockup cliente v2.zip" -> "Mockup-cliente-v2").
    public static string FolderName(string fileName)
    {
        var name = Regex.Replace(Path.GetFileNameWithoutExtension(fileName), @"[^\p{L}\p{Nd}_-]+", "-").Trim('-');
        return name.Length == 0 ? "estilo" : name;
    }

    // The path of an entry inside the .zip, or null if it is a folder, a file
    // of the Mac's metadata or hidden, or a path that would leave the folder
    // ("../x", "/x"): a .zip can be made to write anywhere ("zip slip").
    private static string? SafePath(string fullName)
    {
        var parts = fullName.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || fullName.EndsWith('/') || fullName.StartsWith('/') || fullName.Contains(':'))
            return null;
        if (parts.Any(p => p == ".." || p == "." || p.StartsWith('.') || p == "__MACOSX"))
            return null;
        return string.Join('/', parts);
    }

    // A minified stylesheet is a single long line, which Read cuts at 2,000
    // characters: a line break after each rule keeps it readable.
    private static string Readable(string text, string extension) =>
        extension.Equals(".css", StringComparison.OrdinalIgnoreCase) && text.Split('\n').Any(l => l.Length > 1_000)
            ? text.Replace("}", "}\n")
            : text;

    // What the prompt says about the reference ({{ESTILO}}), from the files
    // in workDirectory/input/estilo/, or null if there is none. Text files
    // after MaxTextChars and images or fonts over the limits are not offered
    // (the prompt says so).
    public static string? Describe(string workDirectory)
    {
        var root = Path.Combine(workDirectory, Folder);
        if (!Directory.Exists(root))
            return null;

        var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(f => (Full: f, Path: Relative(workDirectory, f)))
            .OrderBy(f => f.Path, StringComparer.Ordinal)
            .ToList();

        var texts = new List<string>();
        var left = 0;
        var chars = 0;
        // The pages first, then the stylesheets.
        foreach (var file in files.Where(f => TextExtensions.Contains(Path.GetExtension(f.Path)))
                     .OrderBy(f => Path.GetExtension(f.Path).Equals(".css", StringComparison.OrdinalIgnoreCase)))
        {
            var content = File.ReadAllText(file.Full);
            if (chars + content.Length > MaxTextChars)
            {
                left++;
                continue;
            }
            chars += content.Length;
            texts.Add($"  - `{file.Path}` ({Runner.ReadCoverage.LineCount(content)} líneas)");
        }

        var assets = new List<string>();
        long assetBytes = 0;
        foreach (var file in files.Where(f => AssetTypes.ContainsKey(Path.GetExtension(f.Path))))
        {
            var size = new FileInfo(file.Full).Length;
            if (size > MaxAssetBytes || assetBytes + size > MaxAssetsBytes)
                continue;
            assetBytes += size;
            assets.Add($"  - `{file.Path}` ({Math.Max(1, size / 1024)} KB)");
        }

        var text = new StringBuilder();
        text.AppendLine("El cliente ha dado un prototipo suyo como referencia de estilo, en `input/estilo/`. El tuyo debe parecer de la misma aplicación: la misma paleta, fuentes, tamaños, espaciados y bordes, y la misma forma de cabecera, menú, tarjetas, tablas, formularios y botones.");
        text.AppendLine();
        text.AppendLine("- Lee con Read sus páginas y hojas de estilo para sacar el estilo; no hace falta leerlas enteras:");
        text.AppendLine(string.Join("\n", texts));
        if (left > 0)
            text.AppendLine($"  ({left} ficheros más no se ofrecen: con estos basta para ver el estilo.)");
        text.AppendLine("- No copies sus textos, sus datos ni sus pantallas: el contenido sale solo del backlog.");
        text.AppendLine("- Si necesitas algo de un `.js` de la referencia, no lo busques: no se incluye.");
        if (assets.Count > 0)
        {
            text.AppendLine("- Puedes usar estas imágenes y fuentes de la referencia, escribiendo su ruta exacta en `src=\"…\"` o en `url(…)`. La plataforma las mete dentro del fichero al juntarlo, así que no cuentan como recurso externo. Puedes ver las imágenes con Read.");
            text.AppendLine(string.Join("\n", assets));
        }
        text.Append("- No uses ninguna otra ruta de `input/estilo/`.");
        return text.ToString();
    }

    // Puts the images and fonts of the reference that the page uses inside it,
    // as data: URIs. Also returns the paths that do not exist (or are too
    // large): errors for the repair.
    public static (string Html, IReadOnlyList<string> Missing) Inline(string workDirectory, string html)
    {
        var missing = new List<string>();
        var inlined = AssetPath.Replace(html, match =>
        {
            var path = match.Value.Trim();
            var full = Path.GetFullPath(Path.Combine(workDirectory, path));
            var root = Path.GetFullPath(Path.Combine(workDirectory, Folder)) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(root, StringComparison.Ordinal) || !File.Exists(full)
                || !AssetTypes.TryGetValue(Path.GetExtension(full), out var type)
                || new FileInfo(full).Length > MaxAssetBytes)
            {
                missing.Add(path);
                return match.Value;
            }
            return $"data:{type};base64,{Convert.ToBase64String(File.ReadAllBytes(full))}";
        });
        return (inlined, missing.Distinct().ToList());
    }

    private static string Relative(string workDirectory, string full) =>
        Path.GetRelativePath(workDirectory, full).Replace('\\', '/');
}
