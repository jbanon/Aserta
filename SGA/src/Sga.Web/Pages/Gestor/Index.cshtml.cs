using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Gestor;

public class IndexModel(SgaDb db, IReloj reloj, ServicioGestor servicio) : PaginaGestor(db)
{
    public short Ejercicio; public string Periodo = "3T"; public Plazo? Plazo; public int DiasPlazo;
    public Matriz Matriz { get; private set; } = default!;
    public List<TareaHoy> Tareas { get; private set; } = [];
    public List<Plazo> Proximos { get; private set; } = [];
    public int DocsPorRevisar { get; private set; }

    public async Task OnGetAsync()
    {
        (Ejercicio, Periodo) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        Plazo = CalendarioFiscal.Buscar("303", Ejercicio, Periodo);
        DiasPlazo = Plazo is null ? 99 : Plazo.Fin.DayNumber - reloj.Hoy.DayNumber;
        Matriz = await servicio.MatrizAsync(Ejercicio, Periodo);
        Tareas = await servicio.HoyAsync(GestorId);
        Proximos = CalendarioFiscal.Plazos.Where(p => p.Fin >= reloj.Hoy && p.Modelo is "303" or "111" or "115" or "130" or "202" or "349" or "390" or "347" or "190" or "200" or "CCAA" or "LIBROS").GroupBy(p => (p.Fin, p.Modelo)).Select(g => g.First()).OrderBy(p => p.Fin).Take(6).ToList();
        DocsPorRevisar = await Db.Documentos.CountAsync(d => d.Estado == EstadoDocumento.Recibido);
    }
}
