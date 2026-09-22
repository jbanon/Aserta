using System.Globalization;
using Aserta.Verifactu.Comun;
using Aserta.Verifactu.Qr;

namespace Aserta.Verifactu.Tests;

public class QrYFormatosTests
{
    [Fact]
    public void Url_del_qr_con_los_cuatro_parametros_en_orden_y_codificados()
    {
        var url = ConstructorUrlQr.ConstruirUrlQr(new OpcionesQr { Entorno = EntornoAeat.Pruebas }, "89890001K", "12345678/G33", new DateOnly(2024, 9, 1), 241.4m);
        Assert.Equal("https://prewww2.aeat.es/wlpl/TIKE-CONT/ValidarQR?nif=89890001K&numserie=12345678%2FG33&fecha=01-09-2024&importe=241.40", url);
    }

    [Fact]
    public void Url_de_produccion_cambia_solo_el_host()
    {
        var url = ConstructorUrlQr.ConstruirUrlQr(new OpcionesQr { Entorno = EntornoAeat.Produccion }, "89890001K", "12345678-G33", new DateOnly(2024, 9, 1), 241.4m);
        Assert.StartsWith("https://www2.agenciatributaria.gob.es/wlpl/TIKE-CONT/ValidarQR?nif=89890001K&numserie=12345678-G33", url);
    }

    [Fact]
    public void La_url_del_qr_nunca_lleva_formato_json_pero_la_de_cotejo_si()
    {
        var o = new OpcionesQr();
        Assert.DoesNotContain("formato", ConstructorUrlQr.ConstruirUrlQr(o, "89890001K", "1", new DateOnly(2026, 1, 1), 1m));
        Assert.EndsWith("&idioma=es&formato=json", ConstructorUrlQr.ConstruirUrlCotejoJson(o, "89890001K", "1", new DateOnly(2026, 1, 1), 1m));
    }

    [Fact]
    public void Limites_del_qr_numserie_60_e_importe_12_enteros()
    {
        var o = new OpcionesQr();
        Assert.Throws<ArgumentException>(() => ConstructorUrlQr.ConstruirUrlQr(o, "89890001K", new string('A', 61), new DateOnly(2026, 1, 1), 1m));
        Assert.Throws<ArgumentException>(() => ConstructorUrlQr.ConstruirUrlQr(o, "89890001K", "A1", new DateOnly(2026, 1, 1), 1_000_000_000_000m));
        Assert.Throws<ArgumentException>(() => ConstructorUrlQr.ConstruirUrlQr(o, "89890001K", "Añ1", new DateOnly(2026, 1, 1), 1m));
        ConstructorUrlQr.ConstruirUrlQr(o, "89890001K", new string('A', 60), new DateOnly(2026, 1, 1), 999_999_999_999.99m);
    }

    [Fact]
    public void Formatos_invariantes_aunque_la_cultura_sea_es_ES()
    {
        var anterior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("es-ES");
            Assert.Equal("1234.50", FormatosVerifactu.Importe(1234.5m));
            Assert.Equal("01-09-2024", FormatosVerifactu.Fecha(new DateOnly(2024, 9, 1)));
            Assert.Equal("2024-01-01T19:20:30+01:00", FormatosVerifactu.FechaHoraHuso(new DateTimeOffset(2024, 1, 1, 19, 20, 30, TimeSpan.FromHours(1))));
            Assert.Equal("2026-07-15T10:00:00+02:00", FormatosVerifactu.FechaHoraHuso(new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.FromHours(2))));
        }
        finally { CultureInfo.CurrentCulture = anterior; }
    }

    [Fact]
    public void El_qr_se_genera_en_servidor_con_correccion_M()
    {
        var png = GeneradorQrVerifactu.Png("https://prewww2.aeat.es/wlpl/TIKE-CONT/ValidarQR?nif=89890001K&numserie=1&fecha=01-01-2026&importe=1.00");
        Assert.True(png.Length > 100);
        Assert.Equal(0x89, png[0]); Assert.Equal((byte)'P', png[1]);
        Assert.InRange(GeneradorQrVerifactu.Modulos("https://prewww2.aeat.es/wlpl/TIKE-CONT/ValidarQR?nif=89890001K&numserie=1&fecha=01-01-2026&importe=1.00"), 25, 60);
    }

    [Fact]
    public void Calculo_de_importes_por_tipo_con_recargo_y_retencion()
    {
        var t = CalculadoraImportes.Calcular(
        [
            new LineaImporte("Servicio", 2, 100m, 21m, null, false),
            new LineaImporte("Pan", 10, 1.5m, 4m, 0.5m, false),
            new LineaImporte("Exento", 1, 50m, 0m, null, true),
        ], porcentajeRetencion: 15m);
        Assert.Equal(265m, t.BaseTotal);
        Assert.Equal(42.6m, t.CuotaTotal);      // 200*21% + 15*4% = 42 + 0.6
        Assert.Equal(0.08m, t.CuotaRecargoTotal); // 15*0.5% = 0.075 -> 0.08
        Assert.Equal(39.75m, t.RetencionTotal);  // 265*15%
        Assert.Equal(265m + 42.6m + 0.08m - 39.75m, t.ImporteTotal);
        Assert.Equal(3, t.Desglose.Count);
    }
}
