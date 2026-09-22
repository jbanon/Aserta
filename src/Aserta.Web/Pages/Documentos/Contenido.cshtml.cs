using Aserta.Aplicacion.Documental;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Aserta.Web.Pages.Documentos;

/// <summary>Sirve el binario descifrado (imagen o PDF en linea; el resto como descarga). El servicio comprueba el tenant y el cliente.</summary>
public class ContenidoModel : PageModel
{
    private readonly ServicioDocumentos _documentos;
    public ContenidoModel(ServicioDocumentos documentos) => _documentos = documentos;

    public async Task<IActionResult> OnGetAsync(Guid id, bool descargar = false)
    {
        var (contenido, doc) = await _documentos.AbrirAsync(id);
        Response.Headers.CacheControl = "private, max-age=300";
        bool enLinea = !descargar && (doc.EsImagen || doc.EsPdf);
        if (enLinea) Response.Headers.ContentDisposition = $"inline; filename*=UTF-8''{Uri.EscapeDataString(doc.NombreOriginal)}";
        return enLinea ? File(contenido, doc.TipoMime) : File(contenido, doc.TipoMime, doc.NombreOriginal);
    }
}
