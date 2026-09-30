using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Calendario;
using Sga.Web.Datos;

namespace Sga.Web.Pages.Gestor;

public class GastosModel(SgaDb db, IReloj reloj) : PaginaGestor(db)
{
    public Sga.Nucleo.Modelo.Cliente Cliente { get; private set; } = default!;
    public short Ejercicio;
    public IReadOnlyList<LibroActividad> Libros { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var c = await Db.Clientes.AsNoTracking().Include(x => x.Actividades).FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        Cliente = c; Ejercicio = (short)reloj.Hoy.Year;
        var facturas = await Db.FacturasRecibidas.AsNoTracking().Where(f => f.ClienteId == id && f.Fecha.Year == Ejercicio).ToListAsync();
        Libros = LibroGastos.PorActividad(facturas, c.Actividades);
        return Page();
    }
}
