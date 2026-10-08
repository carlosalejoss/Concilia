using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ReqArbiter.Core.Catalog;

// Values for the placeholders of the requirements prompt (§1).
public record CatalogPromptInput(
    string SystemName,        // with article: "el sistema ReservaSalas"
    string Language,          // "es" or "en"
    int LotNumber,
    int LotTotal,
    string Glossary,          // may be empty
    string ResolvedDoubts,    // <f id="DUDAS#..."> block; empty until the doubts phase exists
    string Index,             // titles of every section of the documents (Fragmenter.IndexBlock)
    string Documentation);    // <documento> block of the lot's fragments

// Assembles the prompt that models A and B receive, from the versioned files in
// prompts/generacion_requisitos/ (embedded in this DLL).
public static partial class PromptBuilder
{
    // Stored with every run, together with the SHA-256 of the assembled prompt.
    public const string Version = "req-1.4";

    public static string Template { get; } = ReadResource("prompt_requisitos.md");
    public static string RepairAnnexTemplate { get; } = ReadResource("anexo_reparacion.md");

    // {{NAME}}: upper-case letters, digits and underscores (PROPUESTA_1 in the v0 arbiters).
    [GeneratedRegex(@"\{\{([A-Z0-9_]+)\}\}")]
    private static partial Regex PlaceholderRe();

    public static string Build(CatalogPromptInput input) => Fill(Template, new Dictionary<string, string>
    {
        ["NOMBRE_SISTEMA"] = input.SystemName,
        ["IDIOMA"] = input.Language,
        ["LOTE_NUMERO"] = input.LotNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["LOTE_TOTAL"] = input.LotTotal.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["GLOSARIO"] = input.Glossary,
        ["DUDAS_RESUELTAS"] = input.ResolvedDoubts,
        ["INDICE"] = input.Index,
        ["DOCUMENTACION"] = input.Documentation,
    });

    // The text added after the original prompt in a repair retry (§12).
    // errors: one line per reason for the repair (CatalogValidationReport.RepairReasons).
    public static string BuildRepairAnnex(int attempt, string previousCatalog, IEnumerable<string> errors) =>
        Fill(RepairAnnexTemplate, new Dictionary<string, string>
        {
            ["INTENTO"] = attempt.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["CATALOGO_PREVIO"] = previousCatalog,
            ["ERRORES"] = string.Join("\n", errors),
        });

    // SHA-256 of the assembled prompt, as 64 lowercase hex characters.
    public static string Sha256(string prompt) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(prompt)));

    // ONE pass over the template: each {{NAME}} is replaced by its value, and
    // the inserted values are never scanned again. A document can contain
    // "{{...}}" and it must reach the model as it is. Unknown names are left
    // untouched.
    internal static string Fill(string template, IReadOnlyDictionary<string, string> values) =>
        PlaceholderRe().Replace(template, m => values.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);

    internal static string ReadResource(string name)
    {
        using var stream = typeof(PromptBuilder).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource not found: {name}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
