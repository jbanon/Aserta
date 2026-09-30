using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Pages.Gestor;

public class ConciliacionModel(SgaDb db, IReloj reloj) : PaginaGestor(db)
{
    public Sga.Nucleo.Modelo.Cliente Cliente { get; private set; } = default!;
    public short Ejercicio; public string Periodo = "3T";
    public List<MovimientoBancario> Movimientos { get; private set; } = [];
    public int Pendientes => Movimientos.Count(m => m.Estado == EstadoConciliacion.Pendiente);
    public bool IncidenciaAbierta;
    private Dictionary<int, FacturaEmitida> _emitidas = []; private Dictionary<int, FacturaRecibida> _recibidas = [];

    private async Task<bool> CargarAsync(int id)
    {
        var c = await Db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return false;
        Cliente = c; (Ejercicio, Periodo) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        var (ini, fin) = Aserta.Dominio.Catalogo.Periodo.Rango(Ejercicio, Periodo);
        Movimientos = await Db.Movimientos.AsNoTracking().Where(m => m.ClienteId == id && m.Fecha >= ini && m.Fecha <= fin.AddDays(15)).OrderBy(m => m.Fecha).ToListAsync();
        _emitidas = await Db.FacturasEmitidas.AsNoTracking().Where(f => f.ClienteId == id && f.Fecha.Year == Ejercicio).ToDictionaryAsync(f => f.Id);
        _recibidas = await Db.FacturasRecibidas.AsNoTracking().Where(f => f.ClienteId == id && f.Fecha.Year == Ejercicio).ToDictionaryAsync(f => f.Id);
        IncidenciaAbierta = await Db.Incidencias.AnyAsync(i => i.ClienteId == id && i.Tipo == TipoIncidencia.Conciliacion && i.Estado != EstadoIncidencia.Resuelta);
        return true;
    }

    public string Referencia(MovimientoBancario m) => m.FacturaEmitidaId is int e && _emitidas.TryGetValue(e, out var fe) ? fe.Numero : m.FacturaRecibidaId is int r && _recibidas.TryGetValue(r, out var fr) ? fr.Numero + " " + fr.ProveedorNombre : "conciliado";

    /// <summary>Sugerencia por importe exacto (y no usada ya por otro movimiento).</summary>
    public (string Texto, int? EmitidaId, int? RecibidaId)? Sugerencia(MovimientoBancario m)
    {
        var usadasE = Movimientos.Where(x => x.FacturaEmitidaId != null).Select(x => x.FacturaEmitidaId!.Value).ToHashSet();
        var usadasR = Movimientos.Where(x => x.FacturaRecibidaId != null).Select(x => x.FacturaRecibidaId!.Value).ToHashSet();
        // 1) el numero de factura aparece en el concepto; 2) mismo importe y fecha cercana (60 dias antes del movimiento)
        if (m.Importe > 0)
        {
            var f = _emitidas.Values.Where(f => !usadasE.Contains(f.Id)).FirstOrDefault(f => m.Concepto.Contains(f.Numero, StringComparison.OrdinalIgnoreCase))
                ?? _emitidas.Values.Where(f => !usadasE.Contains(f.Id) && f.Total == m.Importe && f.Fecha <= m.Fecha && f.Fecha >= m.Fecha.AddDays(-60)).OrderByDescending(f => f.Fecha).FirstOrDefault();
            return f is null ? null : ($"{f.Numero} · {f.DestinatarioNombre}", f.Id, null);
        }
        var r = _recibidas.Values.Where(f => !usadasR.Contains(f.Id)).FirstOrDefault(f => f.Numero.Length > 2 && m.Concepto.Contains(f.Numero, StringComparison.OrdinalIgnoreCase))
            ?? _recibidas.Values.Where(f => !usadasR.Contains(f.Id) && f.Total == -m.Importe && f.Fecha <= m.Fecha && f.Fecha >= m.Fecha.AddDays(-60)).OrderByDescending(f => f.Fecha).FirstOrDefault();
        return r is null ? null : ($"{r.Numero} · {r.ProveedorNombre}", null, r.Id);
    }

    public async Task<IActionResult> OnGetAsync(int id) => await CargarAsync(id) ? Page() : NotFound();

    public async Task<IActionResult> OnPostConciliarAsync(int id, int movimientoId, int? emitidaId, int? recibidaId)
    {
        var m = await Db.Movimientos.FirstOrDefaultAsync(x => x.Id == movimientoId && x.ClienteId == id);
        if (m is null) return NotFound();
        m.FacturaEmitidaId = emitidaId; m.FacturaRecibidaId = recibidaId; m.Estado = EstadoConciliacion.Conciliado;
        await Db.SaveChangesAsync();
        return Redirect($"/gestor/clientes/{id}/conciliacion");
    }

    public async Task<IActionResult> OnPostIgnorarAsync(int id, int movimientoId)
    {
        var m = await Db.Movimientos.FirstOrDefaultAsync(x => x.Id == movimientoId && x.ClienteId == id);
        if (m is null) return NotFound();
        m.Estado = EstadoConciliacion.Ignorado; await Db.SaveChangesAsync();
        return Redirect($"/gestor/clientes/{id}/conciliacion");
    }

    public async Task<IActionResult> OnPostPreguntarAsync(int id)
    {
        if (!await CargarAsync(id)) return NotFound();
        var pendientes = Movimientos.Where(m => m.Estado == EstadoConciliacion.Pendiente).ToList();
        if (pendientes.Count == 0 || IncidenciaAbierta) return Redirect($"/gestor/clientes/{id}/conciliacion");
        var inc = new Incidencia { ClienteId = id, Tipo = TipoIncidencia.Conciliacion, Titulo = $"{pendientes.Count} movimientos del extracto sin cuadrar", CreadaUtc = reloj.AhoraUtc, GestorId = GestorId };
        inc.Mensajes.Add(new Mensaje { Autor = Gestor.Nombre, EsGestor = true, FechaUtc = reloj.AhoraUtc, LeidoPorGestor = true, Texto = "Hola. Del extracto del trimestre no encuentro factura para: " + string.Join("; ", pendientes.Select(p => $"{Infraestructura.Formato.FechaCorta(p.Fecha)} {p.Concepto} ({Infraestructura.Formato.Euros(p.Importe)})")) + ". ¿Me decís a qué corresponden?" });
        Db.Incidencias.Add(inc);
        Db.Avisos.Add(new Aviso { ClienteId = id, FechaUtc = reloj.AhoraUtc, Texto = $"{Gestor.Nombre.Split(' ')[0]} necesita que confirméis {pendientes.Count} movimientos del banco.", Enlace = "/cliente/mensajes" });
        await Db.SaveChangesAsync();
        AvisoOk = "Pregunta enviada al cliente.";
        return Redirect($"/gestor/clientes/{id}/conciliacion");
    }
}
