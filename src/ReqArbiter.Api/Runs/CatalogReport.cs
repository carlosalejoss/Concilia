using System.Text.Json;
using ReqArbiter.Core.Catalog;

namespace ReqArbiter.Api.Runs;

// Validates the catalog a run wrote and saves the validator's report next to
// it, in output/informe.json (errors, warnings and metrics).
public static class CatalogReport
{
    public const string ReportFile = "output/informe.json";

    private record FragmentsFile(List<Fragment> Fragments);

    public static CatalogValidationReport ValidateRunFolder(string workDirectory, string systemName)
    {
        var catalogPath = Path.Combine(workDirectory, CatalogTask.OutputFile);
        var report = File.Exists(catalogPath)
            ? CatalogValidator.Validate(File.ReadAllText(catalogPath), systemName, LoadFragments(workDirectory))
            : new CatalogValidationReport(false, [$"Falta el fichero {CatalogTask.OutputFile}"], [], null, new Dictionary<string, string?>());

        var reportPath = Path.Combine(workDirectory, ReportFile);
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, CatalogTask.Json));
        return report;
    }

    // The lot's fragments (input/fragmentos.json), written by CatalogTask.BuildRequest.
    private static List<Fragment>? LoadFragments(string workDirectory)
    {
        var path = Path.Combine(workDirectory, CatalogTask.FragmentsFile);
        return File.Exists(path)
            ? JsonSerializer.Deserialize<FragmentsFile>(File.ReadAllText(path), CatalogTask.Json)?.Fragments
            : null;
    }
}
