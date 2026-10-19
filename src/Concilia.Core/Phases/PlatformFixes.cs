using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Concilia.Core.Deliverables;
using Concilia.Core.Reports;

namespace Concilia.Core.Phases;

// Step 89 (decisions of the author, 19-10-2026): mistakes of format with only
// one obvious fix, which the platform makes by itself instead of asking for a
// repair. They were LUNA's most common failures, and the repair of a big
// catalog is expensive or does not fit in the context. Each fix leaves a
// warning, so the report and the evaluation know it happened. Applied before
// checking the output and to what is kept, as the joined keys and the "sigue"
// mark (PhaseTask.Clean).
//
//   REF-LINEA  a cited fragment that does not exist, whose number is a line of
//              input/documentacion.txt with a fragment of the same document:
//              the model cited the line Read showed (DOC-06#F830 for the line
//              830, where DOC-06#F537 is). It becomes that fragment.
//   TIPO       a requirement whose id does not match its type (RF-066 of type
//              RNF): the type is taken as right and the catalog is renumbered
//              by type, as the draft does (nothing cites the own ids).
//   ARB-FALTA  an element of a proposal that the arbiter left out of every
//              `de` and of the discards: if its pair "mismo" of the matching
//              (input/emparejamiento.txt) is in one element of the arbiter, it
//              is added to that `de`. Otherwise it is still an error.
public static partial class PlatformFixes
{
    public record Fixed(string Output, IReadOnlyList<string> Warnings);

    // A whole value that looks like a fragment id: DOC-06#F830.
    [GeneratedRegex(@"^(DOC-[0-9]{2,})#F([0-9]{3,})$")]
    private static partial Regex FragmentId();

    // A line of the matching file: "1:RF-003 2:RF-001 mismo 0.94".
    [GeneratedRegex(@"^1:(\S+) 2:(\S+) mismo ")]
    private static partial Regex SamePair();

    public static Fixed Apply(PhaseDefinition phase, string output, PhaseContext context)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(output);
        }
        catch (JsonException)
        {
            return new Fixed(output, []); // not JSON: the validator reports it
        }
        if (root is null)
            return new Fixed(output, []);

        var warnings = new List<string>();
        FixLineCitations(root, context, phase.OutputFile, warnings);
        if (phase.IsArbiter)
            FixMissing(phase.Kind, root, context, warnings);
        var json = root.ToJsonString(Indented);
        if (phase.Kind == PhaseKind.Catalog)
            json = FixTypes(json, warnings);
        return warnings.Count == 0 ? new Fixed(output, []) : new Fixed(json, warnings);
    }

    // REF-LINEA: every value that is a fragment id is checked, wherever it is.
    private static void FixLineCitations(JsonNode node, PhaseContext context, string file, List<string> warnings)
    {
        string? Fix(JsonNode? value)
        {
            if (value is not JsonValue v || !v.TryGetValue<string>(out var id) || context.FragmentIds.Contains(id))
                return null;
            var match = FragmentId().Match(id);
            if (!match.Success || !int.TryParse(match.Groups[2].Value, out var line)
                || !context.FragmentAtLine.TryGetValue(line, out var atLine)
                || !atLine.StartsWith(match.Groups[1].Value + "#", StringComparison.Ordinal))
                return null;
            warnings.Add($"{file} REF-LINEA: {id} no existe y es el número de una línea; la plataforma lo cambia por {atLine}, el fragmento de esa línea.");
            return atLine;
        }

        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj.ToList())
                {
                    if (Fix(value) is { } fixedId) obj[key] = fixedId;
                    else if (value is not null) FixLineCitations(value, context, file, warnings);
                }
                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    if (Fix(array[i]) is { } fixedId) array[i] = fixedId;
                    else if (array[i] is { } child) FixLineCitations(child, context, file, warnings);
                }
                break;
        }
    }

    // TIPO: only if some id does not match its type; then every requirement
    // is renumbered (RF-001, RF-002…, RNF-001…), so the ids stay consecutive.
    private static string FixTypes(string json, List<string> warnings)
    {
        var requirements = JsonNode.Parse(json)!["requisitos"] as JsonArray ?? [];
        var wrong = requirements.OfType<JsonObject>()
            .Where(r => r["id"] is JsonValue i && i.TryGetValue<string>(out var id) && r["tipo"] is JsonValue t && t.TryGetValue<string>(out var type)
                        && CatalogDraft.Types.Contains(type) && id.Split('-')[0] != type)
            .Select(r => $"{r["id"]} ({r["tipo"]})").ToList();
        if (wrong.Count == 0)
            return json;
        warnings.Add($"requisitos TIPO: el prefijo del id no coincidía con el tipo en {string.Join(", ", wrong)}; la plataforma renumera el catálogo por tipo.");
        return CatalogDraft.Normalize(json);
    }

    // ARB-FALTA: the elements of each proposal that are in no `de` and in no
    // discard, and the element of the arbiter where their pair "mismo" is.
    private static void FixMissing(PhaseKind kind, JsonNode root, PhaseContext context, List<string> warnings)
    {
        if (string.IsNullOrEmpty(context.Matching) || context.Proposal1 is null || context.Proposal2 is null)
            return;
        var elements = JobReports.Elements(kind, root);
        var discardsField = kind == PhaseKind.Catalog ? "descartados" : "descartadas";

        // Which arbiter element cites each id of each proposal ("1:" and "2:" may be there or not).
        var citedIn = new Dictionary<(int, string), JsonObject>();
        foreach (var element in elements)
            if (element["de"] is JsonObject de)
                for (var n = 1; n <= 2; n++)
                    foreach (var id in Ids(de[$"propuesta_{n}"]))
                        citedIn.TryAdd((n, ProposalIds.Bare(id)), element);
        var discarded = (root[discardsField] as JsonArray ?? []).OfType<JsonObject>()
            .Select(d => (d["propuesta"] is JsonValue p && p.TryGetValue<int>(out var n) ? n : 0, ProposalIds.Bare(d["id"]?.GetValue<string>() ?? "")))
            .ToHashSet();

        var pairs = context.Matching.Split('\n').Select(l => SamePair().Match(l)).Where(m => m.Success)
            .Select(m => (One: m.Groups[1].Value, Two: m.Groups[2].Value)).ToList();
        var proposals = new Dictionary<int, string> { [1] = context.Proposal1, [2] = context.Proposal2 };
        for (var n = 1; n <= 2; n++)
        {
            foreach (var id in JobReports.Elements(kind, JsonNode.Parse(proposals[n])).Select(e => e["id"]!.GetValue<string>()))
            {
                if (citedIn.ContainsKey((n, id)) || discarded.Contains((n, id)))
                    continue;
                // Its pairs "mismo" in the other proposal, and the arbiter elements they ended in.
                var other = 3 - n;
                var targets = pairs.Where(p => (n == 1 ? p.One : p.Two) == id)
                    .Select(p => citedIn.GetValueOrDefault((other, n == 1 ? p.Two : p.One)))
                    .OfType<JsonObject>().Distinct().ToList();
                if (targets.Count != 1)
                    continue; // no pair, or pairs in several elements: the arbiter has to decide
                var target = targets[0];
                if (target["de"]![$"propuesta_{n}"] is not JsonArray list)
                    target["de"]![$"propuesta_{n}"] = list = [];
                list.Add($"{n}:{id}");
                citedIn[(n, id)] = target;
                warnings.Add($"{n}:{id} ARB-FALTA: no estaba en ningún `de` ni en `{discardsField}`; la plataforma lo añade a {target["id"]}, " +
                             "donde está su par «mismo» del emparejamiento.");
            }
        }
    }

    private static IEnumerable<string> Ids(JsonNode? list) =>
        (list as JsonArray ?? []).OfType<JsonValue>().Select(v => v.TryGetValue<string>(out var s) ? s : null).OfType<string>();

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
