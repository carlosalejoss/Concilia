using Concilia.Core.Files;

namespace Concilia.Tests;

public class UploadRulesTests
{
    [Theory]
    [InlineData("requisitos.txt")]
    [InlineData("requisitos.md")]
    [InlineData("requisitos.pdf")]
    [InlineData("requisitos.docx")]
    [InlineData("REQUISITOS.PDF")]
    public void Allowed_types_are_valid(string fileName)
    {
        Assert.Null(UploadRules.Validate(fileName, 1000));
    }

    [Theory]
    [InlineData("programa.exe")]
    [InlineData("hoja.xlsx")]
    [InlineData("antiguo.doc")]
    [InlineData("sin_extension")]
    public void Other_types_are_rejected(string fileName)
    {
        var error = UploadRules.Validate(fileName, 1000);

        Assert.NotNull(error);
        Assert.StartsWith("Tipo de archivo no permitido", error);
    }

    [Fact]
    public void Empty_file_is_rejected()
    {
        Assert.Equal("El archivo está vacío.", UploadRules.Validate("a.txt", 0));
    }

    [Fact]
    public void Size_limit_is_inclusive()
    {
        Assert.Null(UploadRules.Validate("a.pdf", UploadRules.MaxBytes));
        Assert.Equal(
            "El archivo supera el tamaño máximo de 10 MB.",
            UploadRules.Validate("a.pdf", UploadRules.MaxBytes + 1));
    }
}
