using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Concilia.Core.Files;

// Spreadsheets (.xlsx and .csv) as Markdown tables: each sheet under a
// "## Hoja: name" heading and each row as a table row. A sheet gives at most
// MaxRows rows; the rest are left out with a line that says how many
// (decision of 14-10-2026), so a large export does not fill the budget.
public static class SpreadsheetText
{
    public const int MaxRows = 2_000;

    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

    // Quoted texts, [colour] or [locale] sections and escaped characters of a
    // number format: they may contain letters that are not date parts.
    private static readonly Regex FormatLiterals = new(@"""[^""]*""|\[[^\]]*\]|\\.");

    // A .csv has a single table. Spanish Excel saves it with ";" (the comma is
    // the decimal separator); other tools with "," or tabs.
    internal static string FromCsv(string text)
    {
        var firstLine = text.Split('\n', 2)[0];
        var delimiter = new[] { ',', ';', '\t' }.MaxBy(d => firstLine.Count(c => c == d));
        return MarkdownText.Blocks(Table(ParseCsv(text, delimiter)));
    }

    // RFC 4180: a cell between quotes can hold the delimiter, line breaks and
    // doubled quotes ("" stands for one ").
    private static List<List<string>> ParseCsv(string text, char delimiter)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c != '"')
                    cell.Append(c);
                else if (i + 1 < text.Length && text[i + 1] == '"')
                    cell.Append(text[++i]);
                else
                    quoted = false;
            }
            else if (c == '"' && cell.Length == 0)
                quoted = true;
            else if (c == delimiter)
            {
                row.Add(cell.ToString());
                cell.Clear();
            }
            else if (c == '\n')
            {
                row.Add(cell.ToString());
                cell.Clear();
                rows.Add(row);
                row = [];
            }
            else
                cell.Append(c);
        }
        row.Add(cell.ToString());
        rows.Add(row);
        return rows;
    }

    // A .xlsx is a ZIP of XML files: the workbook lists the sheets in order,
    // each sheet has its rows, and texts are usually in a shared table that
    // the cells point to by index.
    internal static string FromXlsx(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var document = SpreadsheetDocument.Open(stream, isEditable: false);
        var workbook = document.WorkbookPart;
        if (workbook?.Workbook?.Sheets is null)
        {
            return "";
        }

        var shared = workbook.SharedStringTablePart?.SharedStringTable?.Elements<SharedStringItem>()
            .Select(item => item.InnerText).ToList() ?? [];
        var dateStyles = DateStyles(workbook);

        var blocks = new List<string>();
        foreach (var sheet in workbook.Workbook.Sheets.Elements<Sheet>())
        {
            // Chart sheets have no cells.
            if (sheet.Id?.Value is not { } partId || workbook.GetPartById(partId) is not WorksheetPart part)
            {
                continue;
            }
            blocks.Add($"## Hoja: {sheet.Name?.Value}");
            var rows = part.Worksheet?.Descendants<Row>().Select(row => RowCells(row, shared, dateStyles)) ?? [];
            blocks.AddRange(Table(rows));
        }
        return MarkdownText.Blocks(blocks);
    }

    // The rows of one table, without empty rows or empty cells at the end,
    // and at most MaxRows of them.
    private static IEnumerable<string> Table(IEnumerable<List<string>> rows)
    {
        var shown = 0;
        var omitted = 0;
        foreach (var row in rows)
        {
            var last = row.FindLastIndex(cell => cell.Trim().Length > 0);
            if (last < 0)
            {
                continue;
            }
            if (shown == MaxRows)
            {
                omitted++;
                continue;
            }
            shown++;
            yield return MarkdownText.Row(row.Take(last + 1));
        }
        if (omitted > 0)
        {
            yield return $"(Se han omitido {omitted.ToString("N0", Spanish)} filas: de cada hoja solo se leen las primeras {MaxRows.ToString("N0", Spanish)}.)";
        }
    }

    // The cells of a row in their columns: a cell says where it goes ("C7")
    // and empty cells are not stored, so the gaps are filled with "".
    private static List<string> RowCells(Row row, List<string> shared, HashSet<uint> dateStyles)
    {
        var cells = new List<string>();
        foreach (var cell in row.Elements<Cell>())
        {
            if (cell.CellReference?.Value is { } reference)
            {
                var column = ColumnIndex(reference);
                while (cells.Count < column)
                    cells.Add("");
            }
            cells.Add(CellText(cell, shared, dateStyles));
        }
        return cells;
    }

    // "C7" -> 2: the letters are a number in base 26 (A = 1 ... Z = 26), from 0.
    private static int ColumnIndex(string reference)
    {
        var index = 0;
        foreach (var c in reference.TakeWhile(char.IsLetter))
            index = index * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
        return index - 1;
    }

    // A formula cell keeps its last result, which is what we take.
    private static string CellText(Cell cell, List<string> shared, HashSet<uint> dateStyles)
    {
        var raw = cell.CellValue?.Text ?? "";
        var type = cell.DataType?.Value;
        if (type == CellValues.SharedString)
            return int.TryParse(raw, out var i) && i < shared.Count ? shared[i] : "";
        if (type == CellValues.InlineString)
            return cell.InlineString?.InnerText ?? "";
        if (type == CellValues.Boolean)
            return raw == "1" ? "VERDADERO" : "FALSO";
        // Excel stores a date as the number of days since 1900; only the cell's
        // format says it is a date.
        if ((type is null || type == CellValues.Number) && dateStyles.Contains(cell.StyleIndex?.Value ?? 0)
            && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial))
            return FormatDate(serial);
        return raw;
    }

    private static string FormatDate(double serial)
    {
        var date = DateTime.FromOADate(serial);
        if (date.TimeOfDay == TimeSpan.Zero)
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return serial < 1
            ? date.ToString("HH:mm", CultureInfo.InvariantCulture)
            : date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    // The indexes of the cell styles that show a date or a time: the built-in
    // formats 14 to 22 and 45 to 47, and the workbook's own formats with day,
    // year or hour parts ("dd/mm/yyyy").
    private static HashSet<uint> DateStyles(WorkbookPart workbook)
    {
        var stylesheet = workbook.WorkbookStylesPart?.Stylesheet;
        if (stylesheet?.CellFormats is null)
        {
            return [];
        }

        var customDates = stylesheet.NumberingFormats?.Elements<NumberingFormat>()
            .Where(f => f.NumberFormatId?.Value is not null && IsDateFormat(f.FormatCode?.Value ?? ""))
            .Select(f => f.NumberFormatId!.Value)
            .ToHashSet() ?? [];

        var styles = new HashSet<uint>();
        uint index = 0;
        foreach (var format in stylesheet.CellFormats.Elements<CellFormat>())
        {
            var id = format.NumberFormatId?.Value ?? 0;
            if (id is >= 14 and <= 22 or >= 45 and <= 47 || customDates.Contains(id))
                styles.Add(index);
            index++;
        }
        return styles;
    }

    private static bool IsDateFormat(string code) =>
        FormatLiterals.Replace(code, "").IndexOfAny(['d', 'D', 'y', 'Y', 'h', 'H']) >= 0;
}
