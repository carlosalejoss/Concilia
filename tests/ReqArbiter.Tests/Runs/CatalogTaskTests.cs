using System.Text.Json;
using ReqArbiter.Api.Runs;
using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Projects;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Tests.Runs;

public class CatalogTaskTests
{
    private static string Example(string example) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", example, "fuentes", "DOC-01.txt"));

    private static readonly Project Project = new()
    {
        Id = 1,
        Name = "ReservaSalas",
        SystemName = "el sistema ReservaSalas",
        Language = "es",
        Glossary = "sala: espacio reservable",
    };

    private static Run RunFor(LotPlan lot) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = 1,
        Slot = "A",
        LotNumber = lot.Number,
        LotTotal = lot.Total,
        Provider = "Fake",
        Model = "SS - GPT-6-SOL",
    };

    [Fact]
    public void Plan_fragments_every_document_and_covers_all_fragments_with_lots()
    {
        var plan = CatalogTask.Plan(
            [new CatalogDocument("DOC-01", "reservas.md", Example("ejemplo")), new CatalogDocument("DOC-02", "loans.txt", Example("ejemplo_en"))],
            new CatalogOptions());

        Assert.Equal("reservas.md", plan.DocumentNames["DOC-01"]);
        Assert.Contains(plan.Fragments, f => f.Id == "DOC-02#F001");
        Assert.Equal(plan.Fragments.Select(f => f.Id), plan.Lots.SelectMany(l => l.FragmentIds));
    }

    [Fact]
    public void Plan_survives_a_save_and_load()
    {
        using var temp = new TempDirectory();
        var plan = CatalogTask.Plan([new CatalogDocument("DOC-01", "DOC-01.txt", Example("ejemplo"))], new CatalogOptions());
        var path = Path.Combine(temp.Path, "job", CatalogTask.PlanFile);

        CatalogTask.SavePlan(path, plan);
        var loaded = CatalogTask.LoadPlan(path);

        Assert.Equal(plan.Fragments, loaded.Fragments);
        Assert.Equal(plan.Lots.Select(l => l.FragmentIds), loaded.Lots.Select(l => l.FragmentIds));
        Assert.Contains("\"source_ref\"", File.ReadAllText(path)); // same field names as the reference scripts
    }

    [Fact]
    public void Request_asks_only_for_write_and_the_catalog_file()
    {
        using var temp = new TempDirectory();
        var plan = CatalogTask.Plan([new CatalogDocument("DOC-01", "DOC-01.txt", Example("ejemplo"))], new CatalogOptions());

        var request = CatalogTask.BuildRequest(RunFor(plan.Lots[0]), Project, plan, temp.Path, new CatalogOptions(), 128_000);

        Assert.Equal(["Write"], request.AllowedTools);
        Assert.Equal([CatalogTask.OutputFile], request.ExpectedOutputFiles);
        Assert.Equal(TimeSpan.FromMinutes(15), request.HeartbeatTimeout);
        Assert.Equal(TimeSpan.FromMinutes(30), request.TotalTimeout);
        Assert.Equal("SS - GPT-6-SOL", request.ModelId);
    }

    [Fact]
    public void Prompt_carries_the_project_settings_and_the_lot()
    {
        using var temp = new TempDirectory();
        var plan = CatalogTask.Plan([new CatalogDocument("DOC-01", "DOC-01.txt", Example("ejemplo"))], new CatalogOptions());

        var prompt = CatalogTask.BuildRequest(RunFor(plan.Lots[0]), Project, plan, temp.Path, new CatalogOptions(), 128_000).Prompt;

        Assert.Contains("<nombre_sistema>el sistema ReservaSalas</nombre_sistema>", prompt);
        Assert.Contains("<glosario>sala: espacio reservable</glosario>", prompt);
        Assert.Contains("<lote numero=\"1\" total=\"1\"/>", prompt);
        Assert.Contains("<f id=\"DOC-01#F014\"", prompt);
    }

    [Fact]
    public void Each_lot_gets_only_its_fragments_in_prompt_and_input_file()
    {
        using var temp = new TempDirectory();
        var options = new CatalogOptions { MaxOutputTokensPerLot = 1_500 }; // several lots
        var plan = CatalogTask.Plan([new CatalogDocument("DOC-01", "DOC-01.txt", Example("ejemplo"))], options);
        var lot = plan.Lots[1];

        var request = CatalogTask.BuildRequest(RunFor(lot), Project, plan, temp.Path, options, 128_000);

        using var file = JsonDocument.Parse(File.ReadAllText(Path.Combine(temp.Path, CatalogTask.FragmentsFile)));
        var ids = file.RootElement.GetProperty("fragments").EnumerateArray().Select(f => f.GetProperty("id").GetString());
        Assert.Equal(lot.FragmentIds, ids);
        Assert.Contains($"<lote numero=\"2\" total=\"{plan.Lots.Count}\"/>", request.Prompt);

        // Only inside <documentacion>: the examples of §11 also contain <f id="DOC-01#F001" ...>.
        var start = request.Prompt.IndexOf("<documentacion>", StringComparison.Ordinal);
        var end = request.Prompt.IndexOf("</documentacion>", start, StringComparison.Ordinal);
        var documentation = request.Prompt[start..end];
        Assert.DoesNotContain($"<f id=\"{plan.Lots[0].FragmentIds[0]}\"", documentation);
        Assert.Contains($"<f id=\"{lot.FragmentIds[0]}\"", documentation);
    }
}
