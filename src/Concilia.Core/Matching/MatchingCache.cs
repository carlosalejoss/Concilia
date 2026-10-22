using System.Text.Encodings.Web;
using System.Text.Json;

namespace Concilia.Core.Matching;

// data/jobs/{id}/emparejamiento.json (step 100): the slow part of the matching
// of A and B of a job (ProposalMatcher.Similarities), saved the first time it
// is computed so the report, the CSV of the project and the comparison do not
// compute the embeddings again (a few seconds per job with the real model).
//
// The proposals of a finished job do not change (a new arbitration is another
// job), so only two things make it old: the embedder (another model gives
// other similarities) and the way the platform builds it (Version, raised if
// the text compared for each element changes). The thresholds and the
// arbiter are not in it: they are applied on each use (ProposalMatcher.Classify).
public static class MatchingCache
{
    public const string FileName = "emparejamiento.json";
    public const int Version = 1;

    private record CacheFile(int Version, string Embedder, IReadOnlyList<ElementSimilarity> Elements);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // The saved similarities, or null if there are none, they are from another
    // embedder or version, or the file cannot be read (then they are computed again).
    public static ProposalSimilarities? Read(string jobFolder, string embedder)
    {
        var path = Path.Combine(jobFolder, FileName);
        if (!File.Exists(path))
            return null;
        try
        {
            var file = JsonSerializer.Deserialize<CacheFile>(File.ReadAllText(path), Options);
            return file is not null && file.Version == Version && file.Embedder == embedder && file.Elements is not null
                ? new ProposalSimilarities(file.Embedder, file.Elements)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Written to a temporary file and then moved, so whoever reads it at the
    // same time never sees it half written.
    public static void Write(string jobFolder, ProposalSimilarities similarities)
    {
        Directory.CreateDirectory(jobFolder);
        var path = Path.Combine(jobFolder, FileName);
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new CacheFile(Version, similarities.Embedder, similarities.Elements), Options));
        File.Move(temporary, path, overwrite: true);
    }
}
