using System.Xml.Linq;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Facturacion;
using Aserta.Verifactu.Huella;
using Aserta.Verifactu.Qr;
using Aserta.Verifactu.Servicios;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Facturacion;

public class FacturaModel : PaginaBase
{
    private readonly IAsertaDb _db;
    private readonly ServicioEmision _emision;
    private readonly IContextoUsuarioActual _usuario;

    public FacturaModel(IAsertaDb db, ServicioEmision emision, IContextoUsuarioActual usuario)
    {
        _db = db;
        _emision = emision;
        _usuario = usuario;
    }

    public sealed record RegistroVista(RegistroFacturacion Registro, string HuellaRecalculada, string XmlBonito);
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public FacturaEmitida Factura { get; private set; } = null!;
    public string NombreEmisor { get; private set; } = "";
    public string NombreUsuario { get; private set; } = "";
    public string? NumRectificada { get; private set; }
    public List<RegistroVista> Registros { get; private set; } = [];
    public Dictionary<long, EstadoEnvioRegistro> Estados { get; private set; } = [];
    public bool Anulada { get; private set; }
    public bool PdfListo { get; private set; }
    public string QrDataUri { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        var f = await _db.FacturasEmitidas.AsNoTracking().Include(x => x.Lineas).FirstOrDefaultAsync(x => x.Id == Id);
        if (f is null) return NotFound();
        Factura = f;
        NombreEmisor = await _db.Clientes.AsNoTracking().Where(c => c.Id == f.ClienteEmisorId).Select(c => c.RazonSocial).FirstAsync();
        NombreUsuario = await _db.Usuarios.AsNoTracking().Where(u => u.Id == f.UsuarioId).Select(u => u.NombreCompleto).FirstOrDefaultAsync() ?? "—";
        if (f.FacturaRectificadaId is Guid r) NumRectificada = await _db.FacturasEmitidas.AsNoTracking().Where(x => x.Id == r).Select(x => x.NumSerieFactura).FirstOrDefaultAsync();
        var registros = await _db.RegistrosFacturacion.AsNoTracking().Where(x => x.FacturaEmitidaId == Id).OrderBy(x => x.NumeroEnCadena).ToListAsync();
        Registros = registros.Select(x => new RegistroVista(x, CalculadoraHuella.CalcularHuella(x.CadenaHuella), Formatear(x.XmlRegistro))).ToList();
        Estados = await _db.EstadosEnvio.AsNoTracking().Where(e => registros.Select(x => x.Id).Contains(e.RegistroFacturacionId)).ToDictionaryAsync(e => e.RegistroFacturacionId);
        Anulada = registros.Any(x => x.Tipo == TipoRegistro.ANULACION);
        PdfListo = await _db.FacturasPdf.AsNoTracking().AnyAsync(p => p.FacturaEmitidaId == Id && p.ClaveAlmacen != null);
        QrDataUri = GeneradorQrVerifactu.DataUriPng(f.UrlQr, 5);
        return Page();
    }

    public async Task<IActionResult> OnPostAnularAsync()
    {
        try
        {
            var r = await _emision.AnularAsync(Id, _usuario.UsuarioId!.Value);
            AvisoOk = $"Registro de anulación generado (n.º {r.NumeroEnCadena} de la cadena, huella {r.Huella[..12]}…). La factura permanece inalterada.";
        }
        catch (ExcepcionFacturacion ex) { AvisoError = ex.Message; }
        return RedirectToPage(new { id = Id });
    }

    private static string Formatear(string xml)
    {
        try { return XDocument.Parse(xml).ToString(); } catch { return xml; }
    }
}
