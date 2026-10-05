using System.Text.Json;
using System.Text.Json.Nodes;
using ReqArbiter.Core.Catalog;

namespace ReqArbiter.Tests.Catalog;

// The C# validator (v1.3) must agree with the reference validar_catalogo.py:
// the two example catalogs are valid without warnings, their rendered texts
// match the enriched files, normalizing twice changes nothing, and for every
// mutation exported by test_validador.py the outcome is exactly the one in
// expected-outcomes.json (levels, codes, rejected elements, accepted catalog,
// texts, ids and metrics).
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

    private static string MutationsPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "catalog-mutations", file);

    private record FragmentsFile(List<Fragment> Fragments);

    private static List<Fragment> Fragments(string example) =>
        JsonSerializer.Deserialize<FragmentsFile>(File.ReadAllText(ExamplePath(example, "fragmentos.json")), SnakeCase)!.Fragments;

    // Same extraction as code_of() in test_validador.py: "RF-001 C14 …" -> "C14".
    private static string CodeOf(string message)
    {
        var parts = message.Split(' ');
        return parts.Length > 1 ? parts[1] : parts[0];
    }

    private static CatalogValidationReport ValidateExample(string example) =>
        CatalogValidator.Validate(File.ReadAllText(ExamplePath(example, "catalogo_ejemplo_v1.3.json")), SystemNames[example], Fragments(example));

    [Theory]
    [InlineData("ejemplo")]
    [InlineData("ejemplo_en")]
    public void Example_catalogs_are_accepted_without_warnings(string example)
    {
        var report = ValidateExample(example);

        Assert.True(report.Valid, string.Join("\n", report.Errors));
        Assert.False(report.NeedsRepair, string.Join("\n", report.RepairReasons));
        Assert.Empty(report.Rejected);
        Assert.Empty(report.Warnings);
        Assert.True(report.Metrics!.SelfCheckMatches);
    }

    [Theory]
    [InlineData("ejemplo")]
    [InlineData("ejemplo_en")]
    public void Rendered_texts_match_the_enriched_example(string example)
    {
        var report = ValidateExample(example);

        var expected = JsonNode.Parse(File.ReadAllText(ExamplePath(example, "catalogo_ejemplo_v1.3.enriquecido.json")));
        var actual = JsonNode.Parse(CatalogValidator.Enrich(report.Catalog!, report.Texts));

        // Text by text first, for a readable failure; then the whole document.
        var expectedTexts = expected!["requirements"]!.AsArray().Select(r => r!["text"]!.GetValue<string>());
        var actualTexts = actual!["requirements"]!.AsArray().Select(r => r!["text"]!.GetValue<string>());
        Assert.Equal(expectedTexts, actualTexts);
        Assert.True(JsonNode.DeepEquals(expected, actual));
    }

    [Theory]
    [InlineData("ejemplo")]
    [InlineData("ejemplo_en")]
    public void Normalizing_twice_changes_nothing(string example)
    {
        var first = ValidateExample(example);

        var again = CatalogValidator.Validate(first.Catalog!, SystemNames[example], Fragments(example));

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(first.Catalog!), JsonNode.Parse(again.Catalog!)));
        Assert.Empty(again.Warnings);
    }

    public record Case(string File, string Name, string Example, string Sistema, string Kind, string? Target, string? ExpectedCode);

    private static List<Case> LoadManifest() =>
        JsonSerializer.Deserialize<List<Case>>(File.ReadAllText(MutationsPath("manifest.json")), SnakeCase)!;

    private static readonly Lazy<JsonObject> Outcomes = new(() =>
        JsonNode.Parse(File.ReadAllText(MutationsPath("expected-outcomes.json")))!.AsObject());

    public static TheoryData<string> CaseFiles()
    {
        var data = new TheoryData<string>();
        foreach (var c in LoadManifest())
            data.Add(c.File);
        return data;
    }

    [Fact]
    public void Manifest_has_the_63_cases_of_the_reference()
    {
        var manifest = LoadManifest();

        Assert.Equal(63, manifest.Count);
        Assert.Equal(63, Outcomes.Value.Count);
    }

    private static CatalogValidationReport ValidateCase(Case c) =>
        CatalogValidator.Validate(File.ReadAllText(MutationsPath(c.File)), c.Sistema, Fragments(c.Example));

    // The level the reference expects for this case (the same check as outcome_ok() in test_validador.py).
    [Theory]
    [MemberData(nameof(CaseFiles))]
    public void Each_case_gets_its_expected_level(string file)
    {
        var c = LoadManifest().Single(x => x.File == file);
        var r = ValidateCase(c);
        var detail = $"{c.Name}\nlote: {string.Join(" | ", r.Errors)}\nrechazos: {string.Join(" | ", r.Rejected.Select(x => $"{x.Id}: {string.Join(", ", x.Errors)}"))}\navisos: {string.Join(" | ", r.Warnings)}\nreparación: {string.Join(" | ", r.RepairReasons)}";

        switch (c.Kind)
        {
            case "reject":
                Assert.True(r.Valid && r.Rejected.Any(x => x.Id == c.Target && x.Errors.Any(e => e.Split(' ')[0] == c.ExpectedCode)), detail);
                break;
            case "lot":
                Assert.True(!r.Valid && r.NeedsRepair && r.Errors.Any(e => CodeOf(e) == c.ExpectedCode), detail);
                break;
            case "warning" or "fix":
                Assert.True(r.Valid && !r.NeedsRepair && r.Rejected.Count == 0 && r.Warnings.Any(w => CodeOf(w) == c.ExpectedCode), detail);
                break;
            case "accept":
                Assert.True(r.Valid && !r.NeedsRepair && r.Rejected.Count == 0 && r.Warnings.Count == 0, detail);
                break;
            case "repair":
                Assert.True(r.Valid && r.NeedsRepair && r.RepairReasons.Any(x => CodeOf(x) == c.ExpectedCode), detail);
                break;
            default:
                throw new InvalidOperationException(c.Kind);
        }
    }

    // Stronger than the test above: everything the reference gives must match.
    [Theory]
    [MemberData(nameof(CaseFiles))]
    public void Each_case_matches_the_reference_exactly(string file)
    {
        var c = LoadManifest().Single(x => x.File == file);
        var expected = Outcomes.Value[file]!.AsObject();
        var r = ValidateCase(c);

        static List<string> Sorted(JsonNode? node) => node!.AsArray().Select(x => x!.GetValue<string>()).ToList();

        Assert.Equal(expected["valid"]!.GetValue<bool>(), r.Valid);
        Assert.Equal(expected["needs_repair"]!.GetValue<bool>(), r.NeedsRepair);
        Assert.Equal(Sorted(expected["lot"]), r.Errors.Select(CodeOf).Distinct().Order(StringComparer.Ordinal));
        Assert.Equal(Sorted(expected["warnings"]), r.Warnings.Select(CodeOf).Distinct().Order(StringComparer.Ordinal));
        Assert.Equal(Sorted(expected["repair"]), r.RepairReasons.Select(CodeOf).Distinct().Order(StringComparer.Ordinal));

        var expectedRejected = expected["rejected"]!.AsObject().ToDictionary(p => p.Key, p => Sorted(p.Value));
        var actualRejected = r.Rejected.GroupBy(x => x.Id).ToDictionary(
            g => g.Key, g => g.SelectMany(x => x.Errors).Select(e => e.Split(' ')[0]).Distinct().Order(StringComparer.Ordinal).ToList());
        Assert.Equal(expectedRejected.OrderBy(p => p.Key), actualRejected.OrderBy(p => p.Key));

        // The accepted catalog, normalized and renumbered, and its texts.
        Assert.True(JsonNode.DeepEquals(expected["catalog"], r.Catalog is null ? null : JsonNode.Parse(r.Catalog)),
            $"catálogo aceptado distinto\nesperado {expected["catalog"]?.ToJsonString()}\nobtenido {r.Catalog}");
        var expectedTexts = expected["texts"]!.AsObject().ToDictionary(p => p.Key, p => p.Value?.GetValue<string>());
        Assert.Equal(expectedTexts.OrderBy(p => p.Key), r.Texts.OrderBy(p => p.Key));
        var expectedIds = expected["ids"]!.AsObject().ToDictionary(p => p.Key, p => p.Value!.GetValue<string>());
        Assert.Equal(expectedIds.OrderBy(p => p.Key), r.Ids.OrderBy(p => p.Key));

        // Metrics, field by field (numbers within rounding).
        var expectedMetrics = expected["metrics"]!.AsObject();
        if (r.Metrics is null)
        {
            Assert.Empty(expectedMetrics);
            return;
        }
        var actualMetrics = JsonSerializer.SerializeToNode(r.Metrics)!.AsObject();
        foreach (var (name, value) in expectedMetrics)
        {
            var actual = actualMetrics[name];
            if (value is JsonValue v && v.TryGetValue<double>(out var number))
                Assert.True(Math.Abs(number - actual!.GetValue<double>()) < 1e-9, $"{name}: {number} ≠ {actual}");
            else
                Assert.True(JsonNode.DeepEquals(value, actual), $"{name}: {value?.ToJsonString()} ≠ {actual?.ToJsonString()}");
        }
        Assert.Equal(expectedMetrics.Count, actualMetrics.Count);
    }

    [Fact]
    public void Text_that_is_not_json_is_a_lot_error_not_an_exception()
    {
        var report = CatalogValidator.Validate("{ esto no es JSON", "el sistema X", null);

        Assert.False(report.Valid);
        Assert.True(report.NeedsRepair);
        Assert.StartsWith("LOTE JSON no válido", Assert.Single(report.Errors));
    }

    [Fact]
    public void Metrics_of_the_spanish_example()
    {
        var metrics = ValidateExample("ejemplo").Metrics!;

        Assert.Equal(9, metrics.Requirements);
        Assert.Equal(7, metrics.ByType["RF"]);
        Assert.Equal("14/14", metrics.FragmentCoverage);
        Assert.Equal("6/6", metrics.CapabilityCoverage);
        Assert.Equal(1.0, metrics.RequirementsWithoutErrors);
        Assert.Equal(metrics.Requirements, metrics.Branches.Values.Sum());
    }

    [Theory]
    [InlineData("[DATA] El empleado indica la fecha.", "DATA", "El empleado indica la fecha.")]
    [InlineData("data: El empleado indica la fecha.", "DATA", "El empleado indica la fecha.")]
    [InlineData("[Error]: Se rechaza la reserva.", "ERROR", "Se rechaza la reserva.")]
    [InlineData("Error messages are shown to the librarian.", null, "Error messages are shown to the librarian.")]
    public void A_label_needs_brackets_or_a_colon(string criterion, string? label, string text)
    {
        Assert.Equal((label, text), CatalogValidator.SplitLabel(criterion));
    }
}
