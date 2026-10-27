namespace Concilia.Core.Phases;

// The phases of the v0 flow that run A ‖ B → arbiter (the mockup has its own
// task), and Suggestions: the answers that one model suggests for the pending
// questions of a round (a single run, without arbiter). CatalogUpdate (step
// 113): the changes that some new iterations ask in the validated catalog;
// BacklogUpdate (step 114): those that the updated catalog asks in the backlog.
public enum PhaseKind { Questions, Catalog, Backlog, Suggestions, CatalogUpdate, BacklogUpdate }

// Model A and model B write a proposal; the arbiter joins the two.
public enum PhaseRole { Proposal, Arbiter }

// What one phase and role use: its prompt and schema (embedded from prompts/v0/,
// see prompts/v0/README.md) and the file the model must write.
public record PhaseDefinition(PhaseKind Kind, PhaseRole Role, string PromptResource, string SchemaResource, string OutputFile)
{
    public bool IsArbiter => Role == PhaseRole.Arbiter;

    // A round of questions centred on some iterations (step 112, prompts v0.13).
    public bool Focused { get; init; }

    // focused: only for the questions; the prompts and schemas of the centred
    // round, with the same output file. The rounds of always do not change.
    public static PhaseDefinition For(PhaseKind kind, PhaseRole role, bool focused = false)
    {
        if (focused && kind == PhaseKind.Questions)
        {
            var name = role == PhaseRole.Arbiter ? "arbitro_preguntas_centradas" : "preguntas_centradas";
            return new PhaseDefinition(kind, role, $"v0/{name}.md", $"v0/esquemas/{name}.schema.json", "output/preguntas.json") { Focused = true };
        }
        return Standard(kind, role);
    }

    private static PhaseDefinition Standard(PhaseKind kind, PhaseRole role)
    {
        if (kind == PhaseKind.Suggestions)
            return new PhaseDefinition(kind, PhaseRole.Proposal, "v0/sugerir_respuestas.md", "v0/esquemas/sugerencias.schema.json", "output/sugerencias.json");
        // Step 113 (prompts v0.13): a set of changes, not a whole catalog, so its own output file.
        if (kind is PhaseKind.CatalogUpdate or PhaseKind.BacklogUpdate)
        {
            var updated = kind == PhaseKind.CatalogUpdate ? "catalogo_actualizacion" : "backlog_actualizacion";
            var update = role == PhaseRole.Arbiter ? $"arbitro_{updated}" : updated;
            return new PhaseDefinition(kind, role, $"v0/{update}.md", $"v0/esquemas/{update}.schema.json", "output/cambios.json");
        }
        var name = kind switch
        {
            PhaseKind.Questions => "preguntas",
            PhaseKind.Catalog => "catalogo",
            PhaseKind.Backlog => "backlog",
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        var file = role == PhaseRole.Arbiter ? $"arbitro_{name}" : name;
        return new PhaseDefinition(kind, role, $"v0/{file}.md", $"v0/esquemas/{file}.schema.json", $"output/{name}.json");
    }
}
