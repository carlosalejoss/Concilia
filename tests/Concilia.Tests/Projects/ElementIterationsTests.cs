using System.Text;
using Concilia.Core.Catalog;
using Concilia.Core.Deliverables;
using Concilia.Core.Deliverables.Export;
using Concilia.Core.Projects;

namespace Concilia.Tests.Projects;

// Step 111: the iteration of each element, computed by the platform (fragment
// → document → iteration; question by its sources; requirement by its
// sources and questions; story by its requirements), and how the exports and
// the traceability show it.
public class ElementIterationsTests
{
    // DOC-01 and DOC-02 are from the first iteration, DOC-03 from the second.
    private static readonly ElementIterations Iterations = new(
        new Dictionary<string, int> { ["DOC-01"] = 1, ["DOC-02"] = 1, ["DOC-03"] = 2 },
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["P-001"] = ["DOC-01#F004"],
            ["P-002"] = ["DOC-03#F001", "DOC-02#F009"],
            ["P-003"] = [],
        });

    [Fact]
    public void A_fragment_is_of_the_iteration_of_its_document()
    {
        Assert.Equal(2, Iterations.OfFragment("DOC-03#F010"));
        Assert.Null(Iterations.OfFragment("DOC-09#F001")); // a document the project does not have
        Assert.Equal([1, 2], Iterations.OfFragments(["DOC-03#F001", "DOC-01#F001", "DOC-02#F002"]));
    }

    [Fact]
    public void A_question_is_of_those_of_its_sources()
    {
        Assert.Equal([1], Iterations.OfQuestion("P-001"));
        Assert.Equal([1, 2], Iterations.OfQuestion("P-002"));
        Assert.Empty(Iterations.OfQuestion("P-003"));
        Assert.Empty(Iterations.OfQuestion("P-099"));
    }

    [Fact]
    public void A_requirement_is_of_those_of_its_sources_and_of_its_questions()
    {
        Assert.Equal([1], Iterations.OfRequirement(["DOC-01#F001"], []));
        Assert.Equal([1, 2], Iterations.OfRequirement(["DOC-01#F001"], ["P-002"]));
        Assert.Equal([2], Iterations.OfRequirement(["DOC-03#F002"], ["P-003"]));
        Assert.Empty(Iterations.OfRequirement([], [])); // added by hand, without sources
    }

    [Fact]
    public void A_story_is_of_those_of_its_requirements()
    {
        var catalog = Iterations.OfCatalog("""
            {"requisitos": [
              {"id": "RF-001", "fuentes": ["DOC-01#F001"], "preguntas": []},
              {"id": "RF-004", "fuentes": ["DOC-03#F001"], "preguntas": ["P-001"]}
            ]}
            """);

        Assert.Equal([1, 2], catalog["RF-004"]);
        Assert.Equal([1, 2], ElementIterations.OfStory(["RF-001", "RF-004"], catalog));
        Assert.Equal([1], ElementIterations.OfStory(["RF-001", "RF-099"], catalog));
    }

    private const string Catalog = """
        {"requisitos": [
          {"id": "RF-001", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema deberá permitir reservar una sala.",
           "fuentes": ["DOC-01#F001"], "preguntas": [], "discrepancia": false, "revisada": false},
          {"id": "RF-002", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema deberá avisar por correo.",
           "fuentes": ["DOC-03#F001"], "preguntas": ["P-001"], "discrepancia": false, "revisada": false}
        ]}
        """;

    private const string Backlog = """
        {"epicas": [
          {"id": "EP-01", "titulo": "Reservas", "descripcion": "", "historias": [
            {"id": "HU-001", "titulo": "Reservar", "como": "empleado", "quiero": "reservar una sala", "para": "tenerla",
             "requisitos": ["RF-001"], "criterios": [], "tareas": [{"id": "T-001", "titulo": "Formulario"}], "discrepancia": false, "revisada": false},
            {"id": "HU-002", "titulo": "Avisar", "como": "empleado", "quiero": "un aviso", "para": "enterarme",
             "requisitos": ["RF-002"], "criterios": [], "tareas": [{"id": "T-002", "titulo": "Correo"}], "discrepancia": false, "revisada": false}
          ]}
        ]}
        """;

    private static CatalogExport Export(ElementIterations? iterations) =>
        CatalogExport.Build("ReservaSalas", false, DateTimeOffset.Now, null, Catalog,
            [new Fragment("DOC-01#F001", "DOC-01", Fragmenter.DocumentKind, "§1", "Reservar salas.", null),
             new Fragment("DOC-03#F001", "DOC-03", Fragmenter.DocumentKind, "§1", "Avisar por correo.", null)],
            "=== DOC-01 · a.md ===\n=== DOC-03 · acta.md ===\n",
            [new ExportQuestion("P-001", "¿Por correo?", "Sí.", AiGuess: false)], iterations);

    [Fact]
    public void Word_and_PDF_say_the_iteration_of_each_requirement()
    {
        var export = Export(Iterations);
        var requirements = export.Sections.Single().Requirements;

        Assert.Equal("Fuentes: DOC-01#F001 · Iteración: 1", CatalogExport.References(requirements[0]));
        Assert.Equal("Fuentes: DOC-03#F001 · Preguntas: P-001 · Iteraciones: 1, 2", CatalogExport.References(requirements[1]));
        Assert.Equal([1, 2], export.Sources.Select(s => s.Iteration));
        Assert.Equal([1], export.Questions.Single().Iterations!);
        // Without the iterations, the references are as before.
        Assert.Equal("Fuentes: DOC-01#F001", CatalogExport.References(Export(null).Sections.Single().Requirements[0]));
    }

    [Fact]
    public void Jira_and_Azure_DevOps_get_the_label_of_each_iteration()
    {
        var backlog = BacklogExport.Build("ReservaSalas", false, DateTimeOffset.Now, null, Backlog, Export(Iterations).IterationsById);

        var jira = Encoding.UTF8.GetString(BacklogCsv.Jira(backlog)).Split("\r\n");
        Assert.Contains("\"iteracion-1\"", jira.Single(l => l.Contains("\"Epic\"")));
        Assert.Contains("\"iteracion-2\"", jira.Single(l => l.Contains("\"Epic\"")));     // the epic: those of its stories
        Assert.DoesNotContain("iteracion-2", jira.Single(l => l.Contains("\"Reservar\"")));
        Assert.Contains("\"iteracion-2\"", jira.Single(l => l.Contains("\"Correo\"")));  // a task: those of its story
        var azure = Encoding.UTF8.GetString(BacklogCsv.AzureDevOps(backlog)).Split("\r\n");
        Assert.EndsWith("\"HU-002; RF-002; iteracion-1; iteracion-2\"", azure.Single(l => l.Contains("\"Avisar\"")));
        Assert.EndsWith("\"T-001; iteracion-1\"", azure.Single(l => l.Contains("\"Formulario\"")));
    }

    [Fact]
    public void The_traceability_says_the_iteration_of_each_element()
    {
        var catalog = Export(Iterations);
        var trace = Traceability.Build(catalog, BacklogExport.Build("ReservaSalas", false, DateTimeOffset.Now, null, Backlog, catalog.IterationsById));

        Assert.Equal([1, 2], trace.Requirements.Single(r => r.Id == "RF-002").Iterations);
        Assert.Equal([1], trace.Stories.Single(h => h.Id == "HU-001").Iterations);
        Assert.Equal(2, trace.Fragments.Single(f => f.Id == "DOC-03#F001").Iteration);
    }
}
