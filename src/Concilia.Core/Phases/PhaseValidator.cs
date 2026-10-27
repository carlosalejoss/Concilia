using System.Text.Json;
using System.Text.Json.Nodes;
using LateApexEarlySpeed.Json.Schema;
using Concilia.Core.Catalog;
using static Concilia.Core.Catalog.CatalogJson;

namespace Concilia.Core.Phases;

// What the validator needs besides the output: the fragment ids of the
// documentation, the ids of the answered questions (catalog), the validated
// catalog (backlog) and, for the arbiter, the two proposals as they were given.
public record PhaseContext
{
    public IReadOnlySet<string> FragmentIds { get; init; } = new HashSet<string>();
    // The fragment of each line of input/documentacion.txt, to tell a model
    // that cites a line number as a fragment (DocumentationFile.FragmentAtLine).
    public IReadOnlyDictionary<int, string> FragmentAtLine { get; init; } = new Dictionary<int, string>();
    // The file those lines are of: input/fuentes.txt for the arbiter of the catalog (step 107).
    public string LinesFile { get; init; } = DocumentationFile.Path;
    public IReadOnlySet<string> AnsweredQuestionIds { get; init; } = new HashSet<string>();
    public IReadOnlyList<string> PendingQuestionIds { get; init; } = []; // suggestions, in order
    public string? Catalog { get; init; }
    public string? Proposal1 { get; init; }
    public string? Proposal2 { get; init; }
    // Arbiter: the matching of the platform (MatchingFile), for PlatformFixes.
    public string? Matching { get; init; }
    // The working copy of the catalog or the backlog, whose ids are stable
    // (step 110): deleting leaves gaps, so ID-SEQ does not apply. Ids are
    // still unique and, in the catalog, with the prefix of their type.
    public bool GapsInIds { get; init; }
    // A round centred on some iterations (step 112): the documents of the
    // focus (every new question and revision cites one) and the pending
    // questions, the only ones that can be revised.
    public IReadOnlySet<string> NewDocumentIds { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> RevisableQuestionIds { get; init; } = new HashSet<string>();
    // An update of the catalog (step 113): the answers given after validating it.
    public IReadOnlySet<string> NewAnswerIds { get; init; } = new HashSet<string>();
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
//   FALTA       a pending question without its suggested answer
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
            PhaseKind.Questions when phase.Focused => ValidateFocusedQuestions(phase, data, context, proposals),
            PhaseKind.Questions => ValidateQuestions(phase, data, context, proposals),
            PhaseKind.Catalog => ValidateCatalog(phase, data, context, proposals),
            PhaseKind.Backlog => ValidateBacklog(phase, data, context, proposals),
            PhaseKind.Suggestions => ValidateSuggestions(phase, data, context),
            PhaseKind.CatalogUpdate => ValidateCatalogUpdate(phase, data, context, proposals),
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
        errors.AddRange(IdErrors(questions.Select(q => Str(q, "id")!).ToList(), "P", 3, context.GapsInIds));
        foreach (var q in questions)
            errors.AddRange(FragmentErrors(Str(q, "id")!, StrList(q, "fuentes"), context));
        if (proposals is var (p1, p2))
            errors.AddRange(ArbiterErrors(questions, (Ids(Items(p1, "preguntas")), Ids(Items(p2, "preguntas"))), Items(data!, "descartadas"), "descartadas"));
        return new PhaseValidation(errors, []);
    }

    // Step 112 (prompts v0.13): a round centred on some iterations. What a
    // round of always checks, and also (new codes, not in validar_v0.py):
    //   SIN-NUEVA  a new question or a revision that cites nothing of the documentation of the focus
    //   REF-PREG   `contradice` with a question that is not answered, or a revision of one that is not pending
    //   REF-REQ    `contradice` with a requirement that is not in the validated catalog
    //   REV-DUP    two revisions of the same question
    //   REV-TEXTO  `texto` only in `reformular`, and always in it
    //   ARB-REV    the arbiter: one decision per revised question, `de` with the proposals whose
    //              revision it keeps (the same action), and none in `ninguna`
    private static PhaseValidation ValidateFocusedQuestions(PhaseDefinition phase, JsonNode? data, PhaseContext context, (JsonNode, JsonNode)? proposals)
    {
        var errors = SchemaErrorsOf(phase, data);
        if (errors.Count > 0)
            return new PhaseValidation(errors, []);
        var questions = Items(data!, "preguntas");
        errors.AddRange(IdErrors(questions.Select(q => Str(q, "id")!).ToList(), "P", 3, context.GapsInIds));
        var catalog = Items(JsonNode.Parse(context.Catalog ?? "{\"requisitos\":[]}")!, "requisitos").Select(r => Str(r, "id")).ToHashSet();
        foreach (var q in questions)
        {
            var id = Str(q, "id")!;
            var sources = StrList(q, "fuentes");
            errors.AddRange(FragmentErrors(id, sources, context));
            if (!CitesNew(sources, context))
                errors.Add($"{id} SIN-NUEVA: cita al menos un fragmento de la documentación nueva ({string.Join(", ", context.NewDocumentIds.Order())}).");
            if (q["contradice"] is JsonObject contradicts)
            {
                if (Str(contradicts, "pregunta") is { } answered && !context.AnsweredQuestionIds.Contains(answered))
                    errors.Add($"{id} REF-PREG: `contradice` cita {answered}, que no existe o no está respondida.");
                if (Str(contradicts, "requisito") is { } requirement && !catalog.Contains(requirement))
                    errors.Add($"{id} REF-REQ: `contradice` cita {requirement}, que no está en el catálogo validado.");
            }
        }

        var revisions = Items(data!, "revisiones");
        var revised = new HashSet<string>();
        foreach (var r in revisions)
        {
            var target = Str(r, "pregunta")!;
            var action = Str(r, "accion");
            if (!revised.Add(target))
                errors.Add($"{target} REV-DUP: hay más de una revisión de la pregunta.");
            if (proposals is null)
            {
                if (!context.RevisableQuestionIds.Contains(target))
                    errors.Add($"{target} REF-PREG: solo se revisan las preguntas pendientes, y {target} no lo es o no existe.");
                if (!CitesNew(StrList(r, "fuentes"), context))
                    errors.Add($"{target} SIN-NUEVA: la revisión cita al menos un fragmento de la documentación nueva.");
            }
            if ((action == "reformular") != (r["texto"] is not null))
                errors.Add($"{target} REV-TEXTO: `texto` va en `reformular`, con la pregunta entera, y en ninguna otra acción.");
            errors.AddRange(FragmentErrors(target, StrList(r, "fuentes"), context));
        }

        if (proposals is var (p1, p2))
        {
            errors.AddRange(ArbiterErrors(questions, (Ids(Items(p1, "preguntas")), Ids(Items(p2, "preguntas"))), Items(data!, "descartadas"), "descartadas"));
            var byProposal = new[] { Items(p1, "revisiones"), Items(p2, "revisiones") };
            var targets = byProposal.SelectMany(list => list).Select(r => Str(r, "pregunta")!).ToHashSet();
            foreach (var t in targets.Where(t => !revised.Contains(t)).Order(StringComparer.Ordinal))
                errors.Add($"{t} ARB-REV: falta la decisión sobre la revisión de {t} (una por cada pregunta de input/revisiones.txt; `ninguna` si se queda como está).");
            foreach (var r in revisions)
            {
                var target = Str(r, "pregunta")!;
                var action = Str(r, "accion");
                var de = r["de"]!.AsArray().Select(n => n!.GetValue<int>()).ToList();
                if (!targets.Contains(target))
                    errors.Add($"{target} ARB-REV: ninguna propuesta revisa {target}.");
                else if (action == "ninguna" && de.Count > 0)
                    errors.Add($"{target} ARB-REV: con `ninguna`, `de` va vacío.");
                else if (action != "ninguna" && de.Count == 0)
                    errors.Add($"{target} ARB-REV: `de` dice de qué propuestas es la revisión que mantienes.");
                foreach (var n in de.Where(n => byProposal[n - 1].All(x => Str(x, "pregunta") != target || Str(x, "accion") != action)))
                    errors.Add($"{target} ARB-REV: la propuesta {n} no propone `{action}` para {target}.");
            }
        }
        return new PhaseValidation(errors, []);
    }

    // Step 113 (prompts v0.13): an update of the validated catalog, a set of
    // changes. The rules of a requirement (TIPO, EARS, SIN-ORIGEN, REF-FRAG,
    // REF-PREG) for the new and modified ones, and (new codes, only in C#):
    //   REF-REQ     a modified or deleted requirement that is not in the validated catalog
    //   CAMBIO-DUP  two changes of the same requirement
    //   SIN-NUEVA   a change that cites neither the new documentation nor a new answer
    //   ARB-CAMBIO  the arbiter: one decision per changed requirement, `de` with the proposals
    //               that propose that same action (none in `ninguna`), the whole requirement only in `modificar`
    private static PhaseValidation ValidateCatalogUpdate(PhaseDefinition phase, JsonNode? data, PhaseContext context, (JsonNode, JsonNode)? proposals)
    {
        var errors = SchemaErrorsOf(phase, data);
        if (errors.Count > 0)
            return new PhaseValidation(errors, []);
        var catalog = Items(JsonNode.Parse(context.Catalog ?? "{\"requisitos\":[]}")!, "requisitos").Select(r => Str(r, "id")).ToHashSet();
        var nuevos = Items(data!, "nuevos");
        errors.AddRange(IdErrors(Ids(nuevos), "N", 3, gaps: false));

        void Requirement(JsonObject r, string id)
        {
            var type = Str(r, "tipo")!;
            if (!id.StartsWith("N-", StringComparison.Ordinal) && id.Split('-')[0] != type)
                errors.Add($"{id} TIPO: un requisito modificado mantiene su tipo; si cambia, elimínalo y propón uno nuevo.");
            if (Str(r, "notacion") == "EARS" && Str(r, "obligatoriedad") != "shall")
                errors.Add($"{id} EARS: un requisito EARS siempre es shall; para {Str(r, "obligatoriedad")} usa MASTeR.");
            if (StrList(r, "fuentes").Count == 0 && StrList(r, "preguntas").Count == 0)
                errors.Add($"{id} SIN-ORIGEN: cita al menos un fragmento o una pregunta respondida.");
        }
        void Origin(JsonObject r, string id, bool mustBeNew)
        {
            var sources = StrList(r, "fuentes");
            var questions = StrList(r, "preguntas");
            errors.AddRange(FragmentErrors(id, sources, context));
            foreach (var p in questions.Where(p => !context.AnsweredQuestionIds.Contains(p)))
                errors.Add($"{id} REF-PREG: la pregunta {p} no existe o no está respondida.");
            if (mustBeNew && !CitesNew(sources, context) && !questions.Any(context.NewAnswerIds.Contains))
                errors.Add($"{id} SIN-NUEVA: cita al menos un fragmento de la documentación nueva o una respuesta nueva.");
        }

        if (proposals is null)
        {
            foreach (var r in nuevos)
            {
                Requirement(r, Str(r, "id")!);
                Origin(r, Str(r, "id")!, mustBeNew: true);
            }
            var changed = new HashSet<string>();
            foreach (var (list, isModified) in new[] { ("modificados", true), ("eliminados", false) })
                foreach (var r in Items(data!, list))
                {
                    var id = Str(r, "id")!;
                    if (!catalog.Contains(id))
                        errors.Add($"{id} REF-REQ: {id} no está en el catálogo validado.");
                    if (!changed.Add(id))
                        errors.Add($"{id} CAMBIO-DUP: hay más de un cambio del mismo requisito.");
                    if (isModified)
                        Requirement(r, id);
                    Origin(r, id, mustBeNew: true);
                }
            return new PhaseValidation(errors, []);
        }

        var (p1, p2) = proposals.Value;
        foreach (var r in nuevos)
        {
            Requirement(r, Str(r, "id")!);
            Origin(r, Str(r, "id")!, mustBeNew: false);
        }
        errors.AddRange(ArbiterErrors(nuevos, (Ids(Items(p1, "nuevos")), Ids(Items(p2, "nuevos"))), Items(data!, "descartados"), "descartados"));
        var byProposal = new[] { CatalogUpdateRound.ChangesOf(p1.ToJsonString()), CatalogUpdateRound.ChangesOf(p2.ToJsonString()) };
        var targets = byProposal.SelectMany(list => list).Select(c => c.Id).ToHashSet();
        var decided = new HashSet<string>();
        foreach (var c in Items(data!, "cambios"))
        {
            var id = Str(c, "id")!;
            var action = Str(c, "accion");
            var de = c["de"]!.AsArray().Select(n => n!.GetValue<int>()).ToList();
            if (!decided.Add(id))
                errors.Add($"{id} ARB-CAMBIO: hay más de una decisión sobre {id}.");
            if (!targets.Contains(id))
                errors.Add($"{id} ARB-CAMBIO: ninguna propuesta cambia {id}.");
            else if (action == "ninguna" && de.Count > 0)
                errors.Add($"{id} ARB-CAMBIO: con `ninguna`, `de` va vacío.");
            else if (action != "ninguna" && de.Count == 0)
                errors.Add($"{id} ARB-CAMBIO: `de` dice de qué propuestas es el cambio que mantienes.");
            foreach (var n in de.Where(n => !byProposal[n - 1].Any(x => x.Id == id && x.Action == action)))
                errors.Add($"{id} ARB-CAMBIO: la propuesta {n} no propone `{action}` para {id}.");
            var whole = c["texto"] is not null && c["tipo"] is not null && c["notacion"] is not null && c["obligatoriedad"] is not null;
            if (action == "modificar")
            {
                if (!whole)
                    errors.Add($"{id} ARB-CAMBIO: en `modificar` va el requisito entero (`tipo`, `notacion`, `obligatoriedad` y `texto`).");
                else
                    Requirement(c, id);
                Origin(c, id, mustBeNew: false);
            }
            else if (c["texto"] is not null)
                errors.Add($"{id} ARB-CAMBIO: en `{action}` no va el texto del requisito.");
        }
        foreach (var t in targets.Where(t => !decided.Contains(t)).Order(StringComparer.Ordinal))
            errors.Add($"{t} ARB-CAMBIO: falta la decisión sobre {t} (una por cada requisito de input/cambios.txt; `ninguna` si se queda como está).");
        return new PhaseValidation(errors, []);
    }

    // At least one fragment of the documents of the focus.
    private static bool CitesNew(List<string> sources, PhaseContext context) =>
        sources.Any(f => context.NewDocumentIds.Contains(f.Split('#')[0]));

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
            errors.AddRange(FragmentErrors(id, sources, context));
            foreach (var p in questions.Where(p => !context.AnsweredQuestionIds.Contains(p)))
                errors.Add($"{id} REF-PREG: la pregunta {p} no existe o no está respondida.");
        }
        foreach (var type in Types)
            errors.AddRange(IdErrors(requirements.Where(r => Str(r, "tipo") == type).Select(r => Str(r, "id")!).ToList(), type, 3, context.GapsInIds));
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
        errors.AddRange(IdErrors(Ids(Items(data!, "epicas")), "EP", 2, context.GapsInIds));
        errors.AddRange(IdErrors(Ids(stories), "HU", 3, context.GapsInIds));
        errors.AddRange(IdErrors(stories.SelectMany(h => Items(h, "criterios")).Select(c => Str(c, "id")!).ToList(), "CA", 3, context.GapsInIds));
        errors.AddRange(IdErrors(stories.SelectMany(h => Items(h, "tareas")).Select(t => Str(t, "id")!).ToList(), "T", 3, context.GapsInIds));

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

    // Suggested answers: one for each pending question (in order), without
    // repeating or suggesting for other questions, with fragments that exist;
    // one based on the documentation cites at least one fragment.
    private static PhaseValidation ValidateSuggestions(PhaseDefinition phase, JsonNode? data, PhaseContext context)
    {
        var errors = SchemaErrorsOf(phase, data);
        if (errors.Count > 0)
            return new PhaseValidation(errors, []);
        var seen = new HashSet<string>();
        foreach (var s in Items(data!, "sugerencias"))
        {
            var p = Str(s, "pregunta")!;
            if (!context.PendingQuestionIds.Contains(p))
                errors.Add($"{p} REF-PREG: la pregunta {p} no está entre las pendientes.");
            if (!seen.Add(p))
                errors.Add($"{p} ID-DUP: hay más de una sugerencia para la pregunta.");
            var sources = StrList(s, "fuentes");
            if (Str(s, "base") == "documentacion" && sources.Count == 0)
                errors.Add($"{p} SIN-ORIGEN: una sugerencia basada en la documentación cita al menos un fragmento.");
            errors.AddRange(FragmentErrors(p, sources, context));
        }
        errors.AddRange(context.PendingQuestionIds.Where(p => !seen.Contains(p)).Select(p => $"{p} FALTA: no hay sugerencia para la pregunta."));
        return new PhaseValidation(errors, []);
    }

    // --- Shared checks -----------------------------------------------------

    // Unique ids, consecutive from 1: P-001, P-002… (digits = width of the number).
    // gaps: the working copy, with stable ids (step 110): only unique.
    private static List<string> IdErrors(List<string> ids, string prefix, int digits, bool gaps)
    {
        var errors = new List<string>();
        var seen = new HashSet<string>();
        foreach (var id in ids)
        {
            if (!seen.Add(id))
                errors.Add($"{id} ID-DUP: id repetido.");
        }
        var expected = Enumerable.Range(1, ids.Count).Select(n => $"{prefix}-{n.ToString().PadLeft(digits, '0')}").ToList();
        if (!gaps && !ids.SequenceEqual(expected) && seen.Count == ids.Count)
            errors.Add($"{prefix} ID-SEQ: los ids deben ir seguidos desde {(expected.Count > 0 ? expected[0] : prefix)}; llegan {string.Join(", ", ids)}.");
        return errors;
    }

    private static IEnumerable<string> FragmentErrors(string element, List<string> sources, PhaseContext context) =>
        sources.Where(f => !context.FragmentIds.Contains(f)).Select(f => $"{element} REF-FRAG: el fragmento {f} no existe.{LineHint(f, context)}");

    // LUNA cited line numbers as fragments (DOC-06#F830 for the line 830 of
    // input/documentacion.txt, where DOC-06#F537 is; 14-10-2026, three times
    // in a row, also in its repair). When the number of a fragment that does
    // not exist is a line of the file, the error says which fragment is there.
    private static string LineHint(string fragment, PhaseContext context)
    {
        var hash = fragment.IndexOf("#F", StringComparison.Ordinal);
        if (hash < 0 || !int.TryParse(fragment[(hash + 2)..], out var line) || !context.FragmentAtLine.TryGetValue(line, out var atLine))
            return "";
        return $" La línea {line} de {context.LinesFile} es {atLine}: el id de un fragmento es el que va entre corchetes al principio de la línea, no el número de línea que muestra Read.";
    }

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
