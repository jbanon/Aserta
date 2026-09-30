using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Cliente;

public class FacturaPdfModel(SgaDb db, GeneradorPdf pdf) : PaginaCliente(db)
{
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var f = await Db.FacturasEmitidas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ClienteId == ClienteId);
        if (f is null) return NotFound();
        var contrato = f.ContratoId is int cid ? await Db.Contratos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cid) : null;
        return File(pdf.FacturaAlquiler(f, Cliente, contrato), "application/pdf", $"factura-{f.Numero.Replace('/', '-')}.pdf");
    }
}
