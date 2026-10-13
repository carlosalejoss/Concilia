using System.Text.RegularExpressions;
using Concilia.Core.Catalog;

namespace Concilia.Tests.Catalog;

public class PromptBuilderTests
{
    private static CatalogPromptInput Input(string documentation = "<documento id=\"DOC-01\" nombre=\"DOC-01.txt\">\n</documento>") => new(
        SystemName: "el sistema ReservaSalas",
        Language: "es",
        LotNumber: 2,
        LotTotal: 3,
        Glossary: "sala: espacio reservable",
        ResolvedDoubts: "",
        Index: "§1 Objetivo\n§2 Reservas",
        Documentation: documentation);

    [Fact]
    public void Template_is_the_embedded_v1_4_prompt()
    {
        var file = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "prompt_requisitos_v1.4.md"));

        Assert.Equal(file, PromptBuilder.Template);
        Assert.Equal("req-1.4", PromptBuilder.Version);
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
        Assert.Contains("<indice>§1 Objetivo\n§2 Reservas</indice>", prompt);
        Assert.Contains("<documentacion><documento id=\"DOC-01\"", prompt);
        // {{IDIOMA}} appears twice in the template (§1 and the keyword table of §6).
        Assert.Contains("Columna según `es`;", prompt);
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
        var annex = PromptBuilder.BuildRepairAnnex(1, "{\"schema_version\":\"1.3\"}", ["RF-001 C5 término vago", "LOTE C10 fragmentos sin tratar"]);

        Assert.Contains("# §12 REPARACIÓN (intento 1)", annex);
        Assert.Contains("<catalogo_previo>{\"schema_version\":\"1.3\"}</catalogo_previo>", annex);
        Assert.Contains("<errores>\nRF-001 C5 término vago\nLOTE C10 fragmentos sin tratar\n</errores>", annex.Replace("\r\n", "\n"));
    }
}
