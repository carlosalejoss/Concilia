using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using Concilia.Core.Catalog;
using Concilia.Core.Deliverables.Export;
using UglyToad.PdfPig;

namespace Concilia.Tests.Deliverables;

// Step 96: the catalog as Word and PDF. The content is built once
// (CatalogExport) and both files show it: a section per type in the order of
// the web, the marks for the reader, and the annexes with each cited fragment
// and question once.
public class CatalogExportTests
{
    private const string Catalog = """
        {"requisitos": [
          {"id": "RNF-001", "tipo": "RNF", "notacion": "MASTeR", "obligatoriedad": "should", "texto": "El sistema debería responder en menos de 2 s.",
           "fuentes": ["DOC-01#F002"], "preguntas": [], "origen": "A", "discrepancia": true, "explicacion": "interna", "revisada": false},
          {"id": "RF-001", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "Cuando el empleado reserve una sala, el sistema deberá confirmarla.",
           "fuentes": ["DOC-01#F001", "DOC-01#F002"], "preguntas": ["P-002"], "origen": "ambas", "discrepancia": true, "explicacion": "interna", "revisada": true},
          {"id": "RF-002", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "El sistema deberá permitir anular una reserva.\nSolo su autor.",
           "fuentes": ["DOC-02#F009"], "preguntas": ["P-001"], "origen": "B", "discrepancia": false, "explicacion": "interna", "revisada": false}
        ]}
        """;

    private static readonly Fragment[] Fragments =
    [
        new("DOC-01#F001", "DOC-01", Fragmenter.DocumentKind, "§2.1", "El empleado puede reservar una sala.", null),
        new("DOC-01#F002", "DOC-01", Fragmenter.DocumentKind, "§2.2", "La confirmación llega al momento.", null),
        new("DOC-01#F003", "DOC-01", Fragmenter.DocumentKind, "§2.3", "Fragmento que nadie cita.", null),
    ];

    private const string Documentation = "=== DOC-01 · requisitos.docx ===\n[DOC-01#F001 §2.1] El empleado puede reservar una sala.\n";

    private static readonly ExportQuestion[] Questions =
    [
        new("P-001", "¿Quién puede anular una reserva?", "Solo quien la hizo.", AiGuess: false),
        new("P-002", "¿Se avisa por correo?", "Sí, al momento.", AiGuess: true),
        new("P-003", "¿Pregunta que nadie cita?", null, AiGuess: false),
    ];

    private static readonly DateTimeOffset Date = new(2026, 10, 20, 10, 30, 0, TimeSpan.FromHours(2));

    private static CatalogExport Build(bool validated = false) =>
        CatalogExport.Build("ReservaSalas", validated, Date, validated ? Date : null, Catalog, Fragments, Documentation, Questions);

    [Fact]
    public void Requirements_are_grouped_by_type_in_the_order_of_the_web()
    {
        var export = Build();

        Assert.Equal(["RF", "RNF"], export.Sections.Select(s => s.Type));
        Assert.Equal(["RF-001", "RF-002"], export.Sections[0].Requirements.Select(r => r.Id));
        Assert.Equal(3, export.Total);
        var rf1 = export.Sections[0].Requirements[0];
        Assert.Equal("EARS · shall (obligatorio)", CatalogExport.Details(rf1));
        Assert.Equal("Fuentes: DOC-01#F001, DOC-01#F002 · Preguntas: P-002", CatalogExport.References(rf1));
    }

    [Fact]
    public void A_requirement_is_marked_if_it_cites_a_guess_of_the_AI_or_has_a_discrepancy_not_reviewed()
    {
        var all = Build().Sections.SelectMany(s => s.Requirements).ToDictionary(r => r.Id);

        Assert.True(all["RF-001"].AiGuess);              // cites P-002, a guess accepted as it was
        Assert.False(all["RF-001"].PendingDiscrepancy);  // reviewed
        Assert.False(all["RF-002"].AiGuess);
        Assert.True(all["RNF-001"].PendingDiscrepancy);
    }

    [Fact]
    public void The_annexes_have_each_cited_fragment_and_question_once()
    {
        var export = Build();

        // In the order of the documentation, with the name of the document;
        // a citation that is not in it, at the end and without text.
        Assert.Equal(["DOC-01#F001", "DOC-01#F002", "DOC-02#F009"], export.Sources.Select(s => s.Id));
        Assert.Equal("DOC-01 · requisitos.docx", export.Sources[0].Document);
        Assert.Equal("El empleado puede reservar una sala.", export.Sources[0].Text);
        Assert.Equal("DOC-02", export.Sources[2].Document);
        Assert.Null(export.Sources[2].Text);

        Assert.Equal(["P-001", "P-002"], export.Questions.Select(q => q.Id));
        Assert.Equal("Respuesta (suposición de la IA): Sí, al momento.", CatalogExport.Answer(export.Questions[1]));
    }

    [Fact]
    public void The_status_says_whether_it_is_validated()
    {
        Assert.Equal("Borrador: todavía no está validado", Build().Status);
        Assert.Contains("BORRADOR", Build().Footer);
        Assert.Equal("Validado el 20-10-2026", Build(validated: true).Status);
    }

    [Fact]
    public void The_Word_file_is_valid_and_has_the_catalog()
    {
        var bytes = CatalogDocx.Write(Build());

        using var word = WordprocessingDocument.Open(new MemoryStream(bytes), false);
        Assert.Empty(new OpenXmlValidator().Validate(word).Select(e => $"{e.Path?.XPath}: {e.Description}"));
        var text = word.MainDocumentPart!.Document!.Body!.InnerText;
        Assert.Contains("Requisitos funcionales (2)", text);
        Assert.Contains("RNF-001", text);
        Assert.Contains("Solo su autor.", text);
        Assert.Contains(CatalogExport.AiGuessMark, text);
        Assert.Contains(CatalogExport.SourcesTitle, text);
        Assert.Contains("La confirmación llega al momento.", text);
        Assert.Contains("¿Quién puede anular una reserva?", text);
        Assert.DoesNotContain("interna", text); // the arbiter's explanation is left out
        Assert.Contains("BORRADOR", word.MainDocumentPart.FooterParts.Single().Footer!.InnerText);
    }

    [Fact]
    public void The_PDF_opens_and_has_the_catalog()
    {
        var bytes = CatalogPdf.Write(Build());

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join("\n", pdf.GetPages().Select(p => string.Join(" ", p.GetWords().Select(w => w.Text))));
        Assert.True(pdf.NumberOfPages >= 3); // the cover and the two annexes start a page
        Assert.Contains("RF-001", text);
        Assert.Contains("RNF-001", text);
        Assert.Contains("DOC-01#F002", text);
        Assert.Contains("confirmación", text); // accents, with the embedded font
        Assert.Contains("BORRADOR", text);
        Assert.DoesNotContain("interna", text);
    }

    [Fact]
    public void The_file_name_is_plain()
    {
        Assert.Equal("clinicas-veterinarias-2", ExportFiles.Slug("Clínicas Veterinarias (2)"));
        Assert.Equal("proyecto", ExportFiles.Slug("¿?"));
    }
}
