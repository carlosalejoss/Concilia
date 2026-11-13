using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using Gherkin;
using Concilia.Core.Catalog;
using Concilia.Core.Deliverables.Export;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;
using UglyToad.PdfPig;

namespace Concilia.Tests.Deliverables;

// Step 140: in a project in English, what the client receives has the texts
// of the platform in English too (decisions of the author of 13-11-2026).
// Those in Spanish stay as they were: the tests of each export check them.
public class ProjectLanguageTests
{
    private static readonly ExportLanguage English = new("en");
    private static readonly DateTimeOffset Date = new(2026, 11, 13, 10, 30, 0, TimeSpan.FromHours(1));

    [Fact]
    public void The_narrative_follows_the_template_of_each_language()
    {
        Assert.Equal("Como empleado, quiero reservar una sala, para tenerla asegurada.",
            ExportLanguage.Spanish.Narrative("empleado", "reservar una sala", "tenerla asegurada"));
        // As the model writes the fields in English (PURE, step 105): the role
        // with its article, the other two starting with a verb.
        Assert.Equal("As an authorized user, I want to create vouchers, so that I can supply the pilot.",
            English.Narrative("an authorized user", "create vouchers", "supply the pilot."));
        // A "to" the model already wrote is not doubled.
        Assert.Equal("As a clerk, I want to print a voucher, so that I can hand it out.",
            English.Narrative("a clerk", "to print a voucher", "hand it out"));
    }

    private const string Catalog = """
        {"requisitos": [
          {"id": "RF-001", "tipo": "RF", "notacion": "EARS", "obligatoriedad": "shall", "texto": "When a clerk prints a voucher, the system shall record it.",
           "fuentes": ["DOC-01#F001", "DOC-02#F009"], "preguntas": ["P-001"], "origen": "ambas", "discrepancia": true, "explicacion": "internal", "revisada": false},
          {"id": "RNF-001", "tipo": "RNF", "notacion": "MASTeR", "obligatoriedad": "should", "texto": "The system should answer in under 2 s.",
           "fuentes": ["DOC-01#F001"], "preguntas": [], "origen": "A", "discrepancia": false, "explicacion": "internal", "revisada": false}
        ]}
        """;

    private static CatalogExport CatalogExport() => Core.Deliverables.Export.CatalogExport.Build(
        "Microcare", false, Date, null, Catalog,
        [new Fragment("DOC-01#F001", "DOC-01", Fragmenter.DocumentKind, "§1", "Vouchers are printed by clerks.", null)],
        "=== DOC-01 · microcare.pdf ===\n",
        [new ExportQuestion("P-001", "Who prints the vouchers?", "The clerks.", AiGuess: true)],
        language: "en");

    [Fact]
    public void The_catalog_in_Word_and_PDF_has_the_texts_of_the_platform_in_English()
    {
        var c = CatalogExport();
        Assert.Equal(("Requirements catalog", "Functional requirements", "Draft: not validated yet"), (c.Title, c.Sections[0].Title, c.Status));
        Assert.Equal("EARS · shall (mandatory)", c.Details(c.Sections[0].Requirements[0]));
        Assert.Equal("Sources: DOC-01#F001, DOC-02#F009 · Questions: P-001", c.References(c.Sections[0].Requirements[0]));
        Assert.Equal("Answer (assumption of the AI): The clerks.", c.Answer(c.Questions[0]));
        Assert.Equal("Exported from Concilia on 2026-11-13 at 10:30.", c.Generated);
        Assert.Equal("catalog-microcare", c.FileName("microcare"));

        using var word = WordprocessingDocument.Open(new MemoryStream(CatalogDocx.Write(c)), false);
        Assert.Empty(new OpenXmlValidator().Validate(word).Select(e => $"{e.Path?.XPath}: {e.Description}"));
        var text = word.MainDocumentPart!.Document!.Body!.InnerText;
        foreach (var expected in new[] { "2 requirements:", "Appendix A. Cited sources", "Appendix B. Questions and answers", c.AiGuessMark, c.DiscrepancyMark, c.NotInDocumentation })
            Assert.Contains(expected, text);
        Assert.DoesNotContain("Anexo", text);
        Assert.Contains("DRAFT · page", word.MainDocumentPart.FooterParts.Single().Footer!.InnerText);
        Assert.Equal("en-US", word.MainDocumentPart.StyleDefinitionsPart!.Styles!.Descendants<Languages>().Single().Val!.Value);

        using var pdf = PdfDocument.Open(CatalogPdf.Write(c));
        var pdfText = string.Join("\n", pdf.GetPages().Select(p => string.Join(" ", p.GetWords().Select(w => w.Text))));
        Assert.Contains("Appendix A. Cited sources", pdfText);
        Assert.Contains("page 1 of", pdfText);
        Assert.DoesNotContain("página", pdfText);
    }

    private const string Backlog = """
        {"epicas": [
          {"id": "EP-01", "titulo": "Vouchers", "descripcion": "Print and track vouchers.",
           "historias": [
             {"id": "HU-001", "titulo": "Print vouchers", "como": "a clerk", "quiero": "print a batch of vouchers", "para": "hand them out",
              "requisitos": ["RF-001"],
              "criterios": [{"id": "CA-001", "escenario": "A batch", "dado": "a clerk signed in", "cuando": "she prints a batch", "entonces": ""}],
              "tareas": [{"id": "T-001", "titulo": "Print screen"}],
              "origen": "ambas", "discrepancia": true, "explicacion": "internal", "revisada": false}
           ]}
        ]}
        """;

    private static BacklogExport BacklogExport() => Core.Deliverables.Export.BacklogExport.Build(
        "Microcare", true, Date, Date, Backlog, new Dictionary<string, IReadOnlyList<int>> { ["RF-001"] = [2] }, language: "en");

    [Fact]
    public void Gherkin_has_the_narrative_the_tasks_and_the_tag_in_English()
    {
        var feature = BacklogGherkin.Features(BacklogExport()).Single().Content;

        Assert.StartsWith("# Backlog of Microcare · validated on 2026-11-13 · exported from Concilia on 2026-11-13 at 10:30\n", feature);
        Assert.Contains("As a clerk, I want to print a batch of vouchers, so that I can hand them out.", feature);
        Assert.Contains("Task T-001: Print screen", feature);
        Assert.Contains("@unreviewed-discrepancy", feature);
        Assert.Contains("Then (not stated)", feature);
        new Parser().Parse(new StringReader(feature)); // still valid Gherkin
    }

    [Fact]
    public void The_CSV_has_its_texts_and_labels_in_English()
    {
        var jira = System.Text.Encoding.UTF8.GetString(BacklogCsv.Jira(BacklogExport()));
        Assert.Contains("Acceptance criteria:", jira);
        Assert.Contains("Requirements: RF-001", jira);
        Assert.Contains("\"unreviewed-discrepancy\"", jira);
        Assert.Contains("\"iteration-2\"", jira);
        Assert.DoesNotContain("iteracion", jira);

        var azure = System.Text.Encoding.UTF8.GetString(BacklogCsv.AzureDevOps(BacklogExport()));
        Assert.Contains("As a clerk, I want to print", azure);
        Assert.Contains("HU-001; RF-001; unreviewed-discrepancy; iteration-2", azure);
    }

    [Fact]
    public void GitHub_gets_the_issues_in_English_and_the_analyst_the_README_in_Spanish()
    {
        var issues = BacklogGitHub.Issues(BacklogExport(), "ab12cd34").ToDictionary(i => i.Id);
        Assert.Equal(["epic", "iteration-2"], issues["EP-01"].Labels);
        Assert.Equal(["story", "unreviewed-discrepancy", "iteration-2"], issues["HU-001"].Labels);
        Assert.Equal(["task", "iteration-2"], issues["T-001"].Labels);
        Assert.Contains("## Acceptance criteria", issues["HU-001"].Body);
        Assert.Contains("## Requirements", issues["HU-001"].Body);
        Assert.StartsWith("Task of the story HU-001.", issues["T-001"].Body);

        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(BacklogGitHub.Write(BacklogExport(), Guid.Empty)));
        using var readme = new StreamReader(zip.GetEntry("README.md")!.Open());
        Assert.StartsWith("# Backlog de Microcare para GitHub\n\nvalidado el 13-11-2026", readme.ReadToEnd());
    }

    [Fact]
    public void The_prefix_of_a_decision_of_the_client_goes_in_the_language_of_the_suggestions()
    {
        var phase = PhaseDefinition.For(PhaseKind.Suggestions, PhaseRole.Proposal);
        string Prompt(string language) => PhasePrompt.Build(phase, new PhaseInput { SystemName = "el sistema", Language = language });

        // In Spanish, the sentence of the prompt is the same as before, word for word.
        Assert.Contains("La sugerencia empieza por \"A decidir por el cliente:\" y da las opciones razonables", Prompt("es"));
        Assert.Contains("La sugerencia empieza por \"To be decided by the client:\" y da las opciones razonables", Prompt("en"));
        Assert.DoesNotContain("{{", Prompt("en"));
    }

    [Fact]
    public void The_comparison_shows_the_stories_with_the_narrative_of_the_project()
    {
        const string proposal = """{"epicas": [{"id": "EP-01", "titulo": "V", "descripcion": "", "historias": [{"id": "HU-001", "titulo": "Print vouchers", "como": "a clerk", "quiero": "print vouchers", "para": "hand them out", "requisitos": [], "criterios": [], "tareas": []}]}]}""";
        var arbiter = """{"epicas": [{"id": "EP-01", "titulo": "V", "descripcion": "", "historias": [{"id": "HU-001", "titulo": "Print vouchers", "como": "a clerk", "quiero": "print vouchers", "para": "hand them out", "requisitos": [], "criterios": [], "tareas": [], "de": {"propuesta_1": ["HU-001"], "propuesta_2": ["HU-001"]}, "discrepancia": false, "explicacion": "x"}]}], "descartadas": []}""";
        var matching = new ArbiterMatching(new FakeTextEmbedder(), new MatchThresholds(Same: 0.9, Similar: 0.7)).Compute(PhaseKind.Backlog, proposal, proposal);

        var comparison = JobComparison.Build(PhaseKind.Backlog, arbiter, ["A", "B"], matching, [], "en", proposal, proposal);

        const string shown = "Print vouchers. As a clerk, I want to print vouchers, so that I can hand them out.";
        var row = comparison.Rows.Single();
        Assert.Equal(shown, row.ArbiterText);
        Assert.Equal(shown, row.A.Single().Text);
        // The embeddings compared the same text as always.
        Assert.StartsWith("Print vouchers. Como a clerk", matching.Elements.First().Text);
    }
}
