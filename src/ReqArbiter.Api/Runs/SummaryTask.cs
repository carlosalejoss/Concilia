namespace ReqArbiter.Api.Runs;

// The trivial task of milestone 3: summarise the project's documents into
// output/resumen.md. It only exists to test the runner end to end.
public static class SummaryTask
{
    public const string OutputFile = "output/resumen.md";

    // Paths are absolute: Claude Code's Read and Write tools work with
    // absolute paths.
    public static string Prompt(string workDirectory, IReadOnlyList<string> inputNames)
    {
        var inputFolder = Path.Combine(workDirectory, "input");
        var outputPath = Path.Combine(workDirectory, "output", "resumen.md");
        var list = string.Join("\n", inputNames.Select(name => $"- {Path.Combine(inputFolder, name)}"));

        return $$"""
            Eres un analista de requisitos. Estos son los documentos de un proyecto, en texto plano:
            {{list}}

            Tarea:
            1. Lee todos los documentos con la herramienta Read, usando las rutas indicadas.
            2. Escribe con la herramienta Write el archivo {{outputPath}} con un resumen en español, en Markdown, de 10 a 20 líneas: de qué trata el sistema, quiénes lo usan y sus funciones principales. No añadas nada que no esté en los documentos.
            3. Termina tu respuesta con una última línea exactamente así:
            VEREDICTO: {"estado":"hecho"}

            Si no puedes hacer la tarea (por ejemplo, porque los documentos están vacíos o no se pueden leer), no escribas el archivo y termina con:
            VEREDICTO: {"estado":"bloqueada","motivo":"<explica el motivo>"}
            """;
    }
}
