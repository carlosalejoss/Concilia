using System.Text.Json;
using System.Text.Json.Nodes;
using ReqArbiter.Core.Catalog;

namespace ReqArbiter.Tests.Catalog;

// The C# validator must agree with the reference validar_catalogo.py:
// the two example catalogs are valid, their rendered texts match the enriched
// files, and every mutation exported by test_validador.py gives its code.
public class CatalogValidatorTests
{
    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static readonly Dictionary<string, string> SystemNames = new()
    {
        ["ejemplo"] = "el sistema ReservaSalas",
        ["ejemplo_en"] = "the LibraryLoan system",
    };

    private static string ExamplePath(string example, string file) =>
        Path.Combine(AppContext.BaseDirectory, "prompts", example, file);

    private record FragmentsFile(List<Fragment> Fragments);

    private static List<Fragment> Fragments(string example) =>
        JsonSerializer.Deserialize<FragmentsFile>(File.ReadAllText(ExamplePath(example, "fragmentos.json")), SnakeCase)!.Fragments;

    // Same extraction as error_codes() / warning_codes() in test_validador.py.
    private static HashSet<string> ErrorCodes(CatalogValidationReport report) =>
        report.Errors.Select(e =>
        {
            var head = e.Split(' ');
            return head[0].StartsWith("REQ-") || head[0].StartsWith("Q-") || head[0].StartsWith("DISCARDED[") || head[0].StartsWith("DOMAIN[")
                ? head[1]
                : head[0];
        }).ToHashSet();

    private static HashSet<string> WarningCodes(CatalogValidationReport report) =>
        report.Warnings.Select(w => w.Split(' ')).Where(p => p.Length > 1).Select(p => p[1]).ToHashSet();

    [Theory]
    [InlineData("ejemplo")]
    [InlineData("ejemplo_en")]
    public void Example_catalogs_are_valid_without_warnings(string example)
    {
        var report = CatalogValidator.Validate(File.ReadAllText(ExamplePath(example, "catalogo_ejemplo.json")), SystemNames[example], Fragments(example));

        Assert.True(report.Valid, string.Join("\n", report.Errors));
        Assert.Empty(report.Warnings);
        Assert.True(report.Metrics!.SelfCheckMatches);
    }

    [Theory]
    [InlineData("ejemplo")]
    [InlineData("ejemplo_en")]
    public void Rendered_texts_match_the_enriched_example(string example)
    {
        var catalog = File.ReadAllText(ExamplePath(example, "catalogo_ejemplo.json"));
        var report = CatalogValidator.Validate(catalog, SystemNames[example], Fragments(example));

        var expected = JsonNode.Parse(File.ReadAllText(ExamplePath(example, "catalogo_ejemplo.enriquecido.json")));
        var actual = JsonNode.Parse(CatalogValidator.Enrich(catalog, report.Texts));

        // Text by text first, for a readable failure; then the whole document.
        var expectedTexts = expected!["requirements"]!.AsArray().Select(r => r!["text"]!.GetValue<string>());
        var actualTexts = actual!["requirements"]!.AsArray().Select(r => r!["text"]!.GetValue<string>());
        Assert.Equal(expectedTexts, actualTexts);
        Assert.True(JsonNode.DeepEquals(expected, actual));
    }

    public record Mutation(string File, string Name, string Example, string Sistema, string ExpectedCode, string Kind);

    public static TheoryData<string> MutationFiles()
    {
        var data = new TheoryData<string>();
        foreach (var m in LoadManifest())
            data.Add(m.File);
        return data;
    }

    private static List<Mutation> LoadManifest() =>
        JsonSerializer.Deserialize<List<Mutation>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "catalog-mutations", "manifest.json")), SnakeCase)!;

    [Fact]
    public void Manifest_has_the_44_mutations_of_the_reference()
    {
        var manifest = LoadManifest();

        Assert.Equal(44, manifest.Count);
        Assert.Equal(42, manifest.Count(m => m.Kind == "error"));
        Assert.Equal(2, manifest.Count(m => m.Kind == "warning"));
    }

    [Theory]
    [MemberData(nameof(MutationFiles))]
    public void Each_mutation_gives_its_expected_code(string file)
    {
        var m = LoadManifest().Single(x => x.File == file);
        var catalog = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "catalog-mutations", file));

        var report = CatalogValidator.Validate(catalog, m.Sistema, Fragments(m.Example));

        if (m.Kind == "error")
        {
            Assert.False(report.Valid, $"{m.Name}: debería ser no válido");
            Assert.True(ErrorCodes(report).Contains(m.ExpectedCode), $"{m.Name}: esperado {m.ExpectedCode}, obtenido [{string.Join(", ", ErrorCodes(report))}]\n{string.Join("\n", report.Errors)}");
        }
        else
        {
            Assert.True(report.Valid, $"{m.Name}: debería ser válido\n{string.Join("\n", report.Errors)}");
            Assert.True(WarningCodes(report).Contains(m.ExpectedCode), $"{m.Name}: aviso esperado {m.ExpectedCode}, obtenidos [{string.Join(", ", WarningCodes(report))}]");
        }
    }

    private record ExpectedCodes(bool Valid, List<string> Errors, List<string> Warnings);

    // Stronger than the test above: the complete set of error and warning codes
    // must be the one the Python reference gives (expected-codes.json).
    [Theory]
    [MemberData(nameof(MutationFiles))]
    public void Each_mutation_gives_exactly_the_codes_of_the_reference(string file)
    {
        var m = LoadManifest().Single(x => x.File == file);
        var expected = JsonSerializer.Deserialize<Dictionary<string, ExpectedCodes>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "catalog-mutations", "expected-codes.json")), SnakeCase)![file];
        var catalog = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "catalog-mutations", file));

        var report = CatalogValidator.Validate(catalog, m.Sistema, Fragments(m.Example));

        Assert.Equal(expected.Valid, report.Valid);
        Assert.Equal(expected.Errors, ErrorCodes(report).Order(StringComparer.Ordinal));
        Assert.Equal(expected.Warnings, WarningCodes(report).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Text_that_is_not_json_is_reported_not_thrown()
    {
        var report = CatalogValidator.Validate("{ esto no es JSON", "el sistema X", null);

        Assert.False(report.Valid);
        Assert.StartsWith("JSON no válido", Assert.Single(report.Errors));
    }

    [Fact]
    public void Metrics_of_the_spanish_example()
    {
        var metrics = CatalogValidator.Validate(File.ReadAllText(ExamplePath("ejemplo", "catalogo_ejemplo.json")), SystemNames["ejemplo"], Fragments("ejemplo")).Metrics!;

        Assert.Equal(1.0, metrics.SyntacticConformance);
        Assert.Equal(1.0, metrics.RequirementsWithoutErrors);
        Assert.Equal("14/14", metrics.FragmentCoverage);
        Assert.Equal(metrics.Requirements, metrics.Branches.Values.Sum());
    }
}
