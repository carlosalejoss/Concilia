using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Concilia.Core.Files;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Concilia.Tests;

// The test documents are built in memory with the same libraries, using
// fictitious ReservaSalas content, so no binary files are kept in the repository.
public class DocumentExtractionTests
{
    [Fact]
    public void Docx_paragraphs_become_lines()
    {
        var docx = BuildDocx(body =>
        {
            body.Append(Paragraph("ReservaSalas"));
            body.Append(Paragraph("El sistema permitirá cancelar una reserva."));
        });

        var text = TextExtractor.Extract(docx, ".docx");

        Assert.Equal("ReservaSalas\nEl sistema permitirá cancelar una reserva.", text);
    }

    [Fact]
    public void Docx_table_rows_become_lines_with_separated_cells()
    {
        var docx = BuildDocx(body =>
        {
            body.Append(Paragraph("Requisitos"));
            body.Append(new Table(
                Row("ID", "Descripción"),
                Row("RF-01", "Reservar una sala")));
        });

        var text = TextExtractor.Extract(docx, ".docx");

        Assert.Equal("Requisitos\nID | Descripción\nRF-01 | Reservar una sala", text);
    }

    [Fact]
    public void Pdf_text_is_extracted_page_by_page()
    {
        var pdf = BuildPdf(["ReservaSalas pagina uno", "Cancelar reserva pagina dos"]);

        var text = TextExtractor.Extract(pdf, ".pdf");

        Assert.Equal("ReservaSalas pagina uno\n\nCancelar reserva pagina dos", text);
    }

    [Fact]
    public void Pdf_without_text_gives_empty_text()
    {
        // Like a scanned PDF: pages exist but contain no text.
        var pdf = BuildPdf([""]);

        Assert.Equal("", TextExtractor.Extract(pdf, ".pdf"));
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".docx")]
    public void Damaged_files_throw_extraction_exception(string extension)
    {
        var notADocument = "esto no es un documento"u8.ToArray();

        Assert.Throws<TextExtractionException>(() => TextExtractor.Extract(notADocument, extension));
    }

    private static byte[] BuildDocx(Action<Body> fill)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var body = new Body();
            fill(body);
            document.AddMainDocumentPart().Document = new Document(body);
        }
        return stream.ToArray();
    }

    private static Paragraph Paragraph(string text) => new(new Run(new Text(text)));

    private static TableRow Row(params string[] cells) =>
        new(cells.Select(cell => new TableCell(Paragraph(cell))));

    // One page per string, written with a built-in PDF font (Helvetica).
    private static byte[] BuildPdf(string[] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        foreach (var pageText in pages)
        {
            var page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
            if (pageText.Length > 0)
            {
                page.AddText(pageText, 12, new PdfPoint(50, 750), font);
            }
        }
        return builder.Build();
    }
}
