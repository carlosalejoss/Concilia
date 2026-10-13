using System.Text.Json;
using Concilia.Core.Catalog;

namespace Concilia.Api.Runs;

// Validates the catalog a run wrote and saves, next to it, the validator's
// report (output/informe.json: lot errors, rejected elements, warnings,
// metrics) and the accepted catalog (output/catalogo_aceptado.json: normalized,
// without the rejected elements and renumbered by type), which is what the
// merge of the job uses.
public static class CatalogReport
{
    public const string ReportFile = "output/informe.json";
    public const string AcceptedFile = "output/catalogo_aceptado.json";

    private record FragmentsFile(List<Fragment> Fragments);

    public static CatalogValidationReport ValidateRunFolder(string workDirectory, string systemName)
    {
        var catalogPath = Path.Combine(workDirectory, CatalogTask.OutputFile);
        CatalogValidationReport report;
        if (File.Exists(catalogPath))
        {
            report = CatalogValidator.Validate(File.ReadAllText(catalogPath), systemName, LoadFragments(workDirectory));
        }
        else
        {
            List<string> missing = [$"LOTE Falta el fichero {CatalogTask.OutputFile}"];
            report = new CatalogValidationReport(false, true, missing, missing, [], [], null,
                new Dictionary<string, string>(), null, new Dictionary<string, string?>());
        }

        var reportPath = Path.Combine(workDirectory, ReportFile);
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, CatalogTask.Json));
        if (report.Catalog is not null)
        {
            File.WriteAllText(Path.Combine(workDirectory, AcceptedFile), report.Catalog);
        }
        return report;
    }

    // The lot's fragments (input/fragmentos.json), written by CatalogTask.BuildRequest.
    public static List<Fragment>? LoadFragments(string workDirectory)
    {
        var path = Path.Combine(workDirectory, CatalogTask.FragmentsFile);
        return File.Exists(path)
            ? JsonSerializer.Deserialize<FragmentsFile>(File.ReadAllText(path), CatalogTask.Json)?.Fragments
            : null;
    }
}
