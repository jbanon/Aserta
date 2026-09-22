namespace Aserta.Verifactu.Comun;

/// <summary>Una linea de factura tal y como la introduce el usuario.</summary>
public sealed record LineaImporte(string Descripcion, decimal Cantidad, decimal PrecioUnitario, decimal TipoIva, decimal? TipoRecargoEquivalencia, bool Exenta);

public sealed record DesgloseIva(decimal TipoIva, decimal Base, decimal Cuota, decimal? TipoRecargo, decimal CuotaRecargo, bool Exenta);

public sealed record Totales(decimal BaseTotal, decimal CuotaTotal, decimal CuotaRecargoTotal, decimal RetencionTotal, decimal ImporteTotal, IReadOnlyList<DesgloseIva> Desglose);

/// <summary>
/// Calculo de importes con redondeo a dos decimales por tipo (V6: IVA general,
/// reducido, superreducido, exenta y recargo de equivalencia; retencion de IRPF).
/// ImporteTotal = base + cuotas + recargo - retencion.
/// </summary>
public static class CalculadoraImportes
{
    public static readonly decimal[] TiposIvaAdmitidos = [21m, 10m, 4m, 0m];

    public static Totales Calcular(IEnumerable<LineaImporte> lineas, decimal porcentajeRetencion = 0m)
    {
        var grupos = lineas
            .GroupBy(l => (l.Exenta ? 0m : l.TipoIva, l.Exenta ? null : l.TipoRecargoEquivalencia, l.Exenta))
            .Select(g =>
            {
                decimal baseG = Math.Round(g.Sum(l => Math.Round(l.Cantidad * l.PrecioUnitario, 2)), 2);
                decimal cuota = g.Key.Exenta ? 0m : Math.Round(baseG * g.Key.Item1 / 100m, 2, MidpointRounding.AwayFromZero);
                decimal recargo = g.Key.Item2 is decimal tr && !g.Key.Exenta ? Math.Round(baseG * tr / 100m, 2, MidpointRounding.AwayFromZero) : 0m;
                return new DesgloseIva(g.Key.Item1, baseG, cuota, g.Key.Item2, recargo, g.Key.Exenta);
            })
            .OrderByDescending(d => d.TipoIva).ToList();

        decimal baseTotal = grupos.Sum(d => d.Base);
        decimal cuotaTotal = grupos.Sum(d => d.Cuota);
        decimal recargoTotal = grupos.Sum(d => d.CuotaRecargo);
        decimal retencion = porcentajeRetencion > 0 ? Math.Round(baseTotal * porcentajeRetencion / 100m, 2, MidpointRounding.AwayFromZero) : 0m;
        return new Totales(baseTotal, cuotaTotal, recargoTotal, retencion, baseTotal + cuotaTotal + recargoTotal - retencion, grupos);
    }
}
