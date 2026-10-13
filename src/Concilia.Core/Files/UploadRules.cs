namespace Concilia.Core.Files;

// What an uploaded file must satisfy. The web (frontend/src/uploadRules.ts)
// repeats these values to warn before uploading; keep both in sync.
public static class UploadRules
{
    public const long MaxBytes = 10 * 1024 * 1024; // 10 MB

    public static readonly IReadOnlyList<string> AllowedExtensions = [".txt", ".md", ".pdf", ".docx"];

    // Returns an error message for the user, or null if the file is valid.
    public static string? Validate(string fileName, long sizeBytes)
    {
        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return $"Tipo de archivo no permitido. Se admiten: {string.Join(", ", AllowedExtensions)}.";
        }

        if (sizeBytes == 0)
        {
            return "El archivo está vacío.";
        }

        if (sizeBytes > MaxBytes)
        {
            return $"El archivo supera el tamaño máximo de {MaxBytes / (1024 * 1024)} MB.";
        }

        return null;
    }
}
