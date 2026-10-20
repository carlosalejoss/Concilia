using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Concilia.Core.Deliverables.Export;

// The catalog as a Word document (step 96), with DocumentFormat.OpenXml (MIT),
// the library that already reads .docx. A4, a cover, a Heading 1 per type and
// a Heading 2 per requirement (Word's navigation pane lists them), the two
// annexes and a footer with the page number. Calibri, which any Word has; the
// PDF carries its own font.
public static class CatalogDocx
{
    private const string Grey = "5F6B66";
    private const string Warn = "B45309";

    public static byte[] Write(CatalogExport c)
    {
        using var stream = new MemoryStream();
        using (var word = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = word.AddMainDocumentPart();
            main.AddNewPart<StyleDefinitionsPart>().Styles = Styles();
            var footer = main.AddNewPart<FooterPart>();
            footer.Footer = new Footer(Paragraph("Pie", Run($"{c.Footer} · página "), PageNumber()));

            var body = new Body();
            Cover(body, c);
            foreach (var section in c.Sections)
            {
                body.Append(Paragraph("Heading1", Run($"{section.Title} ({section.Requirements.Count})")));
                foreach (var r in section.Requirements)
                    Requirement(body, r);
            }
            Sources(body, c);
            Questions(body, c);
            body.Append(new SectionProperties(
                new FooterReference { Type = HeaderFooterValues.Default, Id = main.GetIdOfPart(footer) },
                new PageSize { Width = 11906, Height = 16838 }, // A4, in twentieths of a point
                new PageMargin { Top = 1418, Right = 1418, Bottom = 1418, Left = 1418, Header = 708, Footer = 708, Gutter = 0 }));
            main.Document = new Document(body);
        }
        return stream.ToArray();
    }

    private static void Cover(Body body, CatalogExport c)
    {
        body.Append(Paragraph("Title", Run(CatalogExport.Title)));
        body.Append(Paragraph("Subtitulo", Run(c.Project)));
        body.Append(Paragraph(null, Run(c.Status, bold: true, color: c.Validated ? null : Warn)));
        body.Append(Paragraph("Detalle", Run(c.Generated)));
        body.Append(Paragraph(null, Run($"{c.Total} requisitos:", bold: true)));
        foreach (var section in c.Sections)
            body.Append(Paragraph("Lista", Run($"{section.Title}: {section.Requirements.Count}")));
        body.Append(Paragraph(null, Run(CatalogExport.Legend)));
        body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
    }

    private static void Requirement(Body body, ExportRequirement r)
    {
        body.Append(Paragraph("Heading2", Run(r.Id)));
        body.Append(Paragraph(null, new ParagraphProperties { KeepNext = new KeepNext() }, Run(r.Text))); // with its details, on the same page
        var references = CatalogExport.References(r);
        body.Append(Paragraph("Detalle", Run(references.Length > 0 ? $"{CatalogExport.Details(r)} · {references}" : CatalogExport.Details(r))));
        if (r.AiGuess)
            body.Append(Paragraph("Detalle", Run(CatalogExport.AiGuessMark, color: Warn)));
        if (r.PendingDiscrepancy)
            body.Append(Paragraph("Detalle", Run(CatalogExport.DiscrepancyMark, color: Warn)));
    }

    private static void Sources(Body body, CatalogExport c)
    {
        body.Append(Paragraph("Heading1", PageBreakBefore(), Run(CatalogExport.SourcesTitle)));
        if (c.Sources.Count == 0)
            body.Append(Paragraph(null, Run("Ningún requisito cita fragmentos de la documentación.")));
        foreach (var document in c.Sources.GroupBy(s => s.Document))
        {
            body.Append(Paragraph("Heading2", Run(document.Key)));
            foreach (var s in document)
            {
                var section = s.Section.Length > 0 ? $" ({s.Section})" : "";
                body.Append(Paragraph(null,
                    Run(s.Id, bold: true), Run(section, color: Grey), Run(" "),
                    s.Text is null ? Run("No está en la documentación de la que salió el catálogo.", italic: true) : Run(s.Text)));
            }
        }
    }

    private static void Questions(Body body, CatalogExport c)
    {
        body.Append(Paragraph("Heading1", PageBreakBefore(), Run(CatalogExport.QuestionsTitle)));
        if (c.Questions.Count == 0)
            body.Append(Paragraph(null, Run("Ningún requisito cita preguntas al cliente.")));
        foreach (var q in c.Questions)
        {
            body.Append(Paragraph("Pregunta", Run(q.Id, bold: true), Run($" {q.Text}")));
            body.Append(Paragraph(null, Run(CatalogExport.Answer(q), color: q.AiGuess ? Warn : null)));
        }
    }

    // A paragraph of a style (null: Normal) with these runs; a
    // ParagraphProperties among them goes first.
    private static Paragraph Paragraph(string? style, params OpenXmlElement[] content)
    {
        var properties = content.OfType<ParagraphProperties>().FirstOrDefault() ?? new ParagraphProperties();
        if (style is not null)
            properties.ParagraphStyleId = new ParagraphStyleId { Val = style };
        var paragraph = new Paragraph(properties);
        paragraph.Append(content.Where(e => e is not ParagraphProperties));
        return paragraph;
    }

    private static ParagraphProperties PageBreakBefore() => new() { PageBreakBefore = new PageBreakBefore() };

    // A run of text; its line breaks become Word line breaks.
    private static Run Run(string text, bool bold = false, bool italic = false, string? color = null)
    {
        var run = new Run();
        if (bold || italic || color is not null)
        {
            run.RunProperties = new RunProperties
            {
                Bold = bold ? new Bold() : null,
                Italic = italic ? new Italic() : null,
                Color = color is null ? null : new Color { Val = color },
            };
        }
        var lines = XmlSafe(text).ReplaceLineEndings("\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                run.Append(new Break());
            run.Append(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
        }
        return run;
    }

    // The PAGE field: Word fills in the number of each page.
    private static SimpleField PageNumber() => new(Run("1")) { Instruction = " PAGE " };

    // XML 1.0 does not allow control characters; a model may have written one.
    private static string XmlSafe(string text) =>
        new(text.Where(ch => ch is '\t' or '\n' or '\r' || ch >= ' ').ToArray());

    private static Styles Styles()
    {
        var styles = new Styles(new DocDefaults(
            new RunPropertiesDefault(new RunPropertiesBaseStyle(
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", ComplexScript = "Calibri", EastAsia = "Calibri" },
                new FontSize { Val = "22" }, new FontSizeComplexScript { Val = "22" },
                new Languages { Val = "es-ES" })),
            new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle(
                new SpacingBetweenLines { After = "120", Line = "264", LineRule = LineSpacingRuleValues.Auto }))));
        styles.Append(
            Style("Normal", "Normal", isDefault: true),
            Style("Title", "Title", size: 52, bold: true, spaceAfter: 120),
            Style("Subtitulo", "Subtítulo", size: 32, spaceAfter: 360),
            Style("Heading1", "heading 1", size: 32, bold: true, spaceBefore: 360, outline: 0, keepNext: true),
            Style("Heading2", "heading 2", size: 24, bold: true, spaceBefore: 240, spaceAfter: 60, outline: 1, keepNext: true),
            Style("Detalle", "Detalle", size: 18, color: Grey),
            Style("Lista", "Lista", indent: 360, spaceAfter: 40),
            Style("Pregunta", "Pregunta", spaceBefore: 200, spaceAfter: 40, keepNext: true),
            Style("Pie", "Pie", size: 16, color: Grey));
        return styles;
    }

    private static Style Style(string id, string name, bool isDefault = false, int? size = null, bool bold = false, string? color = null,
        int? spaceBefore = null, int? spaceAfter = null, int? outline = null, bool keepNext = false, int? indent = null)
    {
        // Each attribute only if it has a value: assigning null leaves it empty, which is not valid.
        var spacing = spaceBefore is null && spaceAfter is null ? null : new SpacingBetweenLines();
        if (spaceBefore is not null)
            spacing!.Before = spaceBefore.ToString();
        if (spaceAfter is not null)
            spacing!.After = spaceAfter.ToString();
        var style = new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = id,
            Default = isDefault ? true : null,
            StyleName = new StyleName { Val = name },
            BasedOn = isDefault ? null : new BasedOn { Val = "Normal" },
            PrimaryStyle = new PrimaryStyle(),
            StyleParagraphProperties = new StyleParagraphProperties
            {
                KeepNext = keepNext ? new KeepNext() : null,
                SpacingBetweenLines = spacing,
                Indentation = indent is null ? null : new Indentation { Left = indent.ToString() },
                OutlineLevel = outline is null ? null : new OutlineLevel { Val = outline },
            },
            StyleRunProperties = new StyleRunProperties
            {
                Bold = bold ? new Bold() : null,
                Color = color is null ? null : new Color { Val = color },
                FontSize = size is null ? null : new FontSize { Val = size.ToString() },
            },
        };
        return style;
    }
}
