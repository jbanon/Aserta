using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Gestor;

public class IncidenciasModel(SgaDb db, IReloj reloj, ServicioIva iva) : PaginaGestor(db)
{
    public string Filtro = "abiertas"; public int Abiertas;
    public List<Incidencia> Lista { get; private set; } = [];
    public Incidencia? Actual { get; private set; }
    public Obligacion? ObligacionFracc { get; private set; }
    public decimal? ImporteFracc { get; private set; }

    public async Task<IActionResult> OnGetAsync(int? id, string? filtro)
    {
        Abiertas = await Db.Incidencias.CountAsync(i => i.Estado != EstadoIncidencia.Resuelta);
        if (id is int i)
        {
            Actual = await Db.Incidencias.Include(x => x.Cliente).Include(x => x.Mensajes).FirstOrDefaultAsync(x => x.Id == i);
            if (Actual is null) return NotFound();
            bool cambio = false;
            foreach (var m in Actual.Mensajes.Where(m => !m.EsGestor && !m.LeidoPorGestor)) { m.LeidoPorGestor = true; cambio = true; }
            if (cambio) await Db.SaveChangesAsync();
            if (Actual.ObligacionId is int oid) { ObligacionFracc = await Db.Obligaciones.AsNoTracking().FirstOrDefaultAsync(o => o.Id == oid); if (ObligacionFracc is not null) ImporteFracc = ObligacionFracc.Resultado ?? (await iva.LiquidarAsync(ObligacionFracc.ClienteId, ObligacionFracc.Ejercicio, ObligacionFracc.Periodo)).Resultado; }
            return Page();
        }
        Filtro = filtro is "mias" or "todas" ? filtro : "abiertas";
        var q = Db.Incidencias.AsNoTracking().Include(x => x.Cliente).Include(x => x.Mensajes).AsQueryable();
        if (Filtro == "abiertas") q = q.Where(x => x.Estado != EstadoIncidencia.Resuelta);
        if (Filtro == "mias") q = q.Where(x => x.GestorId == GestorId || x.Cliente!.GestorId == GestorId);
        Lista = (await q.ToListAsync()).OrderBy(x => x.Estado == EstadoIncidencia.Resuelta).ThenByDescending(x => x.Mensajes.Count == 0 ? x.CreadaUtc : x.Mensajes.Max(m => m.FechaUtc)).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostResponderAsync(int id, string texto)
    {
        var inc = await Db.Incidencias.Include(x => x.Cliente).FirstOrDefaultAsync(x => x.Id == id);
        if (inc is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(texto))
        {
            Db.Mensajes.Add(new Mensaje { IncidenciaId = id, Autor = Gestor.Nombre, EsGestor = true, Texto = texto.Trim(), FechaUtc = reloj.AhoraUtc, LeidoPorGestor = true });
            if (inc.Estado == EstadoIncidencia.Abierta) inc.Estado = EstadoIncidencia.EnCurso;
            Db.Avisos.Add(new Aviso { ClienteId = inc.ClienteId, FechaUtc = reloj.AhoraUtc, Texto = $"{Gestor.Nombre.Split(' ')[0]} te ha contestado: «{inc.Titulo}».", Enlace = $"/cliente/mensajes/{id}" });
            await Db.SaveChangesAsync();
        }
        return Redirect($"/gestor/incidencias/{id}");
    }

    public async Task<IActionResult> OnPostResolverAsync(int id)
    {
        var inc = await Db.Incidencias.FirstOrDefaultAsync(x => x.Id == id);
        if (inc is null) return NotFound();
        inc.Estado = EstadoIncidencia.Resuelta; await Db.SaveChangesAsync();
        AvisoOk = "Incidencia resuelta.";
        return Redirect("/gestor/incidencias");
    }

    public async Task<IActionResult> OnPostFraccionarAsync(int id, bool conceder)
    {
        var inc = await Db.Incidencias.Include(x => x.Cliente).FirstOrDefaultAsync(x => x.Id == id);
        if (inc?.ObligacionId is not int oid) return NotFound();
        var o = await Db.Obligaciones.FirstAsync(x => x.Id == oid);
        o.Fraccionamiento = conceder ? EstadoFraccionamiento.Concedido : EstadoFraccionamiento.Denegado;
        var texto = conceder ? "Concedido: dividimos el pago en plazos y te confirmo las fechas de cargo en cuanto Hacienda lo admita." : "No podemos fraccionarlo esta vez; el cargo será por el importe completo el día del plazo.";
        Db.Mensajes.Add(new Mensaje { IncidenciaId = id, Autor = Gestor.Nombre, EsGestor = true, Texto = texto, FechaUtc = reloj.AhoraUtc, LeidoPorGestor = true });
        Db.Avisos.Add(new Aviso { ClienteId = inc.ClienteId, FechaUtc = reloj.AhoraUtc, Texto = conceder ? "Fraccionamiento concedido: te confirmamos las fechas de los plazos." : "No ha sido posible fraccionar el pago de este trimestre.", Enlace = $"/cliente/mensajes/{id}" });
        inc.Estado = EstadoIncidencia.Resuelta;
        await Db.SaveChangesAsync();
        AvisoOk = conceder ? "Fraccionamiento concedido y comunicado al cliente." : "Fraccionamiento denegado y comunicado al cliente.";
        return Redirect($"/gestor/incidencias/{id}");
    }
}
