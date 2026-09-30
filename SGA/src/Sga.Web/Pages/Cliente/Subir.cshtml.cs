using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

using Sga.Web.Infraestructura;

namespace Sga.Web.Pages.Cliente;

[RequestSizeLimit(25_000_000)]
public class SubirModel(SgaDb db, IReloj reloj, AlmacenDocumentos almacen) : PaginaCliente(db)
{
    public short Ejercicio { get; private set; }
    public string PeriodoSugerido { get; private set; } = "3T";
    public TipoDocumento TipoSugerido { get; private set; } = TipoDocumento.Ticket;
    public IReadOnlyList<TipoDocumento> Tipos => Cliente.Tipo == TipoCliente.Sociedad
        ? [TipoDocumento.FacturaEmitida, TipoDocumento.FacturaRecibida, TipoDocumento.Nomina, TipoDocumento.ExtractoBancario, TipoDocumento.Otro]
        : Cliente.Emisor == QuienEmite.Cliente ? [TipoDocumento.Ticket, TipoDocumento.FacturaRecibida, TipoDocumento.FacturaEmitida, TipoDocumento.SeguroOCuota, TipoDocumento.ExtractoBancario, TipoDocumento.Nomina, TipoDocumento.Otro]
        : [TipoDocumento.Ticket, TipoDocumento.FacturaRecibida, TipoDocumento.SeguroOCuota, TipoDocumento.ExtractoBancario, TipoDocumento.Nomina, TipoDocumento.Otro];
    public Documento? Subido { get; private set; }
    public string Frase { get; private set; } = "";
    [BindProperty] public IFormFile? Fichero { get; set; }

    public void OnGet()
    {
        var (ej, per) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        Ejercicio = ej; PeriodoSugerido = per;
        if (Cliente.Tipo == TipoCliente.Sociedad) TipoSugerido = TipoDocumento.FacturaRecibida;
    }

    public async Task<IActionResult> OnPostAsync(TipoDocumento tipo, string periodo, short ejercicio, string? nota)
    {
        OnGet();
        if (Fichero is null || Fichero.Length == 0) { ModelState.AddModelError("Fichero", "Elige una foto o un fichero."); return Page(); }
        if (Fichero.Length > 20_000_000) { ModelState.AddModelError("Fichero", "El fichero supera los 20 MB."); return Page(); }
        var mime = Fichero.ContentType.ToLowerInvariant();
        if (!(mime.StartsWith("image/") || mime == "application/pdf")) { ModelState.AddModelError("Fichero", "Solo se admiten imágenes o PDF."); return Page(); }
        if (periodo is not ("1T" or "2T" or "3T" or "4T")) periodo = PeriodoSugerido;
        await using var s = Fichero.OpenReadStream();
        var ruta = await almacen.GuardarAsync(ClienteId, Fichero.FileName, s);
        var d = new Documento { ClienteId = ClienteId, Tipo = tipo, Nombre = Fichero.FileName, Ruta = ruta, TipoMime = mime, Tamano = Fichero.Length, SubidoUtc = reloj.AhoraUtc, SubidoPor = User.NombreActual(), Ejercicio = ejercicio == 0 ? Ejercicio : ejercicio, Periodo = periodo, Estado = EstadoDocumento.Recibido, Nota = nota };
        Db.Documentos.Add(d);
        await Db.SaveChangesAsync();
        Subido = d;
        var docs = await Db.Documentos.AsNoTracking().Where(x => x.ClienteId == ClienteId && x.Ejercicio == d.Ejercicio && x.Periodo == d.Periodo).ToListAsync();
        var req = RequisitosDocumentales.DelTrimestre(Cliente, docs);
        Frase = req.Count == 0 ? "Gracias, lo revisamos en breve." : RequisitosDocumentales.Frase(req);
        return Page();
    }
}
