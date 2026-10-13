namespace Concilia.Core.Projects;

// A workspace that groups the uploaded files, the model runs and their results.
public class Project
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Inputs of the requirements prompt (§1). SystemName is copied literally
    // into every requirement, so it includes the article: "el sistema ReservaSalas".
    public required string SystemName { get; set; }

    // Language of the catalog: "es" or "en".
    public string Language { get; set; } = "es";

    // Optional glossary of the domain, passed to the models as it is.
    public string? Glossary { get; set; }
}
