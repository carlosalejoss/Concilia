using System.Text.Json;

namespace Concilia.Core.Runner;

// Either a verdict or the reason why there is none.
public record VerdictParseResult(Verdict? Verdict, string? Error);

// The prompt requires the final text to end with one of:
//   VEREDICTO: {"estado":"hecho"}
//   VEREDICTO: {"estado":"bloqueada","motivo":"..."}
// Anything else means the verdict is not valid, and nothing is taken as good.
public static class VerdictParser
{
    public const string Prefix = "VEREDICTO:";

    public static VerdictParseResult Parse(string? resultText)
    {
        // The LAST line that starts with the prefix counts: the model may have
        // quoted the format earlier in its answer.
        var line = (resultText ?? "")
            .Split('\n')
            .Select(l => l.Trim())
            .LastOrDefault(l => l.StartsWith(Prefix, StringComparison.Ordinal));

        if (line is null)
        {
            return new(null, $"No se ha encontrado ninguna línea que empiece por \"{Prefix}\".");
        }

        var json = line[Prefix.Length..].Trim();
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new(null, "El veredicto no es un objeto JSON.");
            }

            var state = root.TryGetProperty("estado", out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;
            var reason = root.TryGetProperty("motivo", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;

            return state switch
            {
                "hecho" => new(new Verdict(VerdictState.Done, reason), null),
                "bloqueada" => new(new Verdict(VerdictState.Blocked, reason), null),
                null => new(null, "El veredicto no tiene el campo \"estado\"."),
                _ => new(null, $"Estado de veredicto desconocido: \"{state}\". Solo se admiten \"hecho\" y \"bloqueada\"."),
            };
        }
        catch (JsonException)
        {
            return new(null, $"El JSON del veredicto no es válido: {json}");
        }
    }
}
