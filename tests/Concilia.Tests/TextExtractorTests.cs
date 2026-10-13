using System.Text;
using Concilia.Core.Files;

namespace Concilia.Tests;

public class TextExtractorTests
{
    private const string Spanish = "Reserva de salas: el sistema permitirá cancelar una reserva.";

    [Theory]
    [InlineData(".txt")]
    [InlineData(".md")]
    [InlineData(".MD")]
    public void Supported_extensions_are_extracted(string extension)
    {
        var text = TextExtractor.Extract(Encoding.UTF8.GetBytes("hola"), extension);

        Assert.Equal("hola", text);
    }

    [Theory]
    [InlineData(".xlsx")]
    [InlineData(".doc")]
    [InlineData("")]
    public void Unsupported_extensions_return_null(string extension)
    {
        Assert.Null(TextExtractor.Extract(Encoding.UTF8.GetBytes("hola"), extension));
    }

    [Fact]
    public void Utf8_without_bom_keeps_accents()
    {
        var bytes = new UTF8Encoding(false).GetBytes(Spanish);

        Assert.Equal(Spanish, TextExtractor.Extract(bytes, ".txt"));
    }

    [Fact]
    public void Utf8_bom_is_removed()
    {
        var bytes = Encoding.UTF8.Preamble.ToArray().Concat(Encoding.UTF8.GetBytes(Spanish)).ToArray();

        Assert.Equal(Spanish, TextExtractor.Extract(bytes, ".txt"));
    }

    [Fact]
    public void Windows_1252_is_detected_when_bytes_are_not_utf8()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        // "permitirá" and "€" are encoded as single bytes in Windows-1252,
        // which are invalid as UTF-8.
        var original = Spanish + " Precio: 10 €.";
        var bytes = Encoding.GetEncoding(1252).GetBytes(original);

        Assert.Equal(original, TextExtractor.Extract(bytes, ".txt"));
    }

    [Fact]
    public void Line_endings_are_normalized()
    {
        var bytes = Encoding.UTF8.GetBytes("uno\r\ndos\rtres\ncuatro");

        Assert.Equal("uno\ndos\ntres\ncuatro", TextExtractor.Extract(bytes, ".md"));
    }
}
