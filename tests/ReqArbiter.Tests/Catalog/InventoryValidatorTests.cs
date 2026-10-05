using System.Text.Json;
using System.Text.Json.Nodes;
using ReqArbiter.Core.Catalog;

namespace ReqArbiter.Tests.Catalog;

// The C# inventory validator (v2) must agree with the reference
// validar_inventario.py: the two example inventories are valid without
// warnings, normalizing twice changes nothing, and for every mutation exported
// by test_validador.py the outcome is exactly the one in expected-outcomes.json
// (levels, codes, rejected questions, normalized inventory, ids and metrics).
public class InventoryValidatorTests
{
    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static readonly Dictionary<string, string> FragmentFiles = new()
    {
        ["ejemplo"] = "fragmentos_v2.json",
        ["ejemplo_en"] = "fragmentos.json",
    };

    private static string ExamplePath(string example, string file) =>
        Path.Combine(AppContext.BaseDirectory, "prompts", example, file);

    private static string MutationsPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "inventory-mutations", file);

    private record FragmentsFile(List<Fragment> Fragments);

    private static List<Fragment> Fragments(string example, string file) =>
        JsonSerializer.Deserialize<FragmentsFile>(File.ReadAllText(ExamplePath(example, file)), SnakeCase)!.Fragments;

    // Same extraction as code_of() in test_validador.py: "CAP-001 REP …" -> "REP".
    private static string CodeOf(string message)
    {
        var parts = message.Split(' ');
        return parts.Length > 1 ? parts[1] : parts[0];
    }

    private static InventoryValidationReport ValidateExample(string example) =>
        InventoryValidator.Validate(File.ReadAllText(ExamplePath(example, "inventario_ejemplo_v2.json")), Fragments(example, FragmentFiles[example]));

    [Theory]
    [InlineData("ejemplo")]
    [InlineData("ejemplo_en")]
    public void Example_inventories_are_accepted_without_warnings(string example)
    {
        var report = ValidateExample(example);

        Assert.True(report.Valid, string.Join("\n", report.Errors));
        Assert.False(report.NeedsRepair);
        Assert.Empty(report.Rejected);
        Assert.Empty(report.Warnings);
    }

    [Theory]
    [InlineData("ejemplo")]
    [InlineData("ejemplo_en")]
    public void Normalizing_twice_changes_nothing(string example)
    {
        var first = ValidateExample(example);

        var again = InventoryValidator.Validate(first.Inventory!, Fragments(example, FragmentFiles[example]));

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(first.Inventory!), JsonNode.Parse(again.Inventory!)));
        Assert.Empty(again.Warnings);
    }

    [Fact]
    public void Metrics_of_the_spanish_example()
    {
        var metrics = ValidateExample("ejemplo").Metrics!;

        Assert.Equal(6, metrics.Capabilities);
        Assert.Equal(4, metrics.ByKind["functional"]);
        Assert.Equal("15/21", metrics.AssignedFragments);
        Assert.Equal(0, metrics.PossibleRepetitions);
    }

    [Fact]
    public void Text_that_is_not_json_is_an_inventory_error_not_an_exception()
    {
        var report = InventoryValidator.Validate("{ esto no es JSON", null);

        Assert.False(report.Valid);
        Assert.True(report.NeedsRepair);
        Assert.StartsWith("INVENTARIO JSON no válido", Assert.Single(report.Errors));
    }

    public record Case(string File, string Name, string Example, string Fragments, string Kind, string? Target, string? ExpectedCode);

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
    public void Manifest_has_the_22_cases_of_the_reference()
    {
        Assert.Equal(22, LoadManifest().Count);
        Assert.Equal(22, Outcomes.Value.Count);
    }

    private static InventoryValidationReport ValidateCase(Case c) =>
        InventoryValidator.Validate(File.ReadAllText(MutationsPath(c.File)), Fragments(c.Example, c.Fragments));

    // The level the reference expects for this case (outcome_ok() in test_validador.py).
    [Theory]
    [MemberData(nameof(CaseFiles))]
    public void Each_case_gets_its_expected_level(string file)
    {
        var c = LoadManifest().Single(x => x.File == file);
        var r = ValidateCase(c);
        var detail = $"{c.Name}\nerrores: {string.Join(" | ", r.Errors)}\nrechazos: {string.Join(" | ", r.Rejected.Select(x => $"{x.Id}: {string.Join(", ", x.Errors)}"))}\navisos: {string.Join(" | ", r.Warnings)}";

        var ok = c.Kind switch
        {
            "reject" => r.Valid && r.Rejected.Any(x => x.Id == c.Target && x.Errors.Any(e => e.Split(' ')[0] == c.ExpectedCode)),
            "lot" => !r.Valid && r.NeedsRepair && r.Errors.Any(e => CodeOf(e) == c.ExpectedCode),
            "warning" or "fix" => r.Valid && !r.NeedsRepair && r.Rejected.Count == 0 && r.Warnings.Any(w => CodeOf(w) == c.ExpectedCode),
            "accept" => r.Valid && !r.NeedsRepair && r.Rejected.Count == 0 && r.Warnings.Count == 0,
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
        var actualRejected = r.Rejected.ToDictionary(
            x => x.Id, x => x.Errors.Select(e => e.Split(' ')[0]).Distinct().Order(StringComparer.Ordinal).ToList());
        Assert.Equal(expectedRejected.OrderBy(p => p.Key), actualRejected.OrderBy(p => p.Key));

        Assert.True(JsonNode.DeepEquals(expected["inventory"], r.Inventory is null ? null : JsonNode.Parse(r.Inventory)),
            $"inventario distinto\nesperado {expected["inventory"]?.ToJsonString()}\nobtenido {r.Inventory}");
        var expectedIds = expected["ids"]!.AsObject().ToDictionary(p => p.Key, p => p.Value!.GetValue<string>());
        Assert.Equal(expectedIds.OrderBy(p => p.Key), r.Ids.OrderBy(p => p.Key));

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
