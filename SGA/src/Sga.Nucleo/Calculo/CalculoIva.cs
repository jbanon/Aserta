namespace Sga.Nucleo.Calculo;

/// <summary>Una linea de libro (emitida o recibida) reducida a lo que necesita el IVA.</summary>
public readonly record struct LineaIva(DateOnly Fecha, decimal Base, decimal TipoIva, decimal Cuota, bool Deducible = true);

/// <summary>Lo que ya se liquido en un trimestre anterior del mismo ejercicio.</summary>
public readonly record struct LiquidacionAnterior(string Periodo, decimal RepercutidoLiquidado, decimal SoportadoLiquidado, decimal Resultado);

public sealed record DesgloseTipo(decimal Tipo, decimal Base, decimal Cuota);

/// <summary>
/// Resultado del calculo acumulado, igual que la hoja "LIQ IVA" del Excel de SGA:
/// cuota del ano hasta la fecha ("segun libros") menos lo liquidado en trimestres anteriores.
/// </summary>
public sealed record ResultadoIva(
    short Ejercicio, string Periodo,
    decimal BaseRepercutidaAcumulada, decimal RepercutidoAcumulado, IReadOnlyList<DesgloseTipo> DesgloseRepercutido,
    decimal BaseSoportadaAcumulada, decimal SoportadoAcumulado, IReadOnlyList<DesgloseTipo> DesgloseSoportado,
    IReadOnlyList<LiquidacionAnterior> Anteriores,
    decimal RepercutidoLiquidadoAnterior, decimal SoportadoLiquidadoAnterior,
    decimal RepercutidoALiquidar, decimal SoportadoALiquidar, decimal Resultado,
    decimal BaseRepercutidaTrimestre, decimal BaseSoportadaTrimestre)
{
    public bool AIngresar => Resultado > 0;
    public bool ACompensar => Resultado < 0;
}

public static class CalculoIva
{
    /// <summary>
    /// Calcula la liquidacion del trimestre <paramref name="periodo"/> ("1T".."4T") a partir de TODAS las lineas del ejercicio:
    /// se acumula lo devengado hasta el fin del trimestre y se resta lo liquidado en los anteriores.
    /// </summary>
    public static ResultadoIva Liquidar(short ejercicio, string periodo, IEnumerable<LineaIva> emitidas, IEnumerable<LineaIva> recibidas, IEnumerable<LiquidacionAnterior> anteriores)
    {
        var (inicio, fin) = Aserta.Dominio.Catalogo.Periodo.Rango(ejercicio, periodo);
        var desdeEjercicio = new DateOnly(ejercicio, 1, 1);
        var em = emitidas.Where(l => l.Fecha >= desdeEjercicio && l.Fecha <= fin).ToList();
        var re = recibidas.Where(l => l.Fecha >= desdeEjercicio && l.Fecha <= fin && l.Deducible).ToList();
        var ant = anteriores.Where(a => Aserta.Dominio.Catalogo.Periodo.Orden(a.Periodo) < Aserta.Dominio.Catalogo.Periodo.Orden(periodo)).OrderBy(a => Aserta.Dominio.Catalogo.Periodo.Orden(a.Periodo)).ToList();

        decimal R(decimal v) => CalculoFactura.Redondear(v);
        var repAcum = R(em.Sum(l => l.Cuota));
        var sopAcum = R(re.Sum(l => l.Cuota));
        var repAnt = R(ant.Sum(a => a.RepercutidoLiquidado));
        var sopAnt = R(ant.Sum(a => a.SoportadoLiquidado));
        var repLiq = R(repAcum - repAnt);
        var sopLiq = R(sopAcum - sopAnt);

        static List<DesgloseTipo> Desglose(IEnumerable<LineaIva> lineas) =>
            lineas.GroupBy(l => l.TipoIva).OrderByDescending(g => g.Key).Select(g => new DesgloseTipo(g.Key, CalculoFactura.Redondear(g.Sum(x => x.Base)), CalculoFactura.Redondear(g.Sum(x => x.Cuota)))).ToList();

        return new ResultadoIva(ejercicio, periodo,
            R(em.Sum(l => l.Base)), repAcum, Desglose(em),
            R(re.Sum(l => l.Base)), sopAcum, Desglose(re),
            ant, repAnt, sopAnt, repLiq, sopLiq, R(repLiq - sopLiq),
            R(em.Where(l => l.Fecha >= inicio).Sum(l => l.Base)), R(re.Where(l => l.Fecha >= inicio).Sum(l => l.Base)));
    }
}
