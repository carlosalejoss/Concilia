using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace Concilia.Core.Files;

// A presentation (.pptx) slide by slide: a "## Diapositiva N: title" heading,
// each paragraph of the slide as a list item (slides are mostly bullet
// points), its tables as table rows and, at the end, the speaker notes,
// where the explanation of a slide often is.
internal static class SlidesText
{
    public static string FromPptx(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var document = PresentationDocument.Open(stream, isEditable: false);
        var presentation = document.PresentationPart;
        var slideIds = presentation?.Presentation?.SlideIdList?.Elements<P.SlideId>() ?? [];

        var blocks = new List<string>();
        var number = 0;
        foreach (var slideId in slideIds)
        {
            number++;
            if (slideId.RelationshipId?.Value is not { } partId || presentation!.GetPartById(partId) is not SlidePart slide)
            {
                continue;
            }

            string? title = null;
            var body = new List<string>();
            // In the order of the slide's shape tree, which is the order in which they are drawn.
            foreach (var element in slide.Slide?.CommonSlideData?.ShapeTree?.Descendants() ?? [])
            {
                if (element is P.Shape shape)
                {
                    var paragraphs = Paragraphs(shape);
                    if (title is null && IsTitle(shape))
                        title = string.Join(" ", paragraphs);
                    else
                        body.AddRange(paragraphs.Select(p => "- " + p));
                }
                else if (element is A.Table table)
                {
                    body.AddRange(table.Elements<A.TableRow>()
                        .Select(row => MarkdownText.Row(row.Elements<A.TableCell>().Select(cell => cell.InnerText))));
                }
            }

            blocks.Add(string.IsNullOrEmpty(title) ? $"## Diapositiva {number}" : $"## Diapositiva {number}: {title}");
            blocks.AddRange(body);
            var notes = Notes(slide);
            if (notes.Length > 0)
                blocks.Add($"Notas: {notes}");
        }
        return MarkdownText.Blocks(blocks);
    }

    private static List<string> Paragraphs(P.Shape shape) =>
        shape.TextBody?.Elements<A.Paragraph>()
            .Select(p => MarkdownText.OneLine(p.InnerText))
            .Where(text => text.Length > 0)
            .ToList() ?? [];

    private static P.PlaceholderValues? PlaceholderType(P.Shape shape) =>
        shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.PlaceholderShape?.Type?.Value;

    private static bool IsTitle(P.Shape shape) =>
        PlaceholderType(shape) is { } type && (type == P.PlaceholderValues.Title || type == P.PlaceholderValues.CenteredTitle);

    // The notes page also has the slide's image and its number; the text the
    // speaker wrote is in the body placeholder.
    private static string Notes(SlidePart slide)
    {
        var shapes = slide.NotesSlidePart?.NotesSlide?.Descendants<P.Shape>() ?? [];
        var text = shapes
            .Where(shape => PlaceholderType(shape) is { } type && type == P.PlaceholderValues.Body)
            .SelectMany(Paragraphs);
        return string.Join(" ", text);
    }
}
