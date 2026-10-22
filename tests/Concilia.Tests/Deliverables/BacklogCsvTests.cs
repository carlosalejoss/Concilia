using System.Text;
using Microsoft.VisualBasic.FileIO;
using Concilia.Core.Deliverables.Export;

namespace Concilia.Tests.Deliverables;

// Step 98: the backlog as CSV for Jira and Azure DevOps, read back with a
// standard CSV reader: a row per epic, story and task after its parent, the
// hierarchy as each tool expects it and the ids of Concilia as labels or tags.
public class BacklogCsvTests
{
    private const string Backlog = """
        {"epicas": [
          {"id": "EP-01", "titulo": "Reserva de salas", "descripcion": "Reservar salas.\nCon <reglas> & avisos de ocupación.",
           "historias": [
             {"id": "HU-001", "titulo": "Reservar una sala \"grande\", con catering", "como": "empleado", "quiero": "reservar una sala", "para": "tenerla asegurada",
              "requisitos": ["RF-001", "RNF-001"],
              "criterios": [{"id": "CA-001", "escenario": "Sala libre", "dado": "una sala libre", "cuando": "confirma la reserva", "entonces": "queda registrada"}],
              "tareas": [{"id": "T-001", "titulo": "Pantalla de nueva reserva"}, {"id": "T-002", "titulo": "Correo de confirmación"}],
              "origen": "ambas", "discrepancia": true, "explicacion": "interna", "revisada": false}
           ]},
          {"id": "EP-02", "titulo": "Aprobación", "descripcion": "Aprobar reservas largas.",
           "historias": [
             {"id": "HU-002", "titulo": "Aprobar una reserva", "como": "administrador", "quiero": "aprobar reservas", "para": "controlar las salas",
              "requisitos": ["RF-002"],
              "criterios": [{"id": "CA-002", "escenario": "Aprobada", "dado": "una reserva pendiente", "cuando": "la aprueba", "entonces": "queda confirmada"}],
              "tareas": [], "origen": "A", "discrepancia": false, "explicacion": "interna", "revisada": false}
           ]}
        ]}
        """;

    private static BacklogExport Build() => BacklogExport.Build("ReservaSalas", true, DateTimeOffset.Now, DateTimeOffset.Now, Backlog);

    // The rows of a CSV file, each one a list of fields.
    private static List<string[]> Read(byte[] csv)
    {
        Assert.NotEqual(Encoding.UTF8.GetPreamble(), csv[..3]); // without BOM
        using var parser = new TextFieldParser(new MemoryStream(csv), Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            Delimiters = [","],
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false,
        };
        var rows = new List<string[]>();
        while (!parser.EndOfData)
            rows.Add(parser.ReadFields()!);
        return rows;
    }

    [Fact]
    public void Jira_links_each_story_to_its_epic_and_each_task_to_its_story()
    {
        var rows = Read(BacklogCsv.Jira(Build()));

        var header = rows[0];
        Assert.Equal(["Issue ID", "Parent ID", "Issue Type", "Summary", "Epic Name", "Description", "Labels", "Labels", "Labels", "Labels"], header);
        Assert.All(rows, r => Assert.Equal(header.Length, r.Length));
        var items = rows.Skip(1).ToList();
        Assert.Equal(["Epic", "Story", "Sub-task", "Sub-task", "Epic", "Story"], items.Select(r => r[2]));
        Assert.Equal(["1", "2", "3", "4", "5", "6"], items.Select(r => r[0]));
        Assert.Equal(["", "1", "2", "2", "", "5"], items.Select(r => r[1])); // each one after its parent

        var epic = items[0];
        Assert.Equal("Reserva de salas", epic[4]); // Epic Name
        var story = items[1];
        Assert.Equal("Reservar una sala \"grande\", con catering", story[3]); // quotes and commas survive
        Assert.Equal(["HU-001", "RF-001", "RNF-001", BacklogCsv.PendingDiscrepancyLabel], story[6..]);
        Assert.Contains("Como empleado, quiero reservar una sala, para tenerla asegurada.", story[5]);
        Assert.Contains("CA-001 Sala libre\nGiven una sala libre\nWhen confirma la reserva\nThen queda registrada", story[5]);
        Assert.Contains("Requisitos: RF-001, RNF-001", story[5]);
        Assert.Equal(["T-001", "", "", ""], items[2][6..]);
        Assert.DoesNotContain(rows.SelectMany(r => r), f => f.Contains("interna")); // the arbiter's explanation is left out
    }

    [Fact]
    public void Azure_DevOps_puts_each_title_in_the_column_of_its_level()
    {
        var rows = Read(BacklogCsv.AzureDevOps(Build()));

        Assert.Equal(["ID", "Work Item Type", "Title 1", "Title 2", "Title 3", "Description", "Acceptance Criteria", "Tags"], rows[0]);
        var items = rows.Skip(1).ToList();
        Assert.All(items, r => Assert.Equal("", r[0])); // new work items
        Assert.Equal(["Epic", "User Story", "Task", "Task", "Epic", "User Story"], items.Select(r => r[1]));
        // Exactly one title per row, in the column of its level.
        var levels = new Dictionary<string, int> { ["Epic"] = 2, ["User Story"] = 3, ["Task"] = 4 };
        Assert.All(items, r => Assert.Equal([levels[r[1]]], Enumerable.Range(2, 3).Where(i => r[i] != "")));

        Assert.Equal("Reservar salas.<br>Con &lt;reglas&gt; &amp; avisos de ocupación.", items[0][5]); // HTML, escaped; accents as they are
        var story = items[1];
        Assert.Equal("Como empleado, quiero reservar una sala, para tenerla asegurada.<br><br>Requisitos: RF-001, RNF-001", story[5]);
        Assert.Equal("CA-001 Sala libre<br>Given una sala libre<br>When confirma la reserva<br>Then queda registrada", story[6]);
        Assert.Equal("HU-001; RF-001; RNF-001; " + BacklogCsv.PendingDiscrepancyLabel, story[7]);
        Assert.Equal("HU-002; RF-002", items[5][7]);
        Assert.Equal("T-002", items[3][7]);
    }

    // What the user can type in a draft: a title in two lines, a requirement
    // with a space or left empty, and a backlog without epics.
    [Fact]
    public void Titles_go_in_one_line_and_ids_without_spaces()
    {
        const string edited = """
            {"epicas": [{"id": "EP-01", "titulo": "Reserva\nde salas", "descripcion": "",
              "historias": [{"id": "HU-001", "titulo": "Reservar", "como": "a", "quiero": "b", "para": "c", "requisitos": ["RF 001", "  "],
                             "criterios": [], "tareas": [{"id": "T-001", "titulo": "Primera línea\nsegunda línea"}]}]}]}
            """;
        var backlog = BacklogExport.Build("P", false, DateTimeOffset.Now, null, edited);

        var jira = Read(BacklogCsv.Jira(backlog));
        Assert.Equal("Reserva de salas", jira[1][3]);
        Assert.Equal("Primera línea segunda línea", jira[3][3]);
        Assert.Equal(["HU-001", "RF001"], jira[2][6..]);
        var azure = Read(BacklogCsv.AzureDevOps(backlog));
        Assert.Equal("Reserva de salas", azure[1][2]);
        Assert.Equal("HU-001; RF001", azure[2][7]);
        Assert.Equal(["@HU-001", "@RF001"], new Gherkin.Parser().Parse(new StringReader(BacklogGherkin.Features(backlog)[0].Content))
            .Feature.Children.OfType<Gherkin.Ast.Rule>().Single().Tags.Select(t => t.Name));
    }

    [Fact]
    public void An_empty_backlog_gives_only_the_header()
    {
        var empty = BacklogExport.Build("P", false, DateTimeOffset.Now, null, """{"epicas": []}""");

        Assert.Equal(["Issue ID", "Parent ID", "Issue Type", "Summary", "Epic Name", "Description"], Assert.Single(Read(BacklogCsv.Jira(empty))));
        Assert.Single(Read(BacklogCsv.AzureDevOps(empty)));
    }
}
