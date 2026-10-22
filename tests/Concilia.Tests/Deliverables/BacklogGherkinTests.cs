using System.IO.Compression;
using Gherkin;
using Gherkin.Ast;
using Concilia.Core.Deliverables.Export;

namespace Concilia.Tests.Deliverables;

// Step 97: the backlog as Gherkin, a .feature file per epic in a .zip, read
// back with the official parser: epic → Feature, story → Rule, acceptance
// criterion → Scenario, with the ids as tags.
public class BacklogGherkinTests
{
    private const string Backlog = """
        {"epicas": [
          {"id": "EP-01", "titulo": "Reserva de salas", "descripcion": "Reservar salas.\nScenario: esta línea crearía un escenario\n# esta sería un comentario\n@y esta una etiqueta\nGiven un paso se queda como texto",
           "historias": [
             {"id": "HU-001", "titulo": "Reservar una sala", "como": "empleado", "quiero": "reservar una sala", "para": "tenerla asegurada",
              "requisitos": ["RF-001", "RNF-001"],
              "criterios": [
                {"id": "CA-001", "escenario": "Sala libre", "dado": "una sala libre\nun empleado con sesión iniciada", "cuando": "confirma la reserva", "entonces": "queda registrada"},
                {"id": "CA-002", "escenario": "Sala ocupada", "dado": "una sala ocupada", "cuando": "intenta reservarla", "entonces": ""}
              ],
              "tareas": [{"id": "T-001", "titulo": "Pantalla de nueva reserva"}],
              "origen": "ambas", "discrepancia": true, "explicacion": "interna", "revisada": false}
           ]},
          {"id": "EP-02", "titulo": "Administración: aprobación", "descripcion": "Aprobar reservas largas.",
           "historias": [
             {"id": "HU-002", "titulo": "Aprobar una reserva", "como": "administrador", "quiero": "aprobar reservas", "para": "controlar las salas grandes",
              "requisitos": ["RF-002"],
              "criterios": [{"id": "CA-003", "escenario": "Aprobada", "dado": "una reserva pendiente", "cuando": "la aprueba", "entonces": "queda confirmada"}],
              "tareas": [], "origen": "A", "discrepancia": true, "explicacion": "interna", "revisada": true}
           ]}
        ]}
        """;

    private static BacklogExport Build() =>
        BacklogExport.Build("ReservaSalas", false, new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.FromHours(2)), null, Backlog);

    private static GherkinDocument Parse(string feature) => new Parser().Parse(new StringReader(feature));

    [Fact]
    public void There_is_a_feature_file_per_epic_in_the_zip()
    {
        using var zip = new ZipArchive(new MemoryStream(BacklogGherkin.Write(Build())));

        Assert.Equal(["EP-01-reserva-de-salas.feature", "EP-02-administracion-aprobacion.feature"], zip.Entries.Select(e => e.FullName));
        using var reader = new StreamReader(zip.Entries[0].Open());
        var text = reader.ReadToEnd();
        Assert.Equal("Reserva de salas", Parse(text).Feature.Name);
    }

    [Fact]
    public void Epic_story_and_criterion_are_feature_rule_and_scenario_with_their_ids_as_tags()
    {
        var feature = Parse(BacklogGherkin.Features(Build())[0].Content).Feature;

        Assert.Equal(["@EP-01"], feature.Tags.Select(t => t.Name));
        var rule = Assert.IsType<Rule>(Assert.Single(feature.Children));
        Assert.Equal("Reservar una sala", rule.Name);
        Assert.Equal(["@HU-001", "@RF-001", "@RNF-001", "@discrepancia-sin-revisar"], rule.Tags.Select(t => t.Name));
        Assert.Contains("Como empleado, quiero reservar una sala, para tenerla asegurada.", rule.Description);
        Assert.Contains("Tarea T-001: Pantalla de nueva reserva", rule.Description);

        var scenarios = rule.Children.OfType<Scenario>().ToList();
        Assert.Equal(["Sala libre", "Sala ocupada"], scenarios.Select(s => s.Name));
        Assert.Equal(["@CA-001"], scenarios[0].Tags.Select(t => t.Name));
        // A line per step: the second line of "dado" goes on with And.
        Assert.Equal(["Given ", "And ", "When ", "Then "], scenarios[0].Steps.Select(s => s.Keyword));
        Assert.Equal("un empleado con sesión iniciada", scenarios[0].Steps.ElementAt(1).Text);
        // An empty part still gives a step.
        Assert.Equal("(sin indicar)", scenarios[1].Steps.Last().Text);
    }

    [Fact]
    public void Text_that_would_change_the_meaning_stays_text()
    {
        var feature = Parse(BacklogGherkin.Features(Build())[0].Content).Feature;

        Assert.Contains("- Scenario: esta línea crearía un escenario", feature.Description);
        Assert.Contains("- # esta sería un comentario", feature.Description);
        Assert.Contains("- @y esta una etiqueta", feature.Description);
        Assert.Contains("  Given un paso se queda como texto", feature.Description);
        Assert.Single(feature.Children); // only the Rule of HU-001
    }

    [Fact]
    public void A_reviewed_discrepancy_has_no_tag_and_the_header_says_the_status()
    {
        var (_, content) = BacklogGherkin.Features(Build())[1];

        Assert.StartsWith("# Backlog de ReservaSalas · BORRADOR, todavía no está validado · exportado desde Concilia el 22-10-2026", content);
        var rule = (Rule)Parse(content).Feature.Children.Single();
        Assert.Equal(["@HU-002", "@RF-002"], rule.Tags.Select(t => t.Name));
        Assert.DoesNotContain("interna", content); // the arbiter's explanation is left out
    }

    [Fact]
    public void A_file_the_parser_rejects_is_an_error()
    {
        // A tag at the end, with nothing to tag.
        Assert.Throws<InvalidOperationException>(() => BacklogGherkin.Check("Feature: x\n  Scenario: s\n    Given a\n  @tag\n"));
    }
}
