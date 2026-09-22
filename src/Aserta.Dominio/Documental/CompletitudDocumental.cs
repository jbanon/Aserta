using Aserta.Dominio.Catalogo;

namespace Aserta.Dominio.Documental;

/// <summary>Un requisito con lo recibido: la unidad de "que me falta".</summary>
public sealed record EstadoRequisito(RequisitoPeriodo Requisito, int Recibidos, int Validados, int Rechazados)
{
    public int Esperados => Requisito.CantidadEsperada ?? 1;
    public int Faltan => Math.Max(0, Esperados - Recibidos);
    public bool Completo => Faltan == 0;
    public string Frase => Faltan == 0
        ? $"{Requisito.TipoDocumento.Plural(Recibidos)}: recibido"
        : $"Te falta{(Faltan == 1 ? "" : "n")} {Faltan} {Requisito.TipoDocumento.Plural(Faltan)} de {NombrePeriodo}";
    public string NombrePeriodo => Periodo.Etiqueta(Requisito.Ejercicio, Requisito.Periodo).Replace($" {Requisito.Ejercicio}", "").Replace($"Anual", $"{Requisito.Ejercicio}").ToLowerInvariant();
}

/// <summary>
/// Cruza requisitos con documentos y produce las frases del portal ("te faltan 2
/// facturas de julio") y la decision de "documentacion completa" de un periodo.
/// Logica pura, probada en Aserta.Dominio.Tests.
/// </summary>
public static class CompletitudDocumental
{
    public static List<EstadoRequisito> Evaluar(IEnumerable<RequisitoPeriodo> requisitos, IEnumerable<Documento> documentos)
    {
        var docs = documentos.ToList();
        return requisitos.Where(r => !r.NoAplica)
            .Select(r =>
            {
                var propios = docs.Where(d => d.Ejercicio == r.Ejercicio && d.Periodo == r.Periodo && d.Tipo == r.TipoDocumento).ToList();
                return new EstadoRequisito(r, propios.Count(d => d.CuentaParaRequisitos), propios.Count(d => d.Estado == EstadoDocumento.Validado), propios.Count(d => d.Estado == EstadoDocumento.Rechazado));
            })
            .OrderBy(e => e.Requisito.Ejercicio).ThenBy(e => Periodo.Orden(e.Requisito.Periodo)).ThenBy(e => e.Requisito.TipoDocumento)
            .ToList();
    }

    /// <summary>Un trimestre esta documentalmente completo cuando todos los requisitos OBLIGATORIOS de sus meses (y del propio trimestre) estan cubiertos.</summary>
    public static bool PeriodoCompleto(IReadOnlyList<EstadoRequisito> estados, int ejercicio, string periodoObligacion)
    {
        var (inicio, fin) = Periodo.Rango(ejercicio, periodoObligacion);
        var afectados = estados.Where(e => e.Requisito.Ejercicio == ejercicio && e.Requisito.Obligatorio && PeriodoDentro(e.Requisito.Periodo, ejercicio, inicio, fin)).ToList();
        return afectados.Count > 0 && afectados.All(e => e.Completo);
    }

    public static IReadOnlyList<EstadoRequisito> Pendientes(IReadOnlyList<EstadoRequisito> estados) => estados.Where(e => !e.Completo).ToList();

    public static int TotalFaltantes(IReadOnlyList<EstadoRequisito> estados) => estados.Where(e => e.Requisito.Obligatorio).Sum(e => e.Faltan);

    private static bool PeriodoDentro(string periodoRequisito, int ejercicio, DateOnly inicio, DateOnly fin)
    {
        var (ri, rf) = Periodo.Rango(ejercicio, periodoRequisito);
        return ri >= inicio && rf <= fin;
    }
}
