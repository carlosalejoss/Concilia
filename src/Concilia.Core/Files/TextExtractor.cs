using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Concilia.Core.Files;

// The file could not be read as the type its extension says (e.g. a damaged
// PDF, or a .docx that is not really a Word document).
public class TextExtractionException(string message, Exception inner) : Exception(message, inner);

// Turns the bytes of an uploaded file into plain text, for the supported types:
// documents (.txt, .md, .docx, .pdf), spreadsheets (.xlsx, .csv), presentations
// (.pptx), web pages (.html, .htm), emails (.eml, .msg), meeting transcripts
// (.vtt, .srt) and data files (.json, .yaml, .yml, .xml, kept as they are).
// Scanned PDFs (images only) give empty text: there is no OCR.
public static class TextExtractor
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".md",
        ".docx",
        ".pdf",
        ".xlsx",
        ".csv",
        ".pptx",
        ".html",
        ".htm",
        ".eml",
        ".msg",
        ".vtt",
        ".srt",
        ".json",
        ".yaml",
        ".yml",
        ".xml",
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
    // Throws TextExtractionException if the content cannot be read.
    public static string? Extract(byte[] content, string extension)
    {
        if (!CanExtract(extension))
        {
            return null;
        }

        try
        {
            var text = extension.ToLowerInvariant() switch
            {
                ".docx" => ExtractDocx(content),
                ".pdf" => ExtractPdf(content),
                ".xlsx" => SpreadsheetText.FromXlsx(content),
                ".csv" => SpreadsheetText.FromCsv(PlainText(content)),
                ".pptx" => SlidesText.FromPptx(content),
                ".html" or ".htm" => HtmlText.Convert(PlainText(content)),
                ".eml" => EmailText.FromEml(content),
                ".msg" => EmailText.FromMsg(content),
                ".vtt" or ".srt" => TranscriptText.Convert(PlainText(content)),
                _ => DecodePlainText(content), // .txt, .md, .json, .yaml, .yml and .xml
            };
            return NormalizeLineEndings(text);
        }
        catch (Exception e) when (e is not TextExtractionException)
        {
            // Each library throws its own exception types for broken files;
            // callers only need to know that this file could not be read.
            throw new TextExtractionException($"No se ha podido leer el contenido del archivo {extension}.", e);
        }
    }

    // The text of a text-based format, with \n line endings, for the converters.
    private static string PlainText(byte[] content) => NormalizeLineEndings(DecodePlainText(content));

    // Text files have no fixed encoding. Try UTF-8 first (the common case, with
    // or without BOM); if the bytes are not valid UTF-8, assume Windows-1252,
    // the usual encoding of text files saved by Spanish-language Windows tools.
    private static string DecodePlainText(byte[] content)
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

    // A .docx is a ZIP of XML files. We walk the top-level blocks of the body:
    // each paragraph becomes a line, and each table row becomes a line with its
    // cells separated by " | ", so requirement tables keep their structure.
    private static string ExtractDocx(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var document = WordprocessingDocument.Open(stream, isEditable: false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return "";
        }

        var lines = new List<string>();
        foreach (var block in body.ChildElements)
        {
            switch (block)
            {
                case Paragraph paragraph:
                    lines.Add(paragraph.InnerText);
                    break;
                case Table table:
                    foreach (var row in table.Elements<TableRow>())
                    {
                        var cells = row.Elements<TableCell>().Select(CellText);
                        lines.Add(string.Join(" | ", cells));
                    }
                    break;
                default:
                    // Other blocks (e.g. content controls) may still contain paragraphs.
                    lines.AddRange(block.Descendants<Paragraph>().Select(p => p.InnerText));
                    break;
            }
        }

        return string.Join("\n", lines);
    }

    // A cell can hold several paragraphs; join them with spaces to keep one line per row.
    private static string CellText(TableCell cell) =>
        string.Join(" ", cell.Elements<Paragraph>().Select(p => p.InnerText));

    // A PDF stores positioned glyphs, not paragraphs. ContentOrderTextExtractor
    // rebuilds lines and spaces in reading order. Pages are separated by a blank line.
    private static string ExtractPdf(byte[] content)
    {
        using var document = PdfDocument.Open(content);
        var pages = document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page).Trim());
        return string.Join("\n\n", pages);
    }

    // Windows (\r\n) and old Mac (\r) line endings become \n.
    private static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n');
}
