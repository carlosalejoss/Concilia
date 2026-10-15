using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Concilia.Core.Phases;

// The "sigue" mark of the documentation file (InputFiles): a fragment longer
// than one line goes on in lines that start with "[DOC-01#F008 sigue]". The
// id is still "DOC-01#F008", but a model may copy the mark along with it. In
// a real catalog run (15-10-2026) SOL cited "DOC-17#F003 sigue" and the whole
// catalog went to a repair (about 190,000 tokens of input) for that alone.
// The mark says nothing the id does not, so the platform removes it from
// every cited fragment before checking and keeping the output, as it does
// with the prefixes of the proposals (ProposalIds).
public static partial class FragmentCitations
{
    // A whole JSON string that is a fragment id followed by the mark.
    [GeneratedRegex(@"^(DOC-[0-9]{2,}#F[0-9]{3,})\s+sigue$")]
    private static partial Regex WithMark();

    // The output with "DOC-17#F003 sigue" turned into "DOC-17#F003" wherever
    // it appears as a whole value. An output without the mark, or that is not
    // JSON (the validator reports it), is returned as it came.
    public static string Clean(string output)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(output);
        }
        catch (JsonException)
        {
            return output;
        }
        return root is not null && CleanIn(root) ? root.ToJsonString(Indented) : output;
    }

    // Replaces the marked ids under node; true if it changed any.
    private static bool CleanIn(JsonNode node)
    {
        var changed = false;
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj.ToList())
                {
                    if (Bare(value) is { } bare)
                    {
                        obj[key] = bare;
                        changed = true;
                    }
                    else if (value is not null)
                        changed |= CleanIn(value);
                }
                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    if (Bare(array[i]) is { } bare)
                    {
                        array[i] = bare;
                        changed = true;
                    }
                    else if (array[i] is { } item)
                        changed |= CleanIn(item);
                }
                break;
        }
        return changed;
    }

    // The id without the mark, or null if the value is not a marked id.
    private static string? Bare(JsonNode? value) =>
        value is JsonValue v && v.TryGetValue<string>(out var text) && WithMark().Match(text) is { Success: true } match
            ? match.Groups[1].Value
            : null;

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
