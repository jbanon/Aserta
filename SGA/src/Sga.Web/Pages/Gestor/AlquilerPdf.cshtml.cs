using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Gestor;

public class AlquilerPdfModel(SgaDb db, GeneradorPdf pdf) : PaginaGestor(db)
{
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var f = await Db.FacturasEmitidas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (f is null) return NotFound();
        var c = await Db.Clientes.AsNoTracking().FirstAsync(x => x.Id == f.ClienteId);
        var k = f.ContratoId is int cid ? await Db.Contratos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cid) : null;
        return File(pdf.FacturaAlquiler(f, c, k), "application/pdf", $"factura-{f.Numero.Replace('/', '-')}.pdf");
    }
}
