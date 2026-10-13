namespace Concilia.Core.Projects;

// Rules for the catalog settings of a project.
public static class ProjectSettings
{
    public const int MaxNameLength = 200;
    public const int MaxSystemNameLength = 200;
    public const int MaxGlossaryLength = 20_000;

    public static readonly IReadOnlyList<string> Languages = ["es", "en"];

    // Default system name for a new project: "el sistema <name>" / "the <name> system".
    public static string DefaultSystemName(string projectName, string language = "es") =>
        language == "en" ? $"the {projectName} system" : $"el sistema {projectName}";

    // Deleting a project asks the user to type its name (13-10-2026): the
    // deletion cannot be undone, so it must not happen with one click. The
    // name is compared as it is, after trimming the spaces around it.
    public static bool ConfirmsDeletion(string projectName, string? typed) =>
        typed is not null && typed.Trim() == projectName;

    // Error for the project name, or null if it is valid (expected already trimmed).
    public static string? ValidateName(string? name) =>
        string.IsNullOrEmpty(name) ? "El nombre es obligatorio."
        : name.Length > MaxNameLength ? $"El nombre no puede superar {MaxNameLength} caracteres."
        : null;

    // Returns the errors by field (empty if everything is valid). The values
    // are expected already trimmed.
    public static Dictionary<string, string[]> Validate(string? name, string? systemName, string? language, string? glossary)
    {
        var errors = new Dictionary<string, string[]>();

        if (ValidateName(name) is { } nameError)
            errors["name"] = [nameError];

        if (string.IsNullOrEmpty(systemName))
            errors["systemName"] = ["El nombre del sistema es obligatorio (con artículo: \"el sistema X\")."];
        else if (systemName.Length > MaxSystemNameLength)
            errors["systemName"] = [$"El nombre del sistema no puede superar {MaxSystemNameLength} caracteres."];

        if (language is null || !Languages.Contains(language))
            errors["language"] = ["El idioma debe ser \"es\" o \"en\"."];

        if (glossary is { Length: > MaxGlossaryLength })
            errors["glossary"] = [$"El glosario no puede superar {MaxGlossaryLength} caracteres."];

        return errors;
    }
}
