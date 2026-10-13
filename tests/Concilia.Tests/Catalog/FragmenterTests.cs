using System.Text.Json;
using Concilia.Core.Catalog;

namespace Concilia.Tests.Catalog;

// The C# fragmenter must give exactly the same output as the reference script
// prompts/generacion_requisitos/fragmentar.py (see test_fragmentar.py).
public class FragmenterTests
{
    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static string ExamplePath(string example, params string[] parts) =>
        Path.Combine([AppContext.BaseDirectory, "prompts", example, .. parts]);

    private record FragmentsFile(List<Fragment> Fragments);

    [Theory]
    [InlineData("ejemplo")]
    [InlineData("ejemplo_en")]
    public void Examples_reproduce_their_fragments_json_exactly(string example)
    {
        var expected = JsonSerializer.Deserialize<FragmentsFile>(
            File.ReadAllText(ExamplePath(example, "fragmentos.json")), SnakeCase)!.Fragments;

        var actual = Fragmenter.FragmentDocument("DOC-01", File.ReadAllText(ExamplePath(example, "fuentes", "DOC-01.txt")));

        Assert.Equal(expected.Where(f => f.Kind == Fragmenter.DocumentKind), actual);
    }

    [Theory]
    [InlineData("ejemplo")]
    [InlineData("ejemplo_en")]
    public void Document_block_matches_the_reference_script(string example)
    {
        // Written by: python fragmentar.py --docs <example>/fuentes --bloque-doc ...
        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "fragmenter", $"bloque-doc-{example}.txt"));
        var fragments = Fragmenter.FragmentDocument("DOC-01", File.ReadAllText(ExamplePath(example, "fuentes", "DOC-01.txt")));

        var block = Fragmenter.DocumentBlock(fragments, new Dictionary<string, string> { ["DOC-01"] = "DOC-01.txt" });

        Assert.Equal(expected.TrimEnd('\n', '\r'), block);
    }

    // The edge cases of test_fragmentar.py: (text, section, source_ref) of each fragment.
    public static TheoryData<string, string, (string, string, string?)[]> EdgeCases => new()
    {
        {
            "título numerado como fragmento propio",
            "3.2 Gestión de reservas\nEl sistema deberá permitir reservar.\n",
            [("Gestión de reservas", "3.2", null), ("El sistema deberá permitir reservar.", "3.2", null)]
        },
        {
            "título numerado de un nivel ('2.')",
            "2. Functional requirements\n2.1 A member can reserve a book.\n",
            [("Functional requirements", "2.", null), ("A member can reserve a book.", "2.1", null)]
        },
        {
            "línea partida de PDF: no es título aunque sea corta y sin punto",
            "3.3 El sistema mostrará la lista de\nsalas disponibles.\n",
            [("El sistema mostrará la lista de salas disponibles.", "3.3", null)]
        },
        {
            "frase introductoria y viñetas sin su marcador",
            "El sistema deberá permitir al administrador:\n- crear salas,\n- eliminar salas.\n",
            [("El sistema deberá permitir al administrador:", "", null), ("crear salas,", "", null), ("eliminar salas.", "", null)]
        },
        {
            "título markdown",
            "## Seguridad\nEl sistema cifrará las contraseñas.\n",
            [("Seguridad", "Seguridad", null), ("El sistema cifrará las contraseñas.", "Seguridad", null)]
        },
        {
            "id de requisito de la fuente en source_ref (el texto no cambia)",
            "FR-12 The system shall log every failed login. R7: El usuario puede salir. ISO 9001 aplica.\n",
            [("FR-12 The system shall log every failed login.", "", "FR-12"), ("R7: El usuario puede salir.", "", "R7"), ("ISO 9001 aplica.", "", null)]
        },
        {
            "abreviaturas e iniciales no cortan la frase",
            "Se admiten tarjetas, p. ej. de EE. UU. y de la UE. Lo aprueba J. Pérez.\n",
            [("Se admiten tarjetas, p. ej. de EE. UU. y de la UE.", "", null), ("Lo aprueba J. Pérez.", "", null)]
        },
        {
            "'etc.' seguido de mayúscula cierra la frase",
            "Se exportan PDF, CSV, etc. El envío es diario.\n",
            [("Se exportan PDF, CSV, etc.", "", null), ("El envío es diario.", "", null)]
        },
        {
            "etiquetas neutralizadas",
            "El campo <nombre> es obligatorio.\n",
            [("El campo ＜nombre＞ es obligatorio.", "", null)]
        },
    };

    [Theory]
    [MemberData(nameof(EdgeCases))]
    public void Edge_cases_match_the_reference_script(string name, string text, (string, string, string?)[] expected)
    {
        var actual = Fragmenter.FragmentDocument("DOC-01", text).Select(f => (f.Text, f.Section, f.SourceRef));

        Assert.True(expected.SequenceEqual(actual), $"{name}: {string.Join(" | ", actual)}");
    }

    [Fact]
    public void Ids_are_numbered_per_document_with_three_digits()
    {
        var fragments = Fragmenter.FragmentDocument("DOC-02", "Primera frase. Segunda frase.");

        Assert.Equal(["DOC-02#F001", "DOC-02#F002"], fragments.Select(f => f.Id));
        Assert.All(fragments, f => Assert.Equal("DOC-02", f.Source));
    }

    [Fact]
    public void Page_breaks_and_crlf_split_lines_like_python()
    {
        // \f (page break in PDF text) ends a line, as in Python's str.splitlines().
        var fragments = Fragmenter.FragmentDocument("DOC-01", "1. Alcance\fEl sistema reserva salas.\r\n");

        Assert.Equal(["Alcance", "El sistema reserva salas."], fragments.Select(f => f.Text));
    }

    [Fact]
    public void Doubts_become_one_fragment_each()
    {
        var fragments = Fragmenter.FragmentDoubts([("D-001", "¿Quién aprueba?", "El  administrador <jefe>.")]);

        var doubt = Assert.Single(fragments);
        Assert.Equal("DUDAS#D-001", doubt.Id);
        Assert.Equal(Fragmenter.ClarificationKind, doubt.Kind);
        Assert.Equal("El administrador ＜jefe＞.", doubt.Text);
        Assert.Equal("<f id=\"DUDAS#D-001\" pregunta=\"¿Quién aprueba?\">El administrador ＜jefe＞.</f>", Fragmenter.DoubtsBlock(fragments));
    }

    // The index ({{INDICE}}, prompt v1.3): the same lines as document_index in
    // fragmentar.py (see INDEX_CASES in test_fragmentar.py).
    public static TheoryData<string, string[]> IndexCases => new()
    {
        { Example("ejemplo"), ["§1", "§2.1", "§2.2", "§2.3", "§2.4", "§2.5", "§2.6", "§2.7", "§3.1", "§3.2", "§3.3", "§3.4", "§3.5"] },
        { Example("ejemplo_en"), ["1. Introduction", "2. Functional requirements", "2.1", "2.2", "2.3", "2.4", "2.5", "2.6",
                                  "3. Non-functional requirements", "3.1", "3.2"] },
        { "# Calendario\n\nIntro.\n\n## 3. Roles y acceso\n\n### 3.1 Roles\nTexto.\n", ["Calendario", "3. Roles y acceso", "3.1 Roles"] },
        { "§2.1 Primera frase.\n§2.1 Segunda frase.\n§2.2 Otra.\n", ["§2.1", "§2.2"] },
    };

    private static string Example(string example) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", example, "fuentes", "DOC-01.txt"));

    [Theory]
    [MemberData(nameof(IndexCases))]
    public void Index_matches_the_reference_script(string text, string[] expected)
    {
        Assert.Equal(expected, Fragmenter.DocumentIndex(text));
    }

    [Fact]
    public void Index_of_several_documents_starts_each_one_with_its_id_and_name()
    {
        var block = Fragmenter.IndexBlock([("DOC-01", "requisitos.md", "## Uno\nTexto.\n"), ("DOC-02", "anexo.txt", "§1 Texto.\n")]);

        Assert.Equal("DOC-01 requisitos.md\nUno\nDOC-02 anexo.txt\n§1", block);
    }
}
