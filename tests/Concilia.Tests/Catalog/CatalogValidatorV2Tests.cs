using System.Text.Json;
using System.Text.Json.Nodes;
using Concilia.Core.Catalog;

namespace Concilia.Tests.Catalog;

// The C# catalog validator v2 must agree with the reference validar_catalogo.py
// (v2): the two example catalogs are valid without warnings, their rendered
// texts match the enriched files, normalizing twice changes nothing, and for
// every mutation exported by test_validador.py the outcome is exactly the one
// in expected-outcomes.json (levels, codes, rejected elements, accepted
// catalog, texts, ids, metrics, objections and capabilities without [ERROR]).
public class CatalogValidatorV2Tests
{
    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static readonly Dictionary<string, (string System, string Fragments)> Examples = new()
    {
        ["ejemplo"] = ("el sistema ReservaSalas", "fragmentos_v2.json"),
        ["ejemplo_en"] = ("the LibraryLoan system", "fragmentos.json"),
    };

    private const string InventoryFile = "inventario_ejemplo_v2.json";

    private static string ExamplePath(string example, string file) =>
        Path.Combine(AppContext.BaseDirectory, "prompts", example, file);

    private static string MutationsPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "catalog-v2-mutations", file);

    private record FragmentsFile(List<Fragment> Fragments);

    private static List<Fragment> Fragments(string example, string file) =>
        JsonSerializer.Deserialize<FragmentsFile>(File.ReadAllText(ExamplePath(example, file)), SnakeCase)!.Fragments;

    private static string Inventory(string example, string file = InventoryFile) =>
        File.ReadAllText(ExamplePath(example, file));

    // Same extraction as code_of() in test_validador.py: "RF-001 C14 …" -> "C14".
    private static string CodeOf(string message)
    {
        var parts = message.Split(' ');
        return parts.Length > 1 ? parts[1] : parts[0];
    }

    private static CatalogV2ValidationReport ValidateExample(string example) =>
        CatalogValidatorV2.Validate(
            File.ReadAllText(ExamplePath(example, "catalogo_ejemplo_v2.json")), Examples[example].System,
            Fragments(example, Examples[example].Fragments), Inventory(example));

    [Theory]
    [InlineData("ejemplo", new[] { "CAP-002", "CAP-003", "CAP-004" })]
    [InlineData("ejemplo_en", new[] { "CAP-001", "CAP-003", "CAP-004", "CAP-005" })]
    public void Example_catalogs_are_accepted_without_warnings(string example, string[] withoutError)
    {
        var report = ValidateExample(example);

        Assert.True(report.Valid, string.Join("\n", report.Errors));
        Assert.False(report.NeedsRepair, string.Join("\n", report.RepairReasons));
        Assert.Empty(report.Rejected);
        Assert.Empty(report.Warnings);
        Assert.Equal(withoutError, report.CapabilitiesWithoutError);
    }

    [Theory]
    [InlineData("ejemplo")]
    [InlineData("ejemplo_en")]
    public void Rendered_texts_match_the_enriched_example(string example)
    {
        var report = ValidateExample(example);

        var expected = JsonNode.Parse(File.ReadAllText(ExamplePath(example, "catalogo_ejemplo_v2.enriquecido.json")));
        var actual = JsonNode.Parse(CatalogValidatorV2.Enrich(report.Catalog!, report.Texts));

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

        var again = CatalogValidatorV2.Validate(first.Catalog!, Examples[example].System,
            Fragments(example, Examples[example].Fragments), Inventory(example));

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(first.Catalog!), JsonNode.Parse(again.Catalog!)));
        Assert.Empty(again.Warnings);
    }

    [Fact]
    public void Text_that_is_not_json_is_a_lot_error_not_an_exception()
    {
        var report = CatalogValidatorV2.Validate("{ esto no es JSON", "el sistema X", null, Inventory("ejemplo"));

        Assert.False(report.Valid);
        Assert.True(report.NeedsRepair);
        Assert.StartsWith("LOTE JSON no válido", Assert.Single(report.Errors));
    }

    public record Case(
        string File, string Name, string Example, string Sistema, string Fragments, string Inventory,
        List<string>? Lot, string Kind, string? Target, string? ExpectedCode);

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
    public void Manifest_has_the_75_cases_of_the_reference()
    {
        Assert.Equal(75, LoadManifest().Count);
        Assert.Equal(75, Outcomes.Value.Count);
    }

    private static CatalogV2ValidationReport ValidateCase(Case c) =>
        CatalogValidatorV2.Validate(File.ReadAllText(MutationsPath(c.File)), c.Sistema,
            Fragments(c.Example, c.Fragments), Inventory(c.Example, c.Inventory), c.Lot);

    // The level the reference expects for this case (outcome_ok() in test_validador.py).
    [Theory]
    [MemberData(nameof(CaseFiles))]
    public void Each_case_gets_its_expected_level(string file)
    {
        var c = LoadManifest().Single(x => x.File == file);
        var r = ValidateCase(c);
        var detail = $"{c.Name}\nlote: {string.Join(" | ", r.Errors)}\nrechazos: {string.Join(" | ", r.Rejected.Select(x => $"{x.Id}: {string.Join(", ", x.Errors)}"))}\navisos: {string.Join(" | ", r.Warnings)}\nreparación: {string.Join(" | ", r.RepairReasons)}";

        var ok = c.Kind switch
        {
            "reject" => r.Valid && r.Rejected.Any(x => x.Id == c.Target && x.Errors.Any(e => e.Split(' ')[0] == c.ExpectedCode)),
            "lot" => !r.Valid && r.NeedsRepair && r.Errors.Any(e => CodeOf(e) == c.ExpectedCode),
            "warning" or "fix" => r.Valid && !r.NeedsRepair && r.Rejected.Count == 0 && r.Warnings.Any(w => CodeOf(w) == c.ExpectedCode),
            "accept" => r.Valid && !r.NeedsRepair && r.Rejected.Count == 0 && r.Warnings.Count == 0,
            "repair" => r.Valid && r.NeedsRepair && r.RepairReasons.Any(x => CodeOf(x) == c.ExpectedCode),
            _ => throw new InvalidOperationException(c.Kind),
        };
        Assert.True(ok, detail);
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

        Assert.True(JsonNode.DeepEquals(expected["catalog"], r.Catalog is null ? null : JsonNode.Parse(r.Catalog)),
            $"catálogo aceptado distinto\nesperado {expected["catalog"]?.ToJsonString()}\nobtenido {r.Catalog}");
        var expectedTexts = expected["texts"]!.AsObject().ToDictionary(p => p.Key, p => p.Value?.GetValue<string>());
        Assert.Equal(expectedTexts.OrderBy(p => p.Key), r.Texts.OrderBy(p => p.Key));
        var expectedIds = expected["ids"]!.AsObject().ToDictionary(p => p.Key, p => p.Value!.GetValue<string>());
        Assert.Equal(expectedIds.OrderBy(p => p.Key), r.Ids.OrderBy(p => p.Key));
        Assert.Equal(expected["objections"]!.AsArray().Select(o => o!["capability_id"]!.GetValue<string>()), r.Objections);
        Assert.Equal(Sorted(expected["capabilities_without_error"]), r.CapabilitiesWithoutError);

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
}
