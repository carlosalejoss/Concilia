using ReqArbiter.Core.Catalog;

namespace ReqArbiter.Tests.Catalog;

// The catalogs FakeProvider writes must go through the real validator: valid
// ones for the normal flow, and an invalid one to simulate a repair.
public class FakeCatalogTests
{
    private static string ExampleFragments =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fragmentos.json"));

    // A run folder with input/fragmentos.json as BuildRequest writes it.
    private static string RunFolder(TempDirectory temp, string system, string language, string fragmentsJson)
    {
        var input = Path.Combine(temp.Path, "input");
        Directory.CreateDirectory(input);
        var node = System.Text.Json.Nodes.JsonNode.Parse(fragmentsJson)!.AsObject();
        node["system"] = system;
        node["language"] = language;
        File.WriteAllText(Path.Combine(input, "fragmentos.json"), node.ToJsonString());
        return temp.Path;
    }

    private static List<Fragment> Fragments(string folder) =>
        System.Text.Json.JsonSerializer.Deserialize<FragmentsFile>(
            File.ReadAllText(Path.Combine(folder, "input", "fragmentos.json")),
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower })!.Fragments;

    private record FragmentsFile(List<Fragment> Fragments);

    [Fact]
    public void The_reservasalas_example_gets_its_catalog_with_the_project_system_name()
    {
        using var temp = new TempDirectory();
        var folder = RunFolder(temp, "el sistema de reservas", "es", ExampleFragments);

        var catalog = FakeCatalog.For(folder, mutated: false);
        var report = CatalogValidator.Validate(catalog, "el sistema de reservas", Fragments(folder));

        Assert.True(report.Valid, string.Join("\n", report.Errors));
        Assert.Equal(13, report.Metrics!.Requirements);
    }

    [Fact]
    public void Any_other_input_gets_a_valid_catalog_that_discards_every_fragment()
    {
        using var temp = new TempDirectory();
        var fragments = Fragmenter.FragmentDocument("DOC-01", "# Otro sistema\n\nEl operador registra los pedidos.\n\nEl sistema envía un aviso.\n");
        var json = System.Text.Json.JsonSerializer.Serialize(new { fragments },
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower });
        var folder = RunFolder(temp, "el sistema Pedidos", "es", json);

        var catalog = FakeCatalog.For(folder, mutated: false);
        var report = CatalogValidator.Validate(catalog, "el sistema Pedidos", Fragments(folder));

        Assert.True(report.Valid, string.Join("\n", report.Errors));
        Assert.Equal(0, report.Metrics!.Requirements);
        Assert.Equal($"{fragments.Count}/{fragments.Count}", report.Metrics.FragmentCoverage);
    }

    [Fact]
    public void The_mutated_catalog_is_invalid()
    {
        using var temp = new TempDirectory();
        var folder = RunFolder(temp, "el sistema ReservaSalas", "es", ExampleFragments);

        var report = CatalogValidator.Validate(FakeCatalog.For(folder, mutated: true), "el sistema ReservaSalas", Fragments(folder));

        Assert.False(report.Valid);
        Assert.Contains(report.Errors, e => e.StartsWith("SCHEMA", StringComparison.Ordinal));
    }
}
