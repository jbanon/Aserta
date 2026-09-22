using Aserta.Dominio.Comun;

namespace Aserta.Dominio.Tests;

public class ValidadorNifTests
{
    [Theory]
    [InlineData("00000000T")]
    [InlineData("12345678Z")]
    [InlineData("99999999R")]
    [InlineData("X0000000T")] // NIE
    [InlineData("Y1234567X")]
    [InlineData("Z9999999H")]
    public void Acepta_nif_y_nie_validos(string nif)
    {
        var r = ValidadorNif.Validar(nif);
        Assert.True(r.EsValido, r.Error);
        Assert.NotEqual(ValidadorNif.TipoIdentificador.Cif, r.Tipo);
    }

    [Theory]
    [InlineData("12345678A")]
    [InlineData("X0000000A")]
    [InlineData("1234567Z")]
    [InlineData("")]
    [InlineData("ABCDEFGHI")]
    public void Rechaza_nif_invalidos(string nif)
    {
        var r = ValidadorNif.Validar(nif);
        Assert.False(r.EsValido);
        Assert.False(string.IsNullOrEmpty(r.Error));
    }

    [Theory]
    [InlineData('A', 1234567)]
    [InlineData('B', 8123456)]
    [InlineData('E', 7654321)]
    [InlineData('P', 2800000)] // control letra obligatoria
    [InlineData('Q', 1234567)]
    public void Cif_construido_es_valido(char letra, int numero)
    {
        var cif = ValidadorNif.ConstruirCif(letra, numero);
        var r = ValidadorNif.Validar(cif);
        Assert.True(r.EsValido, r.Error);
        Assert.Equal(ValidadorNif.TipoIdentificador.Cif, r.Tipo);
        Assert.Equal(9, cif.Length);
    }

    [Fact]
    public void Cif_con_control_incorrecto_se_rechaza()
    {
        var cif = ValidadorNif.ConstruirCif('B', 4567890);
        var malo = cif[..8] + (cif[8] == '0' ? '1' : '0');
        Assert.False(ValidadorNif.EsValido(malo));
    }

    [Fact]
    public void Normaliza_espacios_guiones_y_minusculas()
    {
        var r = ValidadorNif.Validar(" 12345678-z ");
        Assert.True(r.EsValido);
        Assert.Equal("12345678Z", r.Normalizado);
    }

    [Fact]
    public void Nif_construido_es_valido()
    {
        for (int n = 0; n < 50; n++)
            Assert.True(ValidadorNif.EsValido(ValidadorNif.ConstruirNif(n * 1234567 % 99999999)));
    }
}
