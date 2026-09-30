using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

using Sga.Web.Infraestructura;

namespace Sga.Web.Pages.Cliente;

public class MensajesModel(SgaDb db, IReloj reloj) : PaginaCliente(db)
{
    public List<Incidencia> Incidencias { get; private set; } = [];
    public Incidencia? Actual { get; private set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is int i)
        {
            Actual = await Db.Incidencias.Include(x => x.Mensajes).FirstOrDefaultAsync(x => x.Id == i && x.ClienteId == ClienteId);
            if (Actual is null) return NotFound();
            bool cambio = false;
            foreach (var m in Actual.Mensajes.Where(m => m.EsGestor && !m.LeidoPorCliente)) { m.LeidoPorCliente = true; cambio = true; }
            if (cambio) await Db.SaveChangesAsync();
            return Page();
        }
        Incidencias = await Db.Incidencias.AsNoTracking().Include(x => x.Mensajes).Where(x => x.ClienteId == ClienteId && x.Mensajes.Count > 0).ToListAsync();
        Incidencias = Incidencias.OrderBy(x => x.Estado == EstadoIncidencia.Resuelta).ThenByDescending(x => x.Mensajes.Max(m => m.FechaUtc)).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostResponderAsync(int id, string texto)
    {
        var inc = await Db.Incidencias.FirstOrDefaultAsync(x => x.Id == id && x.ClienteId == ClienteId);
        if (inc is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(texto))
        {
            Db.Mensajes.Add(new Mensaje { IncidenciaId = inc.Id, Autor = User.NombreActual(), EsGestor = false, Texto = texto.Trim(), FechaUtc = reloj.AhoraUtc, LeidoPorCliente = true });
            if (inc.Estado == EstadoIncidencia.Abierta) inc.Estado = EstadoIncidencia.EnCurso;
            await Db.SaveChangesAsync();
            AvisoOk = "Mensaje enviado. Te contestamos en menos de 48 horas.";
        }
        return Redirect($"/cliente/mensajes/{id}");
    }
}
