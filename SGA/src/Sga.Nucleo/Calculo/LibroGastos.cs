using Sga.Nucleo.Modelo;

namespace Sga.Nucleo.Calculo;

public sealed record TotalCategoria(CategoriaGasto Categoria, decimal Importe);
public sealed record LibroActividad(Actividad Actividad, IReadOnlyList<FacturaRecibida> Lineas, IReadOnlyList<TotalCategoria> Totales, decimal Total, int Comunes);

/// <summary>
/// Libro de gastos por actividad (IAE). Regla de SGA: una factura comun a varias actividades
/// se imputa a la de mayor actividad (la marcada como principal).
/// </summary>
public static class LibroGastos
{
    public static Actividad? Principal(IEnumerable<Actividad> actividades) =>
        actividades.FirstOrDefault(a => a.Principal) ?? actividades.FirstOrDefault();

    public static int? Imputar(FacturaRecibida f, IReadOnlyList<Actividad> actividades)
    {
        if (actividades.Count == 0) return null;
        if (f.Comun || f.ActividadId is null) return Principal(actividades)?.Id;
        return actividades.Any(a => a.Id == f.ActividadId) ? f.ActividadId : Principal(actividades)?.Id;
    }

    public static IReadOnlyList<LibroActividad> PorActividad(IEnumerable<FacturaRecibida> facturas, IReadOnlyList<Actividad> actividades)
    {
        var lista = facturas.ToList();
        return actividades.Select(a =>
        {
            var lineas = lista.Where(f => Imputar(f, actividades) == a.Id).OrderBy(f => f.Fecha).ToList();
            var totales = Enum.GetValues<CategoriaGasto>().Select(c => new TotalCategoria(c, lineas.Where(l => l.Categoria == c).Sum(l => l.Base))).Where(t => t.Importe != 0).ToList();
            return new LibroActividad(a, lineas, totales, lineas.Sum(l => l.Base), lineas.Count(l => l.Comun));
        }).ToList();
    }
}
