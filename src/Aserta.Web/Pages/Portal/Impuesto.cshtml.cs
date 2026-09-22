using Aserta.Aplicacion.Mensajeria;
using Aserta.Aplicacion.PortalCliente;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Mensajeria;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Portal;

[Authorize(Policy = Politicas.Cliente)]
public class ImpuestoModel : PaginaBase
{
    private readonly ServicioPortal _portal;
    private readonly ServicioMensajeria _mensajeria;
    private readonly IAsertaDb _db;
    private readonly IContextoUsuarioActual _usuario;

    public ImpuestoModel(ServicioPortal portal, ServicioMensajeria mensajeria, IAsertaDb db, IContextoUsuarioActual usuario)
    {
        _portal = portal;
        _mensajeria = mensajeria;
        _db = db;
        _usuario = usuario;
    }

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public string? Motivo { get; set; }
    public Obligacion Obligacion { get; private set; } = null!;
    public ModeloTributario? Modelo { get; private set; }
    public List<Hilo> Hilos { get; private set; } = [];
    public bool PuedeAprobar => _usuario.TieneRol(Roles.ClienteAdmin);

    public async Task<IActionResult> OnGetAsync()
    {
        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAprobarAsync()
    {
        var ok = await IntentarAsync(() => _portal.AprobarBorradorAsync(Id));
        if (ok) { AvisoOk = "¡Gracias! Conformidad registrada. Tu gestoría presentará el impuesto."; return RedirectToPage(new { id = Id }); }
        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRechazarAsync()
    {
        var ok = await IntentarAsync(() => _portal.RechazarBorradorAsync(Id, Motivo ?? ""));
        if (ok) { AvisoOk = "Enviado. Tu gestoría revisará el borrador."; return RedirectToPage(new { id = Id }); }
        await CargarAsync();
        return Page();
    }

    private async Task CargarAsync()
    {
        Obligacion = await _portal.ObligacionAsync(Id);
        Modelo = await _db.ModelosTributarios.AsNoTracking().FirstOrDefaultAsync(m => m.Codigo == Obligacion.ModeloCodigo);
        Hilos = await _mensajeria.DeObligacionAsync(Id);
    }

    public string EventoLlano(ObligacionHistorial h) => h.TipoEvento switch
    {
        TiposEventoHistorial.Generada => "Impuesto previsto en tu calendario",
        TiposEventoHistorial.CambioEstado => IndexModel.EstadoLlano(h.EstadoNuevo ?? EstadoObligacion.PendienteDocumentacion),
        TiposEventoHistorial.AprobacionCliente => "Diste tu conformidad",
        TiposEventoHistorial.DocumentoVinculado => "Documento recibido",
        TiposEventoHistorial.Mensaje => "Mensaje",
        TiposEventoHistorial.BorradorRegistrado => "Importe calculado",
        TiposEventoHistorial.JustificanteArchivado => "Justificante archivado",
        _ => h.TipoEvento
    };
}
