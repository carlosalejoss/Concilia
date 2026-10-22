using System.Text;

namespace Concilia.Core.Deliverables.Export;

// The backlog as CSV to import it into Jira or Azure DevOps (step 98).
// Decisions of the author (22-10-2026): one file per tool, because each one
// expects other columns, and in Azure DevOps the Agile process (Epic → User
// Story → Task), the default one.
//
// Both: comma-separated, every field in quotes (RFC 4180), UTF-8 without BOM
// (a BOM would become part of the first column name), a row per epic, story
// and task, each one after its parent. The ids of Concilia go as labels or
// tags, so every item can be traced back; a story with a discrepancy not
// reviewed gets one more, "discrepancia-sin-revisar".
public static class BacklogCsv
{
    public const string PendingDiscrepancyLabel = "discrepancia-sin-revisar";

    // Jira (Cloud and Data Center, "Import issues from a CSV file"): the
    // hierarchy goes with "Issue ID", a number of this file, and "Parent ID",
    // the number of its parent: the epic of a story and the story of a
    // sub-task. "Epic Name" is still required for an epic in some company-managed
    // projects; elsewhere the import assistant lets it be left unmapped. A
    // field with several values (Labels) repeats its column, one value each.
    public static byte[] Jira(BacklogExport backlog)
    {
        var rows = new List<(string Id, string Parent, string Type, string Summary, string EpicName, string Description, List<string> Labels)>();
        var next = 0;
        foreach (var epic in backlog.Epics)
        {
            var epicId = (++next).ToString();
            rows.Add((epicId, "", "Epic", epic.Title, epic.Title, epic.Description, [epic.Id]));
            foreach (var story in epic.Stories)
            {
                var storyId = (++next).ToString();
                rows.Add((storyId, epicId, "Story", story.Title, "", JiraStory(story), StoryLabels(story)));
                foreach (var task in story.Tasks)
                    rows.Add(((++next).ToString(), storyId, "Sub-task", task.Title, "", "", [task.Id]));
            }
        }

        var labels = rows.Max(r => r.Labels.Count);
        var csv = new StringBuilder();
        Row(csv, ["Issue ID", "Parent ID", "Issue Type", "Summary", "Epic Name", "Description", .. Enumerable.Repeat("Labels", labels)]);
        foreach (var r in rows)
            Row(csv, [r.Id, r.Parent, r.Type, r.Summary, r.EpicName, r.Description, .. r.Labels, .. Enumerable.Repeat("", labels - r.Labels.Count)]);
        return Utf8(csv);
    }

    // Azure DevOps (Boards, "Import work items from a CSV file"): "ID" empty,
    // so each row is a new work item, and the hierarchy with the title in the
    // column of its level: Title 1 the epic, Title 2 its user stories and
    // Title 3 their tasks. Description and Acceptance Criteria are HTML fields:
    // the text goes escaped, with <br> between lines. Tags are separated by "; ".
    public static byte[] AzureDevOps(BacklogExport backlog)
    {
        var csv = new StringBuilder();
        Row(csv, ["ID", "Work Item Type", "Title 1", "Title 2", "Title 3", "Description", "Acceptance Criteria", "Tags"]);
        foreach (var epic in backlog.Epics)
        {
            Row(csv, ["", "Epic", epic.Title, "", "", Html([epic.Description]), "", epic.Id]);
            foreach (var story in epic.Stories)
            {
                Row(csv, ["", "User Story", "", story.Title, "", Html([story.Narrative, "", Requirements(story)]),
                    Html(story.Criteria.SelectMany(Criterion).ToList()), string.Join("; ", StoryLabels(story))]);
                foreach (var task in story.Tasks)
                    Row(csv, ["", "Task", "", "", task.Title, "", "", task.Id]);
            }
        }
        return Utf8(csv);
    }

    // The description of a story in Jira: the narrative, the acceptance
    // criteria in Gherkin and the requirements it covers, as plain lines.
    private static string JiraStory(ExportStory story) => string.Join("\n",
    [
        story.Narrative,
        "",
        "Criterios de aceptación:",
        .. story.Criteria.SelectMany(c => Criterion(c).Append("")),
        Requirements(story),
    ]);

    private static IEnumerable<string> Criterion(ExportCriterion c) =>
        [$"{c.Id} {c.Scenario}", $"Given {c.Given}", $"When {c.When}", $"Then {c.Then}"];

    private static string Requirements(ExportStory story) => $"Requisitos: {string.Join(", ", story.Requirements)}";

    private static List<string> StoryLabels(ExportStory story) =>
        [story.Id, .. story.Requirements, .. story.PendingDiscrepancy ? new[] { PendingDiscrepancyLabel } : []];

    // Lines as HTML, one per line; an empty line separates paragraphs. Only
    // &, < and > are escaped: WebUtility.HtmlEncode would also turn every
    // accent into an entity ("ó" → "&#243;"), valid but unreadable as text.
    private static string Html(IReadOnlyList<string> lines) =>
        string.Join("<br>", lines.SelectMany(l => l.ReplaceLineEndings("\n").Split('\n'))
            .Select(l => l.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")));

    // A CSV row: every field in quotes, the quotes inside doubled, CRLF at the end.
    private static void Row(StringBuilder csv, IEnumerable<string> fields) =>
        csv.Append(string.Join(',', fields.Select(f => $"\"{f.Replace("\"", "\"\"")}\""))).Append("\r\n");

    private static byte[] Utf8(StringBuilder csv) => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(csv.ToString());
}
