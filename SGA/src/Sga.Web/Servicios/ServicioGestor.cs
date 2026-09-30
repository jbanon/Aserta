using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Servicios;

public sealed record CeldaMatriz(string Modelo, Obligacion? Obligacion)
{
    public EstadoObligacion Estado => Obligacion?.Estado ?? EstadoObligacion.NoProcede;
}
public sealed record FilaMatriz(Cliente Cliente, IReadOnlyList<CeldaMatriz> Celdas, int Presentadas, int Aplicables, int DocsRecibidos, int DocsEsperados, int IncidenciasAbiertas)
{
    public bool Completa => Aplicables > 0 && Presentadas == Aplicables;
}
public sealed record Matriz(short Ejercicio, string Periodo, IReadOnlyList<string> Columnas, IReadOnlyList<FilaMatriz> Filas, int Presentadas, int Aplicables, IReadOnlyDictionary<string, int> PorGestor)
{
    public int Avance => Aplicables == 0 ? 0 : (int)Math.Round(100.0 * Presentadas / Aplicables);
}

public sealed record TareaHoy(string Tipo, string Titulo, string Detalle, string Enlace, string Nivel);

/// <summary>La mesa de trabajo del gestor: la matriz que sustituye al Excel, y "hoy: que me toca".</summary>
public sealed class ServicioGestor(SgaDb db, IReloj reloj)
{
    public static readonly IReadOnlyList<string> Columnas = ["303", "349", "130", "111", "115", "123", "202", "LIBROS", "CCAA"];

    public async Task<Matriz> MatrizAsync(short ejercicio, string periodo, int? gestorId = null, TipoCliente? tipo = null, string? modelo = null, CancellationToken ct = default)
    {
        var clientes = await db.Clientes.AsNoTracking().Include(c => c.Gestor).Where(c => tipo == null || c.Tipo == tipo).OrderBy(c => c.Tipo).ThenBy(c => c.NombreCorto).ToListAsync(ct);
        // Las celdas de la matriz: trimestrales del periodo pedido; anuales (AN) y pagos fraccionados se muestran en su propio periodo
        var periodos = PeriodosVisibles(periodo);
        // Los anuales (Libros, CC.AA., 390, 200...) que se presentan durante el ejercicio corresponden al ejercicio anterior: el Excel los lleva en la hoja del ano en curso
        short anterior = (short)(ejercicio - 1);
        var obligaciones = await db.Obligaciones.AsNoTracking().Include(o => o.Gestor).Where(o => (o.Ejercicio == ejercicio && periodos.Contains(o.Periodo) && o.Periodo != "AN") || (o.Ejercicio == anterior && o.Periodo == "AN")).ToListAsync(ct);
        var docs = await db.Documentos.AsNoTracking().Where(d => d.Ejercicio == ejercicio && d.Periodo == periodo).ToListAsync(ct);
        var incidencias = await db.Incidencias.AsNoTracking().Where(i => i.Estado != EstadoIncidencia.Resuelta).GroupBy(i => i.ClienteId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
        var filas = new List<FilaMatriz>();
        foreach (var c in clientes)
        {
            var celdas = Columnas.Select(col => new CeldaMatriz(col, obligaciones.Where(o => o.ClienteId == c.Id && o.Modelo == col).OrderByDescending(o => o.Periodo == periodo).FirstOrDefault())).ToList();
            var aplicables = celdas.Where(x => x.Estado != EstadoObligacion.NoProcede).ToList();
            if (gestorId is not null && !(c.GestorId == gestorId || aplicables.Any(x => x.Obligacion?.GestorId == gestorId))) continue;
            if (modelo is not null && !aplicables.Any(x => x.Modelo == modelo)) continue;
            var req = Sga.Nucleo.Calculo.RequisitosDocumentales.DelTrimestre(c, docs.Where(d => d.ClienteId == c.Id));
            filas.Add(new FilaMatriz(c, celdas, aplicables.Count(x => x.Estado == EstadoObligacion.Presentado), aplicables.Count, req.Sum(r => Math.Min(r.Recibidos, r.Esperados)), req.Sum(r => r.Esperados), incidencias.GetValueOrDefault(c.Id)));
        }
        var porGestor = obligaciones.Where(o => o.Estado is EstadoObligacion.EnCurso or EstadoObligacion.Presentado && o.Gestor != null).GroupBy(o => o.Gestor!.Nombre).ToDictionary(g => g.Key, g => g.Count());
        return new Matriz(ejercicio, periodo, Columnas, filas, filas.Sum(f => f.Presentadas), filas.Sum(f => f.Aplicables), porGestor);
    }

    /// <summary>Que periodos entran en la vista de un trimestre: el trimestre, el pago fraccionado del 202 que vence en el, y los anuales que se presentan en el.</summary>
    public static string[] PeriodosVisibles(string periodo) => periodo switch
    {
        "1T" => ["1T", "AN"],           // enero-abril: 390, 190, 347 (del ejercicio anterior) y Libros
        "2T" => ["2T", "1P"],           // abril: primer pago fraccionado
        "3T" => ["3T", "2P", "AN"],     // julio: 200, CC.AA.; octubre: segundo pago fraccionado
        "4T" => ["4T", "3P"],
        _ => [periodo],
    };

    public async Task<List<TareaHoy>> HoyAsync(int gestorId, CancellationToken ct = default)
    {
        var hoy = reloj.Hoy;
        var tareas = new List<TareaHoy>();
        var (ej, per) = CalendarioFiscal.TrimestreEnCampana(hoy);
        var mias = await db.Obligaciones.AsNoTracking().Include(o => o.Cliente).Where(o => o.Ejercicio == ej && (o.Periodo == per || o.Periodo == "2P") && o.Estado != EstadoObligacion.NoProcede && o.Estado != EstadoObligacion.Presentado && (o.GestorId == gestorId || (o.GestorId == null && o.Cliente!.GestorId == gestorId))).ToListAsync(ct);
        foreach (var o in mias.OrderBy(o => o.Estado).ThenBy(o => o.Cliente!.NombreCorto))
        {
            var plazo = CalendarioFiscal.Buscar(o.Modelo, o.Ejercicio, o.Periodo);
            int dias = plazo is null ? 99 : plazo.Fin.DayNumber - hoy.DayNumber;
            tareas.Add(new("modelo", $"{o.Modelo} {o.Periodo} · {o.Cliente!.NombreCorto}", o.Estado == EstadoObligacion.EnCurso ? $"En curso · vence en {dias} días" : $"Pendiente de empezar · vence en {dias} días", $"/gestor/clientes/{o.ClienteId}?modelo={o.Modelo}&periodo={o.Periodo}", dias <= 5 ? "rojo" : dias <= 10 ? "ambar" : "neutro"));
        }
        var incidencias = await db.Incidencias.AsNoTracking().Include(i => i.Cliente).Where(i => i.Estado != EstadoIncidencia.Resuelta && (i.GestorId == gestorId || i.Cliente!.GestorId == gestorId)).OrderBy(i => i.CreadaUtc).ToListAsync(ct);
        foreach (var i in incidencias)
            tareas.Add(new("incidencia", i.Titulo, $"{i.Tipo.Texto()} · {i.Cliente!.NombreCorto} · {Infraestructura.Formato.Relativa(i.CreadaUtc, reloj.AhoraUtc)}", $"/gestor/incidencias/{i.Id}", i.Tipo == TipoIncidencia.Fraccionamiento ? "ambar" : "neutro"));
        var docs = await db.Documentos.AsNoTracking().Where(d => d.Estado == EstadoDocumento.Recibido && db.Clientes.Any(c => c.Id == d.ClienteId && c.GestorId == gestorId)).GroupBy(d => d.ClienteId).Select(g => new { g.Key, N = g.Count() }).ToListAsync(ct);
        var nombres = await db.Clientes.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.NombreCorto, ct);
        foreach (var d in docs)
            tareas.Add(new("documentos", $"{d.N} documentos por revisar · {nombres.GetValueOrDefault(d.Key)}", "Subidos desde el móvil del cliente", $"/gestor/clientes/{d.Key}?pestana=documentos", "neutro"));
        return tareas;
    }
}
