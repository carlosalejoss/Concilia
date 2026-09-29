using System.Text.RegularExpressions;
using ReqArbiter.Core.Catalog;

namespace ReqArbiter.Tests.Catalog;

public class PromptBuilderTests
{
    private static CatalogPromptInput Input(string documentation = "<documento id=\"DOC-01\" nombre=\"DOC-01.txt\">\n</documento>") => new(
        SystemName: "el sistema ReservaSalas",
        Language: "es",
        LotNumber: 2,
        LotTotal: 3,
        Glossary: "sala: espacio reservable",
        ResolvedDoubts: "",
        Documentation: documentation);

    [Fact]
    public void Template_is_the_embedded_v1_2_prompt()
    {
        var file = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "prompt_requisitos_v1.2.md"));

        Assert.Equal(file, PromptBuilder.Template);
        Assert.Equal("req-1.2", PromptBuilder.Version);
    }

    [Fact]
    public void Every_placeholder_is_filled()
    {
        var prompt = PromptBuilder.Build(Input());

        Assert.DoesNotMatch(new Regex(@"\{\{[A-Z_]+\}\}"), prompt);
        Assert.Contains("<nombre_sistema>el sistema ReservaSalas</nombre_sistema>", prompt);
        Assert.Contains("<idioma>es</idioma>", prompt);
        Assert.Contains("<lote numero=\"2\" total=\"3\"/>", prompt);
        Assert.Contains("<glosario>sala: espacio reservable</glosario>", prompt);
        Assert.Contains("<dudas_resueltas></dudas_resueltas>", prompt);
        Assert.Contains("<documentacion><documento id=\"DOC-01\"", prompt);
        // {{IDIOMA}} appears twice in the template (§1 and the keyword table of §3).
        Assert.Contains("Columna según `es`.", prompt);
    }

    [Fact]
    public void Placeholders_inside_the_documents_are_not_replaced()
    {
        // A document that contains placeholder-like text must reach the model unchanged.
        const string documentation = "<f id=\"DOC-01#F001\" sec=\"\">Escribe {{NOMBRE_SISTEMA}} y {{DOCUMENTACION}} aquí.</f>";

        var prompt = PromptBuilder.Build(Input(documentation));

        Assert.Contains("Escribe {{NOMBRE_SISTEMA}} y {{DOCUMENTACION}} aquí.", prompt);
    }

    [Fact]
    public void Sha256_is_stable_and_changes_with_the_input()
    {
        var a = PromptBuilder.Sha256(PromptBuilder.Build(Input()));
        var b = PromptBuilder.Sha256(PromptBuilder.Build(Input()));
        var c = PromptBuilder.Sha256(PromptBuilder.Build(Input() with { LotNumber = 3 }));

        Assert.Matches("^[0-9a-f]{64}$", a);
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void Repair_annex_carries_the_attempt_the_catalog_and_one_error_per_line()
    {
        var annex = PromptBuilder.BuildRepairAnnex(1, "{\"schema_version\":\"1.2\"}", ["REQ-001 C5 término vago", "C10 fragmentos sin tratar"]);

        Assert.Contains("# §12 REPARACIÓN (intento 1 de 2)", annex);
        Assert.Contains("<catalogo_previo>{\"schema_version\":\"1.2\"}</catalogo_previo>", annex);
        Assert.Contains("<errores>\nREQ-001 C5 término vago\nC10 fragmentos sin tratar\n</errores>", annex.Replace("\r\n", "\n"));
    }
}
