using System.IO.Compression;
using System.Text;
using Concilia.Api.Deliverables;
using Concilia.Core.Deliverables;
using Concilia.Core.Deliverables.Export;

namespace Concilia.Tests.Deliverables;

// Step 124: the backlog for GitHub. What the script does against a repository
// is tested by hand (no test calls GitHub); here, what it is built from.
public class BacklogGitHubTests
{
    private const string Backlog = """
        {"epicas": [
          {"id": "EP-01", "titulo": "Reserva de salas", "descripcion": "Pregunta a @ana por las <reglas> y el aviso #12.",
           "historias": [
             {"id": "HU-001", "titulo": "Reservar la sala de 'dirección' $(rm -rf .)", "como": "empleado", "quiero": "reservar una sala", "para": "tenerla asegurada",
              "requisitos": ["RF-001", "RNF-001"],
              "criterios": [{"id": "CA-001", "escenario": "Sala libre", "dado": "una sala libre\nen horario", "cuando": "confirma ```la reserva```", "entonces": "@ana recibe un aviso"}],
              "tareas": [{"id": "T-001", "titulo": "Pantalla de nueva reserva"}],
              "origen": "ambas", "discrepancia": true, "explicacion": "interna", "revisada": false}
           ]}
        ]}
        """;

    private static BacklogExport Build(string json = Backlog) => BacklogExport.Build("Reserva Salas", true, DateTimeOffset.Now, DateTimeOffset.Now, json);

    private static Dictionary<string, BacklogGitHub.Issue> Issues(string json = Backlog) =>
        BacklogGitHub.Issues(Build(json), "ab12cd34").ToDictionary(i => i.Id);

    [Fact]
    public void Parents_come_before_their_sub_issues()
    {
        var issues = BacklogGitHub.Issues(Build(), "ab12cd34");

        Assert.Equal(["EP-01", "HU-001", "T-001"], issues.Select(i => i.Id));
        Assert.Equal(["", "EP-01", "HU-001"], issues.Select(i => i.Parent));
    }

    [Fact]
    public void The_type_goes_as_a_label_and_a_pending_discrepancy_too()
    {
        var issues = Issues();

        Assert.Equal([BacklogGitHub.EpicLabel], issues["EP-01"].Labels);
        Assert.Equal([BacklogGitHub.StoryLabel, BacklogCsv.PendingDiscrepancyLabel], issues["HU-001"].Labels);
        Assert.Equal([BacklogGitHub.TaskLabel], issues["T-001"].Labels);
    }

    [Fact]
    public void A_body_ends_with_the_mark_that_identifies_the_issue()
    {
        var story = Issues()["HU-001"];

        Assert.EndsWith($"\n\n<!-- concilia:HU-001 generacion=ab12cd34 hash={story.Hash} -->\n", story.Body);
    }

    [Fact]
    public void A_story_has_its_narrative_criteria_and_requirements()
    {
        var body = Issues()["HU-001"].Body;

        Assert.StartsWith("Como empleado, quiero reservar una sala, para tenerla asegurada.", body);
        Assert.Contains("Scenario: CA-001 Sala libre\n  Given una sala libre\n  And en horario\n  When confirma", body);
        Assert.Contains("## Requisitos\n\nRF-001, RNF-001", body);
    }

    [Fact]
    public void Backticks_in_a_criterion_do_not_close_its_code_block() =>
        Assert.Contains("\n````gherkin\n", Issues()["HU-001"].Body);

    [Fact]
    public void Free_text_neither_notifies_nor_links_nor_hides_anything()
    {
        Assert.Equal(@"Pregunta a `@ana` por las \<reglas> y el aviso `#12`.", BacklogGitHub.Text("Pregunta a @ana por las <reglas> y el aviso #12."));
        Assert.Equal("Escribe a ana@example.com y mira DOC-01#F001", BacklogGitHub.Text("Escribe a ana@example.com y mira DOC-01#F001"));
        Assert.Equal(@"\<!-- concilia:HU-001 generacion=x hash=y -->", BacklogGitHub.Text("<!-- concilia:HU-001 generacion=x hash=y -->"));
    }

    [Fact]
    public void A_value_for_the_shell_stays_text() =>
        Assert.Equal(@"'la '\''sala'\'' de $(x)'", BacklogGitHub.Quote("la 'sala' de $(x)"));

    [Fact]
    public void The_hash_changes_with_the_text_and_the_parent_not_with_the_labels()
    {
        var before = Issues()["HU-001"].Hash;

        Assert.Equal(before, Issues(Backlog.Replace("\"revisada\": false", "\"revisada\": true"))["HU-001"].Hash);
        Assert.NotEqual(before, Issues(Backlog.Replace("reservar una sala", "reservar una sala grande"))["HU-001"].Hash);
    }

    [Fact]
    public void The_zip_has_the_script_the_readme_and_a_body_per_issue()
    {
        var generation = Guid.Parse("ab12cd34-0000-0000-0000-000000000000");
        using var zip = new ZipArchive(new MemoryStream(BacklogGitHub.Write(Build(), generation)));

        Assert.Equal(["README.md", "cuerpos/EP-01.md", "cuerpos/HU-001.md", "cuerpos/T-001.md", "importar.sh"], zip.Entries.Select(e => e.FullName).Order(StringComparer.Ordinal));

        using var reader = new StreamReader(zip.GetEntry("importar.sh")!.Open(), Encoding.UTF8);
        var script = reader.ReadToEnd();
        Assert.DoesNotContain("\r", script); // bash does not accept CRLF
        Assert.DoesNotContain("{{", script);
        Assert.Contains("GENERACION='ab12cd34'", script);
        Assert.Contains(@"crear 'HU-001' 'EP-01' '", script);
        Assert.Contains(@"'HU-001: Reservar la sala de '\''dirección'\'' $(rm -rf .)' 'historia' 'discrepancia-sin-revisar'", script);
    }

    [Fact]
    public void An_update_keeps_the_generation_and_a_new_generation_starts_another()
    {
        var (first, update, second) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        // Newest first; a job has several versions (generated, edited, validated).
        (Guid, VersionOrigin)[] updated =
            [(update, VersionOrigin.Validated), (update, VersionOrigin.Updated), (first, VersionOrigin.Validated), (first, VersionOrigin.Generated)];

        Assert.Equal(first, DeliverableVersions.JobChain(updated, update)[^1]);
        Assert.Equal(second, DeliverableVersions.JobChain([(second, VersionOrigin.Generated), .. updated], second)[^1]);
        Assert.Equal(first, DeliverableVersions.JobChain([(update, VersionOrigin.Restored), (second, VersionOrigin.Generated), .. updated], update)[^1]);
    }
}
