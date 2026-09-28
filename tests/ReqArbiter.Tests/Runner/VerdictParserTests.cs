using ReqArbiter.Core.Runner;

namespace ReqArbiter.Tests.Runner;

public class VerdictParserTests
{
    [Fact]
    public void Done_verdict()
    {
        var parsed = VerdictParser.Parse("Resumen escrito.\nVEREDICTO: {\"estado\":\"hecho\"}");

        Assert.Equal(new Verdict(VerdictState.Done, null), parsed.Verdict);
        Assert.Null(parsed.Error);
    }

    [Fact]
    public void Blocked_verdict_keeps_its_reason()
    {
        var parsed = VerdictParser.Parse("VEREDICTO: {\"estado\":\"bloqueada\",\"motivo\":\"No hay archivos de entrada.\"}");

        Assert.Equal(new Verdict(VerdictState.Blocked, "No hay archivos de entrada."), parsed.Verdict);
    }

    [Fact]
    public void The_last_verdict_line_wins()
    {
        const string text = """
            Debo terminar con VEREDICTO: {"estado":"hecho"} o bloqueada.
            VEREDICTO: {"estado":"hecho"}
            Revisando de nuevo, falta información.
            VEREDICTO: {"estado":"bloqueada","motivo":"Falta el glosario."}
            """;

        Assert.Equal(VerdictState.Blocked, VerdictParser.Parse(text).Verdict?.State);
    }

    [Fact]
    public void Crlf_and_surrounding_spaces_are_tolerated()
    {
        var parsed = VerdictParser.Parse("Hecho.\r\n  VEREDICTO:   {\"estado\":\"hecho\"}  \r\n");

        Assert.Equal(VerdictState.Done, parsed.Verdict?.State);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("He terminado, todo correcto.")]
    [InlineData("veredicto: {\"estado\":\"hecho\"}")] // the prefix is case-sensitive
    public void Missing_verdict_is_not_valid(string? text)
    {
        var parsed = VerdictParser.Parse(text);

        Assert.Null(parsed.Verdict);
        Assert.StartsWith("No se ha encontrado", parsed.Error);
    }

    [Theory]
    [InlineData("VEREDICTO: {estado: hecho", "no es válido")]
    [InlineData("VEREDICTO: \"hecho\"", "no es un objeto")]
    [InlineData("VEREDICTO: {\"motivo\":\"x\"}", "no tiene el campo")]
    [InlineData("VEREDICTO: {\"estado\":\"terminado\"}", "desconocido")]
    [InlineData("VEREDICTO: {\"estado\":\"Hecho\"}", "desconocido")]
    public void Invalid_verdicts_are_not_valid(string text, string expectedError)
    {
        var parsed = VerdictParser.Parse(text);

        Assert.Null(parsed.Verdict);
        Assert.Contains(expectedError, parsed.Error);
    }
}
