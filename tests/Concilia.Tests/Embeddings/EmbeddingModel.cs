namespace Concilia.Tests.Embeddings;

// Where the tests find the real embeddings model: the folder in the variable
// CONCILIA_EMBEDDINGS_MODEL, or models/paraphrase-multilingual-MiniLM-L12-v2
// in the repository, where scripts/descargar-modelo-embeddings.ps1 leaves it.
// It is not in git (470 MB) nor in GitHub Actions: the tests that need it are
// skipped when it is missing (decision of the author, 19-10-2026).
public static class EmbeddingModel
{
    public static string? Folder { get; } = Find();

    private static string? Find()
    {
        var fromVariable = Environment.GetEnvironmentVariable("CONCILIA_EMBEDDINGS_MODEL");
        if (!string.IsNullOrEmpty(fromVariable))
            return HasModel(fromVariable) ? fromVariable : null;

        // From the test DLL (tests/Concilia.Tests/bin/...) up to the repository root.
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var candidate = Path.Combine(folder.FullName, "models", "paraphrase-multilingual-MiniLM-L12-v2");
            if (HasModel(candidate)) return candidate;
        }
        return null;
    }

    private static bool HasModel(string folder) =>
        File.Exists(Path.Combine(folder, "model.onnx")) && File.Exists(Path.Combine(folder, "sentencepiece.bpe.model"));
}

// A [Fact] that is skipped, with the reason, when the model is not downloaded.
public sealed class ModelFactAttribute : FactAttribute
{
    public ModelFactAttribute()
    {
        if (EmbeddingModel.Folder is null)
            Skip = "Falta el modelo de embeddings (scripts/descargar-modelo-embeddings.ps1).";
    }
}
