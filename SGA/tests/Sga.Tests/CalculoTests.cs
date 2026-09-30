using Sga.Nucleo.Calculo;

namespace Sga.Tests;

/// <summary>
/// Oraculos anonimizados (cifras, no ficheros) de los Excel de referencia de SGA, tal como los fija el encargo:
/// IVA acumulado del arrendador en 3T y resumen de la sociedad en 2T, retencion de la factura de alquiler.
/// </summary>
public class CalculoTests
{
    private static LineaIva Emitida(int mes, decimal baseImp, decimal tipo = 21m) => new(new DateOnly(2026, mes, 1), baseImp, tipo, CalculoFactura.Redondear(baseImp * tipo / 100m));

    [Fact]
    public void Factura_de_alquiler_2000_con_21_de_iva_y_19_de_retencion_da_2040()
    {
        var f = CalculoFactura.Alquiler(2000m);
        Assert.Equal(420m, f.CuotaIva);
        Assert.Equal(380m, f.CuotaRetencion);
        Assert.Equal(2040m, f.Total);
        Assert.Equal("26/8", CalculoFactura.NumeroAlquiler(new DateOnly(2026, 8, 1)));
        Assert.Equal("26/8-2", CalculoFactura.NumeroAlquiler(new DateOnly(2026, 8, 1), 2));
    }

    [Fact]
    public void Arrendador_3T_acumulado_repercutido_3780_menos_1260_y_1260_soportado_21_75_resultado_1252_75()
    {
        // Nueve facturas de 2.000 € (enero a septiembre) y tres de honorarios de 34,50 € (una por trimestre)
        var emitidas = Enumerable.Range(1, 9).Select(m => Emitida(m, 2000m));
        var recibidas = new[] { 2, 5, 8 }.Select(m => Emitida(m, 34.5m));
        var anteriores = new[] { new LiquidacionAnterior("1T", 1260m, 7.25m, 1252.75m), new LiquidacionAnterior("2T", 1260m, 7.25m, 1252.75m) };

        var r = CalculoIva.Liquidar(2026, "3T", emitidas, recibidas, anteriores);

        Assert.Equal(18000m, r.BaseRepercutidaAcumulada);
        Assert.Equal(3780m, r.RepercutidoAcumulado);
        Assert.Equal(2520m, r.RepercutidoLiquidadoAnterior);
        Assert.Equal(1260m, r.RepercutidoALiquidar);
        Assert.Equal(21.75m, r.SoportadoAcumulado);
        Assert.Equal(7.25m, r.SoportadoALiquidar);
        Assert.Equal(1252.75m, r.Resultado);
        Assert.True(r.AIngresar);
    }

    [Fact]
    public void Arrendador_2T_reproduce_el_303_presentado()
    {
        var emitidas = Enumerable.Range(1, 9).Select(m => Emitida(m, 2000m));
        var recibidas = new[] { 2, 5, 8 }.Select(m => Emitida(m, 34.5m));
        var r = CalculoIva.Liquidar(2026, "2T", emitidas, recibidas, [new LiquidacionAnterior("1T", 1260m, 7.25m, 1252.75m)]);
        Assert.Equal(12000m, r.BaseRepercutidaAcumulada); // enero-junio
        Assert.Equal(6000m, r.BaseRepercutidaTrimestre);   // casilla 07 del 303 real
        Assert.Equal(1260m, r.RepercutidoALiquidar);      // casilla 27
        Assert.Equal(34.5m, r.BaseSoportadaTrimestre);     // casilla 28
        Assert.Equal(7.25m, r.SoportadoALiquidar);        // casilla 29
        Assert.Equal(1252.75m, r.Resultado);              // casilla 71
    }

    [Fact]
    public void Sociedad_2T_resumen_repercutido_27260_10_menos_11884_95_soportado_6063_75_menos_3891_62()
    {
        // Cifras del resumen de la sociedad de referencia; el detalle de facturas es sintetico pero suma lo mismo
        var emitidas = new List<LineaIva>
        {
            new(new(2026, 2, 10), 56595m, 21m, 11884.95m),   // 1T (ya liquidado)
            new(new(2026, 5, 20), 73215m, 21m, 15375.15m),   // 2T
            new(new(2026, 3, 3), 317990.12m, 0m, 0m),        // exportaciones al 0 %
        };
        var recibidas = new List<LineaIva>
        {
            new(new(2026, 1, 15), 18531.52m, 21m, 3891.62m), // 1T
            new(new(2026, 4, 15), 10343.48m, 21m, 2172.13m), // 2T
        };
        var r = CalculoIva.Liquidar(2026, "2T", emitidas, recibidas, [new LiquidacionAnterior("1T", 11884.95m, 3891.62m, 7993.33m)]);
        Assert.Equal(447800.12m, r.BaseRepercutidaAcumulada);
        Assert.Equal(27260.10m, r.RepercutidoAcumulado);
        Assert.Equal(15375.15m, r.RepercutidoALiquidar);
        Assert.Equal(6063.75m, r.SoportadoAcumulado);
        Assert.Equal(2172.13m, r.SoportadoALiquidar);
        Assert.Equal(13203.02m, r.Resultado);
    }

    [Fact]
    public void Numeracion_detecta_huecos_duplicados_y_fechas_desordenadas()
    {
        var facturas = new[]
        {
            new FacturaNumerada("AF-2026-01", new(2026, 1, 10)),
            new FacturaNumerada("AF-2026-02", new(2026, 1, 20)),
            new FacturaNumerada("AF-2026-04", new(2026, 2, 5)),   // falta la 03
            new FacturaNumerada("AF-2026-05", new(2026, 2, 1)),   // fecha anterior a la 04
            new FacturaNumerada("AF-2026-05", new(2026, 2, 12)),  // duplicada
            new FacturaNumerada("AF-2026-08", new(2026, 3, 1)),   // faltan 06 y 07
        };
        var informe = ComprobadorNumeracion.Analizar(facturas);
        Assert.False(informe.Correcta);
        Assert.Equal(2, informe.Huecos);
        Assert.Equal(1, informe.Duplicados);
        Assert.Equal(1, informe.Desordenadas);
        Assert.Contains(informe.Anomalias, a => a.Tipo == TipoAnomalia.Hueco && a.Detalle.Contains("AF-2026-03"));
        Assert.Equal("AF-2026-", informe.Serie);

        var correcta = ComprobadorNumeracion.Analizar(Enumerable.Range(1, 12).Select(i => new FacturaNumerada($"26/{i}", new DateOnly(2026, i, 1))));
        Assert.True(correcta.Correcta);
    }

    [Fact]
    public void Calendario_traslada_el_fin_de_plazo_al_siguiente_dia_habil()
    {
        var p = Sga.Nucleo.Calendario.CalendarioFiscal.Buscar("303", 2026, "3T")!;
        Assert.Equal(new DateOnly(2026, 10, 1), p.Inicio);
        Assert.Equal(new DateOnly(2026, 10, 20), p.Fin);
        Assert.False(p.Confirmado);
        var (ej, per) = Sga.Nucleo.Calendario.CalendarioFiscal.TrimestreEnCampana(new DateOnly(2026, 10, 6));
        Assert.Equal((2026, "3T"), (ej, per));
        var enero = Sga.Nucleo.Calendario.CalendarioFiscal.Buscar("303", 2026, "4T")!;
        Assert.Equal(new DateOnly(2027, 2, 1), enero.Fin); // 30/01/2027 es sabado -> lunes 1 de febrero
    }
}
