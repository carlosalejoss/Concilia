using Concilia.Core.Catalog;
using Concilia.Core.Deliverables;
using Concilia.Core.Deliverables.Export;

namespace Concilia.Tests.Deliverables;

// Step 99: the traceability of a project, from a requirement to its fragments,
// questions and stories, and from a story to its requirements. Built from the
// same content as the exports.
public class TraceabilityTests
{
    private const string Catalog = """
        {"requisitos": [
          {"id": "RF-001", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema deberá permitir reservar una sala.",
           "fuentes": ["DOC-01#F001"], "preguntas": ["P-001"], "discrepancia": false, "revisada": false},
          {"id": "RF-002", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema deberá permitir anular una reserva.",
           "fuentes": ["DOC-01#F001", "DOC-01#F009"], "preguntas": ["P-001", "P-007"], "discrepancia": true, "revisada": false},
          {"id": "RNF-001", "tipo": "RNF", "notacion": "MASTeR", "obligatoriedad": "should", "texto": "El sistema debería responder en menos de 2 s.",
           "fuentes": [], "preguntas": [], "discrepancia": false, "revisada": false}
        ]}
        """;

    private const string Backlog = """
        {"epicas": [
          {"id": "EP-01", "titulo": "Reservas", "descripcion": "", "historias": [
            {"id": "HU-001", "titulo": "Reservar", "como": "empleado", "quiero": "reservar una sala", "para": "tenerla asegurada",
             "requisitos": ["RF-001", "RF-002"], "criterios": [], "tareas": [], "discrepancia": false, "revisada": false},
            {"id": "HU-002", "titulo": "Anular", "como": "empleado", "quiero": "anular mi reserva", "para": "liberar la sala",
             "requisitos": ["RF-002", "RF-099"], "criterios": [], "tareas": [], "discrepancia": true, "revisada": false}
          ]}
        ]}
        """;

    private static readonly Fragment[] Fragments =
    [
        new("DOC-01#F001", "DOC-01", Fragmenter.DocumentKind, "§2.1", "El empleado puede reservar y anular una sala.", null),
        new("DOC-01#F002", "DOC-01", Fragmenter.DocumentKind, "§2.2", "Fragmento que nadie cita.", null),
    ];

    private const string Documentation = "=== DOC-01 · requisitos.docx ===\n[DOC-01#F001 §2.1] El empleado puede reservar y anular una sala.\n";

    private static readonly ExportQuestion[] Questions =
    [
        new("P-001", "¿Quién puede anular una reserva?", "Solo quien la hizo.", AiGuess: true),
        new("P-002", "¿Pregunta que nadie cita?", null, AiGuess: false),
    ];

    private static readonly DateTimeOffset Date = new(2026, 10, 22, 12, 0, 0, TimeSpan.FromHours(2));

    private static Traceability Build(string? backlog = Backlog) => Traceability.Build(
        CatalogExport.Build("ReservaSalas", false, Date, null, Catalog, Fragments, Documentation, Questions),
        backlog is null ? null : BacklogExport.Build("ReservaSalas", false, Date, null, backlog));

    [Fact]
    public void A_requirement_knows_its_fragments_questions_and_stories()
    {
        var trace = Build();
        var rf2 = trace.Requirements.Single(r => r.Id == "RF-002");

        Assert.Equal("RF", rf2.Type);
        Assert.Equal(["DOC-01#F001", "DOC-01#F009"], rf2.Sources);
        Assert.Equal(["P-001", "P-007"], rf2.Questions);
        Assert.Equal(["HU-001", "HU-002"], rf2.Stories);
        Assert.True(rf2.AiGuess);              // cites P-001, a guess of the AI
        Assert.True(rf2.PendingDiscrepancy);
        Assert.Empty(trace.Requirements.Single(r => r.Id == "RNF-001").Stories);
    }

    [Fact]
    public void A_story_knows_its_epic_and_requirements()
    {
        var hu2 = Build().Stories.Single(h => h.Id == "HU-002");

        Assert.Equal("EP-01", hu2.EpicId);
        Assert.Equal("Reservas", hu2.EpicTitle);
        Assert.Equal("Como empleado, quiero anular mi reserva, para liberar la sala.", hu2.Narrative);
        Assert.Equal(["RF-002", "RF-099"], hu2.Requirements);
        Assert.True(hu2.PendingDiscrepancy);
    }

    [Fact]
    public void Each_cited_fragment_and_question_is_there_once_with_its_text()
    {
        var trace = Build();

        // F001 is cited twice and F002 not at all; F009 is not in the documentation.
        Assert.Equal(["DOC-01#F001", "DOC-01#F009"], trace.Fragments.Select(f => f.Id));
        Assert.Equal("El empleado puede reservar y anular una sala.", trace.Fragments[0].Text);
        Assert.Equal("DOC-01 · requisitos.docx", trace.Fragments[0].Document);
        Assert.Equal("§2.1", trace.Fragments[0].Section);
        Assert.Null(trace.Fragments[1].Text);
        Assert.Equal(["P-001"], trace.Questions.Select(q => q.Id));
        Assert.Equal("Solo quien la hizo.", trace.Questions[0].Answer);
    }

    [Fact]
    public void What_is_cited_and_does_not_exist_is_reported()
    {
        var trace = Build();

        Assert.Equal(["P-007"], trace.MissingQuestions);
        Assert.Equal(["RF-099"], trace.MissingRequirements);
    }

    [Fact]
    public void Without_a_backlog_there_are_no_stories()
    {
        var trace = Build(backlog: null);

        Assert.Empty(trace.Stories);
        Assert.All(trace.Requirements, r => Assert.Empty(r.Stories));
        Assert.Empty(trace.MissingRequirements);
    }
}
