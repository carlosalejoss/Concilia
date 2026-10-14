using System.Text.RegularExpressions;

namespace Concilia.Core.Files;

// The converters of spreadsheets, slides, web pages, emails and transcripts
// write their text with a few Markdown marks that both the fragmenter and the
// web reader (frontend/src/DocumentText.tsx) understand: "## " headings, "- "
// list items and "| a | b |" table rows. Blocks go apart, with a blank line
// between them: the fragmenter joins consecutive lines into one paragraph, so
// the blank line keeps each block (each table row, each item) a fragment of
// its own that the model can cite. The reader skips blank lines, so the rows
// still make one table.
internal static class MarkdownText
{
    private static readonly Regex Whitespace = new(@"\s+");

    public static string Blocks(IEnumerable<string> blocks) =>
        string.Join("\n\n", blocks.Where(b => b.Length > 0));

    public static string Row(IEnumerable<string> cells) =>
        "| " + string.Join(" | ", cells.Select(Cell)) + " |";

    // A cell on one line, without "|", which would split it in two columns.
    public static string Cell(string text) => OneLine(text).Replace('|', '/');

    // Spaces, tabs and line breaks collapsed into one space.
    public static string OneLine(string text) => Whitespace.Replace(text, " ").Trim();
}
