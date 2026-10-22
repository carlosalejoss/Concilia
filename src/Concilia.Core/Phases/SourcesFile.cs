using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Concilia.Core.Phases;

// input/fuentes.txt (step 107, prompts v0.12): the fragments of the
// documentation that the arbiter of the catalog may need, instead of the whole
// documentation. In the real test of the v0.11 (22-10-2026) the arbiter
// consulted 34 ranges of input/documentacion.txt (1,242 lines), filled its
// context, compacted and had to start again, although the prompt told it not
// to read it all: it does not know which fragments it needs. Of the 451
// requirements of the two proposals, 298 had a pair "mismo" and do not need
// the source.
//
// So the platform gives it only the fragments cited by the requirements it has
// to decide: those without any pair "mismo" in the matching, and those of a
// pair "mismo" whose texts carry different numbers ("1 hora" and "24 horas"
// come out "mismo": the embeddings measure how alike two texts are, not
// whether they contradict each other). Same format as the documentation, with
// the header of each document, so even reading it all the arbiter stays well
// within its context.
public static partial class SourcesFile
{
    public const string Path = "input/fuentes.txt";

    // What the file says when no requirement needs the source.
    public const string Empty = "(Ningún requisito necesita consultar la fuente: todos tienen un par «mismo» con las mismas cifras.)\n";

    // The file for the two proposals (without prefixes) and the matching
    // (MatchingFile, with prefixes). Without a matching every requirement
    // needs the source.
    public static string Build(string documentation, string proposal1, string proposal2, string matching)
    {
        var requirements = Requirements(proposal1, 1).Concat(Requirements(proposal2, 2)).ToDictionary(r => r.Id);
        var fragments = ToCheck(requirements, matching)
            .SelectMany(id => requirements[id].Sources)
            .ToHashSet();
        return Filter(documentation, fragments);
    }

    // The requirements that need the source, with the prefix of their
    // proposal ("1:RF-003"): those without a pair "mismo", and those with a
    // pair "mismo" whose numbers differ.
    public static IReadOnlySet<string> ToCheck(IReadOnlyDictionary<string, Requirement> requirements, string matching)
    {
        var same = new Dictionary<string, List<string>>(); // each element and its pairs "mismo"
        foreach (var line in matching.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split(' ');
            if (parts.Length < 3 || parts[2] != "mismo")
                continue;
            Add(same, parts[0], parts[1]);
            Add(same, parts[1], parts[0]);
        }

        bool Settled(string id) =>
            same.TryGetValue(id, out var pairs)
            && pairs.All(other => requirements.TryGetValue(other, out var o) && Numbers(o.Text).SetEquals(Numbers(requirements[id].Text)));
        return requirements.Keys.Where(id => !Settled(id)).ToHashSet();
    }

    // The lines of the documentation whose fragment is in the set (also its
    // "sigue" lines), each document under its header, in the same order.
    public static string Filter(string documentation, IReadOnlySet<string> fragments)
    {
        var text = new StringBuilder();
        string? header = null;
        string? written = null;
        foreach (var line in documentation.Split('\n'))
        {
            if (line.StartsWith("=== ", StringComparison.Ordinal))
            {
                header = line;
                continue;
            }
            var id = LineId.Match(line);
            if (!id.Success || !fragments.Contains(id.Groups[1].Value))
                continue;
            if (header is not null && header != written)
            {
                text.Append(header).Append('\n');
                written = header;
            }
            text.Append(line).Append('\n');
        }
        return text.Length > 0 ? text.ToString() : Empty;
    }

    // A requirement of a proposal: its id with the prefix, its text and the fragments it cites.
    public record Requirement(string Id, string Text, IReadOnlyList<string> Sources);

    // The requirements of a proposal ({"requisitos":[{"id","texto","fuentes"}]}),
    // with "n:" before their ids as in the files the arbiter reads. Text that
    // is not JSON has none.
    public static IEnumerable<Requirement> Requirements(string proposal, int n)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(proposal);
        }
        catch (JsonException)
        {
            yield break;
        }
        if (root?["requisitos"] is not JsonArray requirements)
            yield break;
        foreach (var r in requirements.OfType<JsonObject>())
        {
            if (r["id"] is not JsonValue id || !id.TryGetValue<string>(out var value))
                continue;
            var sources = (r["fuentes"] as JsonArray ?? [])
                .Select(s => s is JsonValue v && v.TryGetValue<string>(out var f) ? f : null)
                .OfType<string>()
                .ToList();
            yield return new Requirement($"{n}:{ProposalIds.Bare(value)}", r["texto"] is JsonValue t && t.TryGetValue<string>(out var text) ? text : "", sources);
        }
    }

    // The numbers of a text, with "," and "." as the same decimal mark: "24" and "1,5".
    public static HashSet<string> Numbers(string text) =>
        Number().Matches(text).Select(m => m.Value.Replace(',', '.')).ToHashSet();

    private static void Add(Dictionary<string, List<string>> pairs, string from, string to)
    {
        if (!pairs.TryGetValue(from, out var list))
            pairs[from] = list = [];
        list.Add(to);
    }

    // The id at the start of a line of the documentation, also of a "sigue" line.
    private static readonly Regex LineId = new(@"^\[([A-Z]+-\d+#F\d+)[ \]]");

    [GeneratedRegex(@"\d+(?:[.,]\d+)?")]
    private static partial Regex Number();
}
