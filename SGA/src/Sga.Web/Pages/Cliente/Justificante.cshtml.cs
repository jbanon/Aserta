using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Cliente;

public class JustificanteModel(SgaDb db, GeneradorPdf pdf, ServicioIva iva) : PaginaCliente(db)
{
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var p = await Db.Presentaciones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        var o = await Db.Obligaciones.AsNoTracking().FirstAsync(x => x.Id == p.ObligacionId);
        if (o.ClienteId != ClienteId) return NotFound();
        var r = o.Modelo == "303" ? await iva.LiquidarAsync(ClienteId, o.Ejercicio, o.Periodo) : null;
        return File(pdf.Justificante(p, o, Cliente, r), "application/pdf", $"justificante-{o.Modelo}-{o.Periodo}-{o.Ejercicio}.pdf");
    }
}
