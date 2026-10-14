using System.Text;
using HtmlAgilityPack;

namespace Concilia.Core.Files;

// A web page (.html, or the HTML body of an email) as text: headings as "#"
// headings, list items as "- " items, table rows as table rows and every
// other block (paragraph, div...) as a paragraph. Scripts, styles and the
// page's head are left out: they are not content.
internal static class HtmlText
{
    private static readonly HashSet<string> Skipped =
        ["head", "title", "script", "style", "noscript", "template", "svg", "iframe", "object"];

    // Elements that start and end a block; the rest (span, a, strong...) are
    // part of the text around them.
    private static readonly HashSet<string> BlockElements =
    [
        "html", "body", "p", "div", "section", "article", "header", "footer", "main", "nav", "aside",
        "blockquote", "pre", "ul", "ol", "dl", "dt", "dd", "figure", "figcaption", "form", "fieldset",
        "legend", "address", "details", "summary", "caption", "hr",
    ];

    public static string Convert(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var writer = new Writer();
        var title = Clean(document.DocumentNode.SelectSingleNode("//title")?.InnerText ?? "");
        if (title.Length > 0)
            writer.Blocks.Add($"# {title}");
        writer.Walk(document.DocumentNode);
        writer.Flush();
        return MarkdownText.Blocks(writer.Blocks);
    }

    // Entities (&amp;, &nbsp;...) decoded and whitespace collapsed, as a browser shows it.
    private static string Clean(string text) => MarkdownText.OneLine(HtmlEntity.DeEntitize(text));

    // Goes through the page collecting the text of the current block until a
    // block ends (Flush).
    private sealed class Writer
    {
        public List<string> Blocks { get; } = [];
        private readonly StringBuilder text = new();
        private string prefix = ""; // "- " inside a list item

        public void Walk(HtmlNode node)
        {
            foreach (var child in node.ChildNodes)
            {
                if (child is HtmlTextNode textNode)
                    text.Append(textNode.Text);
                else if (child.NodeType == HtmlNodeType.Element)
                    Element(child); // comments are left out
            }
        }

        private void Element(HtmlNode node)
        {
            var name = node.Name.ToLowerInvariant();
            if (Skipped.Contains(name))
                return;

            if (name is "h1" or "h2" or "h3" or "h4" or "h5" or "h6")
            {
                Flush();
                var heading = Clean(node.InnerText);
                if (heading.Length > 0)
                    Blocks.Add($"{new string('#', name[1] - '0')} {heading}");
            }
            else if (name == "table")
            {
                Flush();
                foreach (var row in node.SelectNodes(".//tr") ?? Enumerable.Empty<HtmlNode>())
                {
                    var cells = row.ChildNodes.Where(c => c.Name is "td" or "th").Select(c => Clean(c.InnerText)).ToList();
                    if (cells.Any(c => c.Length > 0))
                        Blocks.Add(MarkdownText.Row(cells));
                }
            }
            else if (name == "br")
                Flush();
            else if (name == "li")
            {
                Flush();
                prefix = "- ";
                Walk(node);
                Flush();
                prefix = ""; // an empty item does not pass its "- " on
            }
            else if (BlockElements.Contains(name))
            {
                Flush();
                Walk(node);
                Flush();
            }
            else
                Walk(node);
        }

        // Ends the current block. The "- " of a list item waits for its first
        // text, which may come inside a <p> of the item.
        public void Flush()
        {
            var block = Clean(text.ToString());
            text.Clear();
            if (block.Length > 0)
            {
                Blocks.Add(prefix + block);
                prefix = "";
            }
        }
    }
}
