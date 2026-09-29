namespace ReqArbiter.Core.Catalog;

// A group of fragments processed by one run of each model. Number is 1-based.
public record Lot(int Number, int Total, IReadOnlyList<Fragment> Fragments);

// Claude Code caps each answer at 32k output tokens, which limits the size of
// the catalog one run can write. The documentation is therefore split into
// lots before launching models A and B.
public static class LotPlanner
{
    public const double CharactersPerToken = 3.5;

    // Estimated output tokens of a lot: the catalog is several times longer
    // than its source text (outputRatio; the ReservaSalas example measures 10.7).
    public static int EstimateOutputTokens(int sourceCharacters, double outputRatio) =>
        (int)Math.Ceiling(sourceCharacters * outputRatio / CharactersPerToken);

    // Groups the document fragments into lots of at most maxOutputTokens
    // (estimated). The unit is a section (consecutive fragments of the same
    // document and section): whole sections go together while they fit, so
    // small documents can share a lot. A section that does not fit on its own
    // is cut between fragments. A fragment is never split.
    public static IReadOnlyList<Lot> Plan(IReadOnlyList<Fragment> fragments, double outputRatio, int maxOutputTokens)
    {
        var groups = new List<List<Fragment>>();
        var current = new List<Fragment>();
        var currentTokens = 0;

        int Tokens(IEnumerable<Fragment> fs) => EstimateOutputTokens(fs.Sum(f => f.Text.Length), outputRatio);

        void Close()
        {
            if (current.Count > 0)
            {
                groups.Add(current);
                current = [];
                currentTokens = 0;
            }
        }

        foreach (var section in Sections(fragments.Where(f => f.Kind == Fragmenter.DocumentKind)))
        {
            var sectionTokens = Tokens(section);

            if (sectionTokens > maxOutputTokens)
            {
                // Too big for any lot: it starts its own lots, cut by fragments.
                Close();
                foreach (var fragment in section)
                {
                    var fragmentTokens = Tokens([fragment]);
                    if (current.Count > 0 && currentTokens + fragmentTokens > maxOutputTokens)
                    {
                        Close();
                    }
                    current.Add(fragment);
                    currentTokens += fragmentTokens;
                }
                Close();
                continue;
            }

            if (current.Count > 0 && currentTokens + sectionTokens > maxOutputTokens)
            {
                Close();
            }
            current.AddRange(section);
            currentTokens += sectionTokens;
        }
        Close();

        return groups.Select((g, i) => new Lot(i + 1, groups.Count, g)).ToList();
    }

    // Consecutive fragments of the same document and section.
    private static IEnumerable<List<Fragment>> Sections(IEnumerable<Fragment> fragments)
    {
        var section = new List<Fragment>();
        foreach (var fragment in fragments)
        {
            if (section.Count > 0 && (fragment.Source != section[0].Source || fragment.Section != section[0].Section))
            {
                yield return section;
                section = [];
            }
            section.Add(fragment);
        }
        if (section.Count > 0)
        {
            yield return section;
        }
    }
}
