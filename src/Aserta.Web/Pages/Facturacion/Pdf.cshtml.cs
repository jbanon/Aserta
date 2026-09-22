using Aserta.Aplicacion.Puertos;
using Aserta.Verifactu.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Facturacion;

/// <summary>Sirve el PDF de la factura (generandolo en linea si la cola aun no lo hizo). Accesible para gestoria y para el cliente emisor.</summary>
public class PdfModel : PageModel
{
    private readonly ServicioPdfFactura _pdf;
    private readonly IAsertaDb _db;
    private readonly IContextoUsuarioActual _usuario;

    public PdfModel(ServicioPdfFactura pdf, IAsertaDb db, IContextoUsuarioActual usuario)
    {
        _pdf = pdf;
        _db = db;
        _usuario = usuario;
    }

    public async Task<IActionResult> OnGetAsync(Guid id, bool descargar = false)
    {
        if (!_usuario.EstaAutenticado) return Challenge();
        var f = await _db.FacturasEmitidas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (f is null) return NotFound();
        if (_usuario.ClienteId is Guid propio && propio != f.ClienteEmisorId) return Forbid();
        var bytes = await _pdf.LeerAsync(id);
        if (bytes is null)
        {
            try { await _pdf.GenerarSiFaltaAsync(id); bytes = await _pdf.LeerAsync(id); }
            catch (Exception ex) { return Content("No se pudo generar el PDF: " + ex.Message); }
        }
        if (bytes is null) return NotFound();
        var nombre = $"{f.NumSerieFactura}.pdf";
        if (!descargar) Response.Headers.ContentDisposition = $"inline; filename*=UTF-8''{Uri.EscapeDataString(nombre)}";
        return descargar ? File(bytes, "application/pdf", nombre) : File(bytes, "application/pdf");
    }
}
