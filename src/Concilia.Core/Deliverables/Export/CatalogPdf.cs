using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

namespace Concilia.Core.Deliverables.Export;

// The catalog as a PDF (step 96), with MigraDoc 6.2.4 (MIT; see the .csproj),
// which lays out paragraphs, page breaks and the footer on top of PDFsharp.
// The same content and order as CatalogDocx; the headings become the
// bookmarks of the PDF.
public static class CatalogPdf
{
    private static readonly Color Grey = new(0x5F, 0x6B, 0x66);
    private static readonly Color Warn = new(0xB4, 0x53, 0x09);

    public static byte[] Write(CatalogExport c)
    {
        ExportFonts.Register();
        var document = new Document();
        document.Info.Title = $"{CatalogExport.Title}: {c.Project}";
        document.Info.Author = "Concilia";
        DefineStyles(document);

        var page = document.AddSection();
        page.PageSetup.PageFormat = PageFormat.A4;
        page.PageSetup.TopMargin = page.PageSetup.BottomMargin = Unit.FromCentimeter(2.5);
        page.PageSetup.LeftMargin = page.PageSetup.RightMargin = Unit.FromCentimeter(2.5);
        var footer = page.Footers.Primary.AddParagraph();
        footer.Style = "Pie";
        footer.AddText($"{c.Footer} · página ");
        footer.AddPageField();
        footer.AddText(" de ");
        footer.AddNumPagesField();

        Cover(page, c);
        foreach (var section in c.Sections)
        {
            page.AddParagraph($"{section.Title} ({section.Requirements.Count})", StyleNames.Heading1);
            foreach (var r in section.Requirements)
                Requirement(page, r);
        }
        Sources(page, c);
        Questions(page, c);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);
        return stream.ToArray();
    }

    private static void Cover(Section page, CatalogExport c)
    {
        page.AddParagraph(CatalogExport.Title, "Titulo");
        page.AddParagraph(c.Project, "Subtitulo");
        var status = page.AddParagraph();
        var text = status.AddFormattedText(c.Status, TextFormat.Bold);
        if (!c.Validated)
            text.Color = Warn;
        page.AddParagraph(c.Generated, "Detalle");
        page.AddParagraph().AddFormattedText($"{c.Total} requisitos:", TextFormat.Bold);
        foreach (var section in c.Sections)
            page.AddParagraph($"{section.Title}: {section.Requirements.Count}", "Lista");
        page.AddParagraph(CatalogExport.Legend);
        page.AddPageBreak();
    }

    private static void Requirement(Section page, ExportRequirement r)
    {
        page.AddParagraph(r.Id, StyleNames.Heading2);
        var text = page.AddParagraph();
        text.Format.KeepWithNext = true; // with its details, on the same page
        AddLines(text, r.Text);
        var references = CatalogExport.References(r);
        page.AddParagraph(references.Length > 0 ? $"{CatalogExport.Details(r)} · {references}" : CatalogExport.Details(r), "Detalle");
        if (r.AiGuess)
            page.AddParagraph(CatalogExport.AiGuessMark, "Detalle").Format.Font.Color = Warn;
        if (r.PendingDiscrepancy)
            page.AddParagraph(CatalogExport.DiscrepancyMark, "Detalle").Format.Font.Color = Warn;
    }

    private static void Sources(Section page, CatalogExport c)
    {
        page.AddParagraph(CatalogExport.SourcesTitle, StyleNames.Heading1).Format.PageBreakBefore = true;
        if (c.Sources.Count == 0)
            page.AddParagraph("Ningún requisito cita fragmentos de la documentación.");
        foreach (var document in c.Sources.GroupBy(s => s.Document))
        {
            page.AddParagraph(document.Key, StyleNames.Heading2);
            foreach (var s in document)
            {
                var paragraph = page.AddParagraph();
                paragraph.AddFormattedText(s.Id, TextFormat.Bold);
                if (s.Section.Length > 0)
                    paragraph.AddFormattedText($" ({s.Section})").Color = Grey;
                paragraph.AddText(" ");
                if (s.Text is null)
                    paragraph.AddFormattedText("No está en la documentación de la que salió el catálogo.", TextFormat.Italic);
                else
                    AddLines(paragraph, s.Text);
            }
        }
    }

    private static void Questions(Section page, CatalogExport c)
    {
        page.AddParagraph(CatalogExport.QuestionsTitle, StyleNames.Heading1).Format.PageBreakBefore = true;
        if (c.Questions.Count == 0)
            page.AddParagraph("Ningún requisito cita preguntas al cliente.");
        foreach (var q in c.Questions)
        {
            var question = page.AddParagraph("", "Pregunta");
            question.AddFormattedText(q.Id, TextFormat.Bold);
            AddLines(question, $" {q.Text}");
            var answer = page.AddParagraph();
            AddLines(answer, CatalogExport.Answer(q));
            if (q.AiGuess)
                answer.Format.Font.Color = Warn;
        }
    }

    // Text that may have line breaks (an answer of the user): MigraDoc needs
    // them as line breaks, and control characters would not show.
    private static void AddLines(Paragraph paragraph, string text)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                paragraph.AddLineBreak();
            paragraph.AddText(new string(lines[i].Where(ch => ch == '\t' || ch >= ' ').ToArray()));
        }
    }

    // Sizes and spacing as in CatalogDocx, in points.
    private static void DefineStyles(Document document)
    {
        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = ExportFonts.Family;
        normal.Font.Size = 10.5;
        normal.ParagraphFormat.SpaceAfter = 6;
        normal.ParagraphFormat.LineSpacingRule = LineSpacingRule.Multiple;
        normal.ParagraphFormat.LineSpacing = 1.1;

        var heading1 = document.Styles[StyleNames.Heading1]!;
        heading1.Font.Size = 16;
        heading1.Font.Bold = true;
        heading1.ParagraphFormat.SpaceBefore = 18;
        heading1.ParagraphFormat.SpaceAfter = 6;
        heading1.ParagraphFormat.KeepWithNext = true;

        var heading2 = document.Styles[StyleNames.Heading2]!;
        heading2.Font.Size = 12;
        heading2.Font.Bold = true;
        heading2.ParagraphFormat.SpaceBefore = 12;
        heading2.ParagraphFormat.SpaceAfter = 3;
        heading2.ParagraphFormat.KeepWithNext = true;

        Add(document, "Titulo", size: 26, bold: true, spaceAfter: 6);
        Add(document, "Subtitulo", size: 16, spaceAfter: 18);
        Add(document, "Detalle", size: 9, color: Grey);
        Add(document, "Lista", spaceAfter: 2).ParagraphFormat.LeftIndent = Unit.FromCentimeter(0.6);
        Add(document, "Pregunta", spaceBefore: 10, spaceAfter: 2).ParagraphFormat.KeepWithNext = true;
        Add(document, "Pie", size: 8, color: Grey);
    }

    private static Style Add(Document document, string name, double? size = null, bool bold = false, Color? color = null,
        double? spaceBefore = null, double? spaceAfter = null)
    {
        var style = document.Styles.AddStyle(name, StyleNames.Normal);
        if (size is not null)
            style.Font.Size = size.Value;
        if (bold)
            style.Font.Bold = true;
        if (color is not null)
            style.Font.Color = color.Value;
        if (spaceBefore is not null)
            style.ParagraphFormat.SpaceBefore = spaceBefore.Value;
        if (spaceAfter is not null)
            style.ParagraphFormat.SpaceAfter = spaceAfter.Value;
        return style;
    }
}
