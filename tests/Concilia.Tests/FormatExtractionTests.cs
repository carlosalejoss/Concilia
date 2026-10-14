using System.Text;
using Concilia.Core.Catalog;
using Concilia.Core.Files;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using MimeKit;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using S = DocumentFormat.OpenXml.Spreadsheet;

namespace Concilia.Tests;

// The formats added on 14-10-2026: spreadsheets, presentations, web pages,
// emails, transcripts and data files. As in DocumentExtractionTests, the
// binary documents are built in memory with fictitious ReservaSalas content.
public class FormatExtractionTests
{
    private static string? Extract(string text, string extension) =>
        TextExtractor.Extract(Encoding.UTF8.GetBytes(text), extension);

    [Fact]
    public void Csv_with_semicolons_becomes_a_table()
    {
        var text = Extract("Sala;Capacidad\r\nAzul;12\r\n\"Roja; grande\";\"30 \"\"máx.\"\"\"\r\n", ".csv");

        Assert.Equal("| Sala | Capacidad |\n\n| Azul | 12 |\n\n| Roja; grande | 30 \"máx.\" |", text);
    }

    [Fact]
    public void Csv_with_commas_and_line_breaks_in_a_cell()
    {
        var text = Extract("id,descripción\nRF-01,\"Reservar\nuna sala\"\n", ".csv");

        Assert.Equal("| id | descripción |\n\n| RF-01 | Reservar una sala |", text);
    }

    [Fact]
    public void A_pipe_in_a_cell_does_not_split_it()
    {
        Assert.Equal("| a/b | c |", Extract("a|b,c", ".csv"));
    }

    [Fact]
    public void A_sheet_keeps_at_most_2000_rows_and_says_how_many_are_left_out()
    {
        var csv = string.Join("\n", Enumerable.Range(1, SpreadsheetText.MaxRows + 5).Select(i => $"fila {i};x"));

        var lines = Extract(csv, ".csv")!.Split("\n\n");

        Assert.Equal(SpreadsheetText.MaxRows + 1, lines.Length);
        Assert.Equal("| fila 2000 | x |", lines[^2]);
        Assert.Equal("(Se han omitido 5 filas: de cada hoja solo se leen las primeras 2.000.)", lines[^1]);
    }

    [Fact]
    public void Each_table_row_is_a_fragment_of_its_own()
    {
        var text = Extract("Sala;Capacidad\nAzul;12\nRoja;30", ".csv")!;

        var fragments = Fragmenter.FragmentDocument("DOC-01", text).Select(f => f.Text);

        Assert.Equal(["| Sala | Capacidad |", "| Azul | 12 |", "| Roja | 30 |"], fragments);
    }

    [Fact]
    public void Xlsx_sheets_become_tables_with_texts_numbers_dates_and_gaps()
    {
        var xlsx = BuildXlsx(
            ("Salas", [
                [Text("A1", "Sala"), Text("B1", "Capacidad"), Text("C1", "Alta")],
                [Text("A2", "Azul"), Number("B2", "12"), Date("C2", "45580")],
                [Text("A3", "Roja"), Text("C3", "pendiente")],
            ]),
            ("Notas", [[Text("B1", "Revisar")]]));

        var text = TextExtractor.Extract(xlsx, ".xlsx");

        Assert.Equal(
            "## Hoja: Salas\n\n| Sala | Capacidad | Alta |\n\n| Azul | 12 | 2024-10-15 |\n\n| Roja |  | pendiente |\n\n"
            + "## Hoja: Notas\n\n|  | Revisar |",
            text);
    }

    [Fact]
    public void Pptx_slides_give_title_items_tables_and_notes()
    {
        var pptx = BuildPptx(
            ("Reservas", ["El empleado reserva una sala.", "Puede cancelarla."], "Origen: reunión con RR. HH."),
            ("", ["Sin título"], null));

        var text = TextExtractor.Extract(pptx, ".pptx");

        Assert.Equal(
            "## Diapositiva 1: Reservas\n\n- El empleado reserva una sala.\n\n- Puede cancelarla.\n\nNotas: Origen: reunión con RR. HH.\n\n"
            + "## Diapositiva 2\n\n- Sin título",
            text);
    }

    [Fact]
    public void Html_keeps_headings_paragraphs_lists_and_tables_without_scripts()
    {
        var html = """
            <html><head><title>ReservaSalas</title><style>p { color: red }</style></head>
            <body>
              <h2>Reservas</h2>
              <p>El empleado <b>reserva</b> una sala &amp; la confirma.</p>
              <ul><li>Cancelar</li><li><p>Modificar</p></li></ul>
              <table><tr><th>Sala</th><th>Capacidad</th></tr><tr><td>Azul</td><td>12</td></tr></table>
              <script>alert('no')</script>
            </body></html>
            """;

        var text = Extract(html, ".html");

        Assert.Equal(
            "# ReservaSalas\n\n## Reservas\n\nEl empleado reserva una sala & la confirma.\n\n- Cancelar\n\n- Modificar\n\n"
            + "| Sala | Capacidad |\n\n| Azul | 12 |",
            text);
    }

    [Fact]
    public void Eml_gives_subject_people_date_body_and_attachments()
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Ana Gómez", "ana@example.com"));
        message.To.Add(new MailboxAddress("Luis Pérez", "luis@example.com"));
        message.Subject = "Requisitos de reservas";
        message.Date = new DateTimeOffset(2026, 10, 14, 9, 30, 0, TimeSpan.Zero);
        var body = new BodyBuilder { TextBody = "Las reservas duran como mucho 4 horas." };
        body.Attachments.Add("salas.xlsx", [1, 2, 3]);
        message.Body = body.ToMessageBody();
        using var stream = new MemoryStream();
        message.WriteTo(stream);

        var text = TextExtractor.Extract(stream.ToArray(), ".eml");

        Assert.Equal(
            "# Requisitos de reservas\n\n- De: \"Ana Gómez\" <ana@example.com>\n\n- Para: \"Luis Pérez\" <luis@example.com>\n\n"
            + "- Fecha: 2026-10-14 09:30\n\nLas reservas duran como mucho 4 horas.\n\n- Adjunto: salas.xlsx",
            text);
    }

    [Fact]
    public void Eml_with_html_body_only_is_converted()
    {
        var eml = "Subject: Prueba\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<p>Hola</p><p>Adiós</p>\r\n";

        Assert.Equal("# Prueba\n\nHola\n\nAdiós", Extract(eml, ".eml"));
    }

    // A real .msg (an Outlook compound file) cannot be built in a test; a file
    // that is not one must fail like any other damaged document.
    [Theory]
    [InlineData(".msg")]
    [InlineData(".xlsx")]
    [InlineData(".pptx")]
    public void Damaged_files_throw_extraction_exception(string extension)
    {
        var notADocument = "esto no es un documento"u8.ToArray();

        Assert.Throws<TextExtractionException>(() => TextExtractor.Extract(notADocument, extension));
    }

    [Fact]
    public void Vtt_keeps_who_speaks_and_joins_their_consecutive_cues()
    {
        var vtt = """
            WEBVTT

            NOTE generado por la herramienta de reuniones

            1
            00:00:01.000 --> 00:00:04.000
            <v Ana Gómez>Necesitamos reservar</v>

            2
            00:00:04.000 --> 00:00:06.000
            <v Ana Gómez>salas por horas.</v>

            3
            00:00:06.500 --> 00:00:08.000 align:start
            <v.equipo Luis Pérez>¿Y cancelar &amp; modificar?</v>
            """;

        Assert.Equal(
            "Ana Gómez: Necesitamos reservar salas por horas.\n\nLuis Pérez: ¿Y cancelar & modificar?",
            Extract(vtt, ".vtt"));
    }

    [Fact]
    public void Srt_drops_numbers_and_timings()
    {
        var srt = "1\r\n00:00:01,000 --> 00:00:03,000\r\nEl empleado reserva\r\n<i>una sala.</i>\r\n\r\n2\r\n00:00:03,500 --> 00:00:05,000\r\nY la cancela.\r\n";

        Assert.Equal("El empleado reserva una sala. Y la cancela.", Extract(srt, ".srt"));
    }

    [Theory]
    [InlineData(".json", "{\n  \"sala\": \"Azul\"\n}")]
    [InlineData(".yaml", "sala: Azul\ncapacidad: 12")]
    [InlineData(".yml", "- Azul\n- Roja")]
    [InlineData(".xml", "<salas>\n  <sala>Azul</sala>\n</salas>")]
    public void Data_files_are_kept_as_they_are(string extension, string content)
    {
        Assert.Equal(content, Extract(content, extension));
    }

    // ---- Spreadsheets ----

    private static S.Cell Text(string reference, string text) =>
        new(new S.InlineString(new S.Text(text))) { CellReference = reference, DataType = S.CellValues.InlineString };

    private static S.Cell Number(string reference, string value) =>
        new(new S.CellValue(value)) { CellReference = reference };

    // Style 1 of the workbook has the built-in date format 14.
    private static S.Cell Date(string reference, string serial) =>
        new(new S.CellValue(serial)) { CellReference = reference, StyleIndex = 1 };

    private static byte[] BuildXlsx(params (string Name, S.Cell[][] Rows)[] sheets)
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbook = document.AddWorkbookPart();
            workbook.Workbook = new S.Workbook(new S.Sheets());
            var styles = workbook.AddNewPart<WorkbookStylesPart>();
            styles.Stylesheet = new S.Stylesheet(new S.CellFormats(
                new S.CellFormat { NumberFormatId = 0 },
                new S.CellFormat { NumberFormatId = 14 }));

            uint sheetId = 1;
            foreach (var (name, rows) in sheets)
            {
                var part = workbook.AddNewPart<WorksheetPart>();
                part.Worksheet = new S.Worksheet(new S.SheetData(rows.Select(cells => new S.Row(cells))));
                workbook.Workbook.Sheets!.Append(new S.Sheet { Name = name, SheetId = sheetId++, Id = workbook.GetIdOfPart(part) });
            }
        }
        return stream.ToArray();
    }

    // ---- Presentations ----

    // One slide per (title, paragraphs of the body, notes); an empty title
    // means a slide without a title placeholder.
    private static byte[] BuildPptx(params (string Title, string[] Body, string? Notes)[] slides)
    {
        using var stream = new MemoryStream();
        using (var document = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
        {
            var presentation = document.AddPresentationPart();
            presentation.Presentation = new P.Presentation(new P.SlideIdList());
            uint id = 256;
            foreach (var (title, body, notes) in slides)
            {
                var slide = presentation.AddNewPart<SlidePart>();
                var tree = new P.ShapeTree();
                if (title.Length > 0)
                    tree.Append(Shape(P.PlaceholderValues.Title, [title]));
                tree.Append(Shape(null, body));
                slide.Slide = new P.Slide(new P.CommonSlideData(tree));
                if (notes is not null)
                {
                    var notesPart = slide.AddNewPart<NotesSlidePart>();
                    notesPart.NotesSlide = new P.NotesSlide(new P.CommonSlideData(new P.ShapeTree(
                        Shape(P.PlaceholderValues.SlideImage, []),
                        Shape(P.PlaceholderValues.Body, [notes]))));
                }
                presentation.Presentation.SlideIdList!.Append(new P.SlideId { Id = id++, RelationshipId = presentation.GetIdOfPart(slide) });
            }
        }
        return stream.ToArray();
    }

    private static P.Shape Shape(P.PlaceholderValues? placeholder, string[] paragraphs)
    {
        var properties = new P.ApplicationNonVisualDrawingProperties();
        if (placeholder is { } type)
            properties.Append(new P.PlaceholderShape { Type = type });
        var text = new P.TextBody(new A.BodyProperties());
        text.Append(paragraphs.Select(p => new A.Paragraph(new A.Run(new A.Text(p)))));
        return new P.Shape(
            new P.NonVisualShapeProperties(new P.NonVisualDrawingProperties { Id = 1, Name = "forma" }, new P.NonVisualShapeDrawingProperties(), properties),
            new P.ShapeProperties(),
            text);
    }
}
