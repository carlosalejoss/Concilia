using System.Reflection;
using PdfSharp.Fonts;

namespace Concilia.Core.Deliverables.Export;

// The fonts of the PDFs: IBM Plex Sans, the font of the web, embedded in
// Concilia.Core (Fonts/, SIL Open Font License 1.1). PDFsharp does not look for
// fonts in the system by itself on Linux (the API container has none), so it
// asks this resolver: any family is Plex, and bold italic is the bold face
// slanted by PDFsharp.
public class ExportFonts : IFontResolver
{
    public const string Family = "IBM Plex Sans";

    private static readonly object Gate = new();

    // PDFsharp keeps one resolver for the whole process: it is set the first
    // time a PDF is made.
    public static void Register()
    {
        lock (Gate)
        {
            if (GlobalFontSettings.FontResolver is not ExportFonts)
                GlobalFontSettings.FontResolver = new ExportFonts();
        }
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) => (bold, italic) switch
    {
        (true, true) => new FontResolverInfo("IBMPlexSans-Bold", mustSimulateBold: false, mustSimulateItalic: true),
        (true, false) => new FontResolverInfo("IBMPlexSans-Bold"),
        (false, true) => new FontResolverInfo("IBMPlexSans-Italic"),
        _ => new FontResolverInfo("IBMPlexSans-Regular"),
    };

    public byte[] GetFont(string faceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"fonts/{faceName}.ttf")
            ?? throw new InvalidOperationException($"Falta la fuente {faceName} en Concilia.Core.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
