using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Gestor;

public class JustificanteModel(SgaDb db, GeneradorPdf pdf, ServicioIva iva) : PaginaGestor(db)
{
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var p = await Db.Presentaciones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        var o = await Db.Obligaciones.AsNoTracking().Include(x => x.Cliente).FirstAsync(x => x.Id == p.ObligacionId);
        var r = o.Modelo == "303" ? await iva.LiquidarAsync(o.ClienteId, o.Ejercicio, o.Periodo) : null;
        return File(pdf.Justificante(p, o, o.Cliente!, r), "application/pdf", $"justificante-{o.Modelo}-{o.Periodo}-{o.Ejercicio}-{o.Cliente!.Nif}.pdf");
    }
}
