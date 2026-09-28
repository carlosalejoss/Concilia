using System.Text;

namespace ReqArbiter.Core.Files;

// Turns the bytes of an uploaded file into plain text, for the file types
// supported so far (.txt and .md). PDF and DOCX will come later.
public static class TextExtractor
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".md",
    };

    // UTF-8 that throws on invalid bytes instead of silently replacing them,
    // so we can tell that a file is not UTF-8.
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    static TextExtractor()
    {
        // .NET only includes a few encodings by default; this enables the
        // Windows code pages, such as Windows-1252.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static bool CanExtract(string extension) => SupportedExtensions.Contains(extension);

    // Returns the text, or null if the extension is not supported.
    public static string? Extract(byte[] content, string extension)
    {
        if (!CanExtract(extension))
        {
            return null;
        }

        return NormalizeLineEndings(Decode(content));
    }

    // Text files have no fixed encoding. Try UTF-8 first (the common case, with
    // or without BOM); if the bytes are not valid UTF-8, assume Windows-1252,
    // the usual encoding of text files saved by Spanish-language Windows tools.
    private static string Decode(byte[] content)
    {
        var bytes = content.AsSpan();
        var utf8Bom = Encoding.UTF8.Preamble;
        if (bytes.StartsWith(utf8Bom))
        {
            bytes = bytes[utf8Bom.Length..];
        }

        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(1252).GetString(content);
        }
    }

    // Windows (\r\n) and old Mac (\r) line endings become \n.
    private static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n');
}
