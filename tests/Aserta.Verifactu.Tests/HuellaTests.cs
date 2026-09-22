using System.Globalization;
using Aserta.Verifactu.Huella;

namespace Aserta.Verifactu.Tests;

/// <summary>
/// Los tres vectores OFICIALES de la AEAT (huella-y-vectores-prueba.md §5), ya
/// verificados por el arquitecto. Si uno falla, la implementacion esta mal y NO
/// se despliega. Se ejecutan ademas bajo tres culturas (H1).
/// </summary>
public class HuellaTests
{
    private static readonly DatosHuellaAlta Caso1 = new("89890001K", "12345678/G33", "01-01-2024", "F1", "12.35", "123.45", null, "2024-01-01T19:20:30+01:00");
    private static readonly DatosHuellaAlta Caso2 = new("89890001K", "12345679/G34", "01-01-2024", "F1", "12.35", "123.45", "3C464DAF61ACB827C65FDA19F352A4E3BDC2C640E9E9FC4CC058073F38F12F60", "2024-01-01T19:20:35+01:00");
    private static readonly DatosHuellaAnulacion Caso3 = new("89890001K", "12345679/G34", "01-01-2024", "F7B94CFD8924EDFF273501B01EE5153E4CE8F259766F88CF6ACB8935802A2B97", "2024-01-01T19:20:40+01:00");

    public static IEnumerable<object[]> Culturas => [["es-ES"], ["en-US"], [""]];

    [Theory, MemberData(nameof(Culturas))]
    public void Caso1_alta_primer_registro(string cultura)
    {
        EnCultura(cultura, () =>
        {
            var cadena = CalculadoraHuella.ConstruirCadenaAlta(Caso1);
            Assert.Equal("IDEmisorFactura=89890001K&NumSerieFactura=12345678/G33&FechaExpedicionFactura=01-01-2024&TipoFactura=F1&CuotaTotal=12.35&ImporteTotal=123.45&Huella=&FechaHoraHusoGenRegistro=2024-01-01T19:20:30+01:00", cadena);
            Assert.Equal(199, cadena.Length);
            Assert.Equal("3C464DAF61ACB827C65FDA19F352A4E3BDC2C640E9E9FC4CC058073F38F12F60", CalculadoraHuella.CalcularHuella(cadena));
        });
    }

    [Theory, MemberData(nameof(Culturas))]
    public void Caso2_alta_encadenado(string cultura)
    {
        EnCultura(cultura, () =>
        {
            var cadena = CalculadoraHuella.ConstruirCadenaAlta(Caso2);
            Assert.Equal(263, cadena.Length);
            Assert.Equal("F7B94CFD8924EDFF273501B01EE5153E4CE8F259766F88CF6ACB8935802A2B97", CalculadoraHuella.CalcularHuella(cadena));
        });
    }

    [Theory, MemberData(nameof(Culturas))]
    public void Caso3_anulacion_encadenada(string cultura)
    {
        EnCultura(cultura, () =>
        {
            var cadena = CalculadoraHuella.ConstruirCadenaAnulacion(Caso3);
            Assert.Equal("IDEmisorFacturaAnulada=89890001K&NumSerieFacturaAnulada=12345679/G34&FechaExpedicionFacturaAnulada=01-01-2024&Huella=F7B94CFD8924EDFF273501B01EE5153E4CE8F259766F88CF6ACB8935802A2B97&FechaHoraHusoGenRegistro=2024-01-01T19:20:40+01:00", cadena);
            Assert.Equal(232, cadena.Length);
            Assert.Equal("177547C0D57AC74748561D054A9CEC14B4C4EA23D1BEFD6F2E69E3A388F90C68", CalculadoraHuella.CalcularHuella(cadena));
        });
    }

    [Fact]
    public void La_huella_es_hexadecimal_mayusculas_de_64()
    {
        var h = CalculadoraHuella.HuellaAlta(Caso1);
        Assert.Equal(64, h.Length);
        Assert.Equal(h, h.ToUpperInvariant());
        Assert.Matches("^[0-9A-F]{64}$", h);
    }

    [Fact]
    public void Los_valores_se_recortan_y_el_numserie_no_se_codifica()
    {
        var conEspacios = Caso1 with { NumSerieFactura = "  12345678/G33 " };
        Assert.Equal(CalculadoraHuella.ConstruirCadenaAlta(Caso1), CalculadoraHuella.ConstruirCadenaAlta(conEspacios));
        Assert.DoesNotContain("%2F", CalculadoraHuella.ConstruirCadenaAlta(Caso1));
    }

    [Fact]
    public void Cadena_de_50_registros_verifica_de_principio_a_fin()
    {
        string? anterior = null;
        var cadenas = new List<(string Cadena, string Huella)>();
        for (int i = 1; i <= 50; i++)
        {
            var d = new DatosHuellaAlta("89890001K", $"A/{i}", "15-03-2026", "F1", Comun.FormatosVerifactu.Importe(2.1m * i), Comun.FormatosVerifactu.Importe(12.1m * i), anterior, $"2026-03-15T10:{i:00}:00+01:00");
            var cadena = CalculadoraHuella.ConstruirCadenaAlta(d);
            anterior = CalculadoraHuella.CalcularHuella(cadena);
            cadenas.Add((cadena, anterior));
        }
        // Reverificacion "de tres lineas" desde las cadenas guardadas
        string? previa = null;
        foreach (var (cadena, huella) in cadenas)
        {
            Assert.Contains($"&Huella={previa ?? ""}&", cadena);
            Assert.Equal(huella, CalculadoraHuella.CalcularHuella(cadena));
            previa = huella;
        }
    }

    private static void EnCultura(string nombre, Action accion)
    {
        var anterior = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = nombre == "" ? CultureInfo.InvariantCulture : new CultureInfo(nombre); accion(); }
        finally { CultureInfo.CurrentCulture = anterior; }
    }
}
