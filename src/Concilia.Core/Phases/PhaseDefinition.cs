namespace Concilia.Core.Phases;

// The phases of the v0 flow that run A ‖ B → arbiter (the mockup has its own
// task), and Suggestions: the answers that one model suggests for the pending
// questions of a round (a single run, without arbiter).
public enum PhaseKind { Questions, Catalog, Backlog, Suggestions }

// Model A and model B write a proposal; the arbiter joins the two.
public enum PhaseRole { Proposal, Arbiter }

// What one phase and role use: its prompt and schema (embedded from prompts/v0/,
// see prompts/v0/README.md) and the file the model must write.
public record PhaseDefinition(PhaseKind Kind, PhaseRole Role, string PromptResource, string SchemaResource, string OutputFile)
{
    public bool IsArbiter => Role == PhaseRole.Arbiter;

    public static PhaseDefinition For(PhaseKind kind, PhaseRole role)
    {
        if (kind == PhaseKind.Suggestions)
            return new PhaseDefinition(kind, PhaseRole.Proposal, "v0/sugerir_respuestas.md", "v0/esquemas/sugerencias.schema.json", "output/sugerencias.json");
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
