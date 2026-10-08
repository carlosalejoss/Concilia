using System.Text.Json;
using System.Text.Json.Nodes;
using LateApexEarlySpeed.Json.Schema;
using ReqArbiter.Core.Catalog;
using static ReqArbiter.Core.Catalog.CatalogJson;

namespace ReqArbiter.Core.Phases;

// What the validator needs besides the output: the fragment ids of the
// documentation, the ids of the answered questions (catalog), the validated
// catalog (backlog) and, for the arbiter, the two proposals as they were given.
public record PhaseContext
{
    public IReadOnlySet<string> FragmentIds { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> AnsweredQuestionIds { get; init; } = new HashSet<string>();
    public string? Catalog { get; init; }
    public string? Proposal1 { get; init; }
    public string? Proposal2 { get; init; }
}

// Errors cause the repair (each line goes to the annex); warnings are shown
// but do not repair. Each line starts with the element and a code.
public record PhaseValidation(IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings)
{
    public bool Valid => Errors.Count == 0;
}

// Port of prompts/v0/validar_v0.py, the reference: for the same output it gives
// the same lines in the same order, except the SCHEMA ones (the messages of
// the two JSON Schema libraries differ). The parity is checked with the cases
// that prompts/v0/test_v0.py exports to tests/fixtures/v0-mutations. Codes:
//   SCHEMA      the JSON does not follow its schema (prompts/v0/esquemas/)
//   ID-DUP      repeated id
//   ID-SEQ      ids not consecutive (P-001, P-002…; by type in the catalog)
//   TIPO        the prefix of the id does not match the type
//   EARS        an EARS requirement with should or may
//   SIN-ORIGEN  requirement without fragments or questions
//   REF-FRAG    fragment that does not exist in the documentation
//   REF-PREG    question that does not exist or is not answered
//   REF-REQ     requirement that does not exist in the catalog
//   ARB-DE      the arbiter cites an id that is not in that proposal, or a `de` without ids
//   ARB-FALTA   an element of a proposal is neither in a `de` nor in the discards
//   COB         (warning) RF or RI requirement without any story
public static class PhaseValidator
{
    private static readonly string[] Types = ["RF", "RNF", "RA", "RT", "RI"];

    private static readonly Dictionary<string, JsonValidator> Schemas = new();

    // Validates the file the model wrote (it may not be JSON).
    public static PhaseValidation Validate(PhaseDefinition phase, string output, PhaseContext context)
    {
        JsonNode? data;
        try
        {
            data = JsonNode.Parse(output);
        }
        catch (JsonException e)
        {
            return new PhaseValidation([$"JSON no válido: {e.Message}"], []);
        }
        var proposals = phase.IsArbiter
            ? (JsonNode.Parse(context.Proposal1 ?? "{}")!, JsonNode.Parse(context.Proposal2 ?? "{}")!)
            : ((JsonNode, JsonNode)?)null;
        return phase.Kind switch
        {
            PhaseKind.Questions => ValidateQuestions(phase, data, context, proposals),
            PhaseKind.Catalog => ValidateCatalog(phase, data, context, proposals),
            PhaseKind.Backlog => ValidateBacklog(phase, data, context, proposals),
            _ => throw new ArgumentOutOfRangeException(nameof(phase)),
        };
    }

    // --- Phases ------------------------------------------------------------

    private static PhaseValidation ValidateQuestions(PhaseDefinition phase, JsonNode? data, PhaseContext context, (JsonNode, JsonNode)? proposals)
    {
        var errors = SchemaErrorsOf(phase, data);
        if (errors.Count > 0)
            return new PhaseValidation(errors, []);
        var questions = Items(data!, "preguntas");
        errors.AddRange(IdErrors(questions.Select(q => Str(q, "id")!).ToList(), "P", 3));
        foreach (var q in questions)
            errors.AddRange(FragmentErrors(Str(q, "id")!, StrList(q, "fuentes"), context.FragmentIds));
        if (proposals is var (p1, p2))
            errors.AddRange(ArbiterErrors(questions, (Ids(Items(p1, "preguntas")), Ids(Items(p2, "preguntas"))), Items(data!, "descartadas"), "descartadas"));
        return new PhaseValidation(errors, []);
    }

    private static PhaseValidation ValidateCatalog(PhaseDefinition phase, JsonNode? data, PhaseContext context, (JsonNode, JsonNode)? proposals)
    {
        var errors = SchemaErrorsOf(phase, data);
        if (errors.Count > 0)
            return new PhaseValidation(errors, []);
        var requirements = Items(data!, "requisitos");
        foreach (var r in requirements)
        {
            var id = Str(r, "id")!;
            var type = Str(r, "tipo")!;
            if (id.Split('-')[0] != type)
                errors.Add($"{id} TIPO: el prefijo no coincide con el tipo {type}.");
            if (Str(r, "notacion") == "EARS" && Str(r, "obligatoriedad") != "shall")
                errors.Add($"{id} EARS: un requisito EARS siempre es shall; para {Str(r, "obligatoriedad")} usa MASTeR.");
            var sources = StrList(r, "fuentes");
            var questions = StrList(r, "preguntas");
            if (sources.Count == 0 && questions.Count == 0)
                errors.Add($"{id} SIN-ORIGEN: cita al menos un fragmento o una pregunta respondida.");
            errors.AddRange(FragmentErrors(id, sources, context.FragmentIds));
            foreach (var p in questions.Where(p => !context.AnsweredQuestionIds.Contains(p)))
                errors.Add($"{id} REF-PREG: la pregunta {p} no existe o no está respondida.");
        }
        foreach (var type in Types)
            errors.AddRange(IdErrors(requirements.Where(r => Str(r, "tipo") == type).Select(r => Str(r, "id")!).ToList(), type, 3));
        if (proposals is var (p1, p2))
            errors.AddRange(ArbiterErrors(requirements, (Ids(Items(p1, "requisitos")), Ids(Items(p2, "requisitos"))), Items(data!, "descartados"), "descartados"));
        return new PhaseValidation(errors, []);
    }

    private static PhaseValidation ValidateBacklog(PhaseDefinition phase, JsonNode? data, PhaseContext context, (JsonNode, JsonNode)? proposals)
    {
        var errors = SchemaErrorsOf(phase, data);
        if (errors.Count > 0)
            return new PhaseValidation(errors, []);
        var stories = StoriesOf(data!);
        errors.AddRange(IdErrors(Ids(Items(data!, "epicas")), "EP", 2));
        errors.AddRange(IdErrors(Ids(stories), "HU", 3));
        errors.AddRange(IdErrors(stories.SelectMany(h => Items(h, "criterios")).Select(c => Str(c, "id")!).ToList(), "CA", 3));
        errors.AddRange(IdErrors(stories.SelectMany(h => Items(h, "tareas")).Select(t => Str(t, "id")!).ToList(), "T", 3));

        var catalog = Items(JsonNode.Parse(context.Catalog ?? "{\"requisitos\":[]}")!, "requisitos");
        var existing = catalog.Select(r => Str(r, "id")!).ToHashSet();
        var covered = new HashSet<string>();
        foreach (var h in stories)
        {
            foreach (var r in StrList(h, "requisitos"))
            {
                if (!existing.Contains(r))
                    errors.Add($"{Str(h, "id")} REF-REQ: el requisito {r} no existe en el catálogo.");
                covered.Add(r);
            }
        }
        var warnings = catalog
            .Where(r => Str(r, "tipo") is "RF" or "RI" && !covered.Contains(Str(r, "id")!))
            .Select(r => $"{Str(r, "id")} COB: el requisito no está en ninguna historia.")
            .ToList();
        // The arbiter compares stories: they are passed all together, without their epics.
        if (proposals is var (p1, p2))
            errors.AddRange(ArbiterErrors(stories, (Ids(StoriesOf(p1)), Ids(StoriesOf(p2))), Items(data!, "descartadas"), "descartadas"));
        return new PhaseValidation(errors, warnings);
    }

    // --- Shared checks -----------------------------------------------------

    // Unique ids, consecutive from 1: P-001, P-002… (digits = width of the number).
    private static List<string> IdErrors(List<string> ids, string prefix, int digits)
    {
        var errors = new List<string>();
        var seen = new HashSet<string>();
        foreach (var id in ids)
        {
            if (!seen.Add(id))
                errors.Add($"{id} ID-DUP: id repetido.");
        }
        var expected = Enumerable.Range(1, ids.Count).Select(n => $"{prefix}-{n.ToString().PadLeft(digits, '0')}").ToList();
        if (!ids.SequenceEqual(expected) && seen.Count == ids.Count)
            errors.Add($"{prefix} ID-SEQ: los ids deben ir seguidos desde {(expected.Count > 0 ? expected[0] : prefix)}; llegan {string.Join(", ", ids)}.");
        return errors;
    }

    private static IEnumerable<string> FragmentErrors(string element, List<string> sources, IReadOnlySet<string> fragments) =>
        sources.Where(f => !fragments.Contains(f)).Select(f => $"{element} REF-FRAG: el fragmento {f} no existe.");

    // Every element of each proposal ends in some `de` or in the discards, and
    // every id cited exists in its proposal.
    private static List<string> ArbiterErrors(
        List<JsonObject> elements, (List<string> P1, List<string> P2) proposals, List<JsonObject> discards, string discardsField)
    {
        var errors = new List<string>();
        var ids = new Dictionary<int, List<string>> { [1] = proposals.P1, [2] = proposals.P2 };
        var used = new Dictionary<int, HashSet<string>> { [1] = [], [2] = [] };
        foreach (var element in elements)
        {
            var de = element["de"]!.AsObject();
            if (StrList(de, "propuesta_1").Count == 0 && StrList(de, "propuesta_2").Count == 0)
                errors.Add($"{Str(element, "id")} ARB-DE: `de` no tiene ningún id; di de qué elementos de las propuestas sale.");
            foreach (var n in new[] { 1, 2 })
            {
                foreach (var i in StrList(de, $"propuesta_{n}"))
                {
                    if (!ids[n].Contains(i))
                        errors.Add($"{Str(element, "id")} ARB-DE: {i} no existe en la propuesta {n}.");
                    used[n].Add(i);
                }
            }
        }
        foreach (var d in discards)
        {
            var n = d["propuesta"]!.GetValue<int>();
            var id = Str(d, "id")!;
            if (!ids[n].Contains(id))
                errors.Add($"{discardsField.ToUpperInvariant()} ARB-DE: {id} no existe en la propuesta {n}.");
            used[n].Add(id);
        }
        foreach (var n in new[] { 1, 2 })
        {
            foreach (var i in ids[n].Where(i => !used[n].Contains(i)))
                errors.Add($"{i} ARB-FALTA: el elemento {i} de la propuesta {n} no está en ningún `de` ni en `{discardsField}`.");
        }
        return errors;
    }

    // --- Helpers -----------------------------------------------------------

    private static List<string> SchemaErrorsOf(PhaseDefinition phase, JsonNode? data)
    {
        JsonValidator validator;
        lock (Schemas)
        {
            if (!Schemas.TryGetValue(phase.SchemaResource, out validator!))
            {
                validator = new JsonValidator(PromptBuilder.ReadResource(phase.SchemaResource));
                Schemas[phase.SchemaResource] = validator;
            }
        }
        return SchemaErrors(validator, data);
    }

    private static List<JsonObject> Items(JsonNode node, string list) =>
        node[list] is JsonArray array ? array.Select(x => x!.AsObject()).ToList() : [];

    private static List<string> Ids(List<JsonObject> items) => items.Select(x => Str(x, "id")!).ToList();

    private static List<JsonObject> StoriesOf(JsonNode backlog) =>
        Items(backlog, "epicas").SelectMany(e => Items(e, "historias")).ToList();
}
