using ReqArbiter.Core.Catalog;

namespace ReqArbiter.Tests.Catalog;

public class LotPlannerTests
{
    // Defaults of the milestone 5 specification.
    private const double Ratio = 15;
    private const int MaxTokens = 20_000;

    private static string ExampleText(string example) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", example, "fuentes", "DOC-01.txt"));

    // A synthetic long document: `sections` sections of `sentences` sentences of ~100 characters.
    private static string LongDocument(int sections, int sentences)
    {
        var sentence = "El sistema deberá registrar la reserva de la sala indicada por el empleado en el calendario común.";
        return string.Join("\n", Enumerable.Range(1, sections).Select(s =>
            $"{s}.1 {string.Join(" ", Enumerable.Repeat(sentence, sentences))}"));
    }

    [Fact]
    public void Estimate_follows_the_specification_formula()
    {
        // 4,666 characters x 15 / 3.5 = 19,997.1, rounded up: about the most one lot holds.
        Assert.Equal(19_998, LotPlanner.EstimateOutputTokens(4_666, Ratio));
    }

    [Fact]
    public void A_short_document_is_one_lot()
    {
        var fragments = Fragmenter.FragmentDocument("DOC-01", ExampleText("ejemplo"));

        var lot = Assert.Single(LotPlanner.Plan(fragments, Ratio, MaxTokens));

        Assert.Equal((1, 1), (lot.Number, lot.Total));
        Assert.Equal(fragments, lot.Fragments);
    }

    [Fact]
    public void A_long_document_is_split_without_losing_or_repeating_fragments()
    {
        var fragments = Fragmenter.FragmentDocument("DOC-01", LongDocument(sections: 30, sentences: 5));

        var lots = LotPlanner.Plan(fragments, Ratio, MaxTokens);

        Assert.True(lots.Count > 1);
        Assert.Equal(fragments, lots.SelectMany(l => l.Fragments)); // same fragments, same order, once each
        Assert.All(lots, l => Assert.Equal(lots.Count, l.Total));
        Assert.Equal(Enumerable.Range(1, lots.Count), lots.Select(l => l.Number));
        Assert.All(lots, l => Assert.True(
            LotPlanner.EstimateOutputTokens(l.Fragments.Sum(f => f.Text.Length), Ratio) <= MaxTokens));
    }

    [Fact]
    public void Sections_that_fit_are_not_split_between_lots()
    {
        var fragments = Fragmenter.FragmentDocument("DOC-01", LongDocument(sections: 30, sentences: 5));

        var lots = LotPlanner.Plan(fragments, Ratio, MaxTokens);

        // Each section appears in exactly one lot.
        var sectionsPerLot = lots.SelectMany(l => l.Fragments.Select(f => f.Section).Distinct().Select(s => (s, l.Number)));
        Assert.All(sectionsPerLot.GroupBy(x => x.s), g => Assert.Single(g.Select(x => x.Number).Distinct()));
    }

    [Fact]
    public void A_section_too_big_for_a_lot_is_cut_between_fragments()
    {
        // One section of 80 sentences (~8,000 characters): more than one lot.
        var fragments = Fragmenter.FragmentDocument("DOC-01", LongDocument(sections: 1, sentences: 80));

        var lots = LotPlanner.Plan(fragments, Ratio, MaxTokens);

        Assert.True(lots.Count > 1);
        Assert.Equal(fragments, lots.SelectMany(l => l.Fragments));
    }

    [Fact]
    public void Small_documents_share_a_lot()
    {
        var doc1 = Fragmenter.FragmentDocument("DOC-01", ExampleText("ejemplo"));
        var doc2 = Fragmenter.FragmentDocument("DOC-02", ExampleText("ejemplo_en"));

        var lot = Assert.Single(LotPlanner.Plan([.. doc1, .. doc2], Ratio, MaxTokens));

        Assert.Equal(doc1.Count + doc2.Count, lot.Fragments.Count);
    }

    [Fact]
    public void Doubts_are_not_part_of_any_lot()
    {
        var fragments = Fragmenter.FragmentDocument("DOC-01", "El sistema reserva salas.")
            .Concat(Fragmenter.FragmentDoubts([("D-001", "¿Quién?", "El administrador.")]))
            .ToList();

        var lot = Assert.Single(LotPlanner.Plan(fragments, Ratio, MaxTokens));

        Assert.All(lot.Fragments, f => Assert.Equal(Fragmenter.DocumentKind, f.Kind));
    }

    [Fact]
    public void No_fragments_gives_no_lots()
    {
        Assert.Empty(LotPlanner.Plan([], Ratio, MaxTokens));
    }
}
