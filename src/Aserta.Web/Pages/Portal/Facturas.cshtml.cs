using Aserta.Aplicacion.PortalCliente;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Facturacion;
using Aserta.Dominio.Nucleo;
using Aserta.Verifactu.Servicios;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Portal;

[Authorize(Policy = Politicas.Cliente)]
public class FacturasModel : PaginaBase
{
    private readonly ServicioPortal _portal;
    private readonly ServicioEmision _emision;
    private readonly IAsertaDb _db;
    private readonly IContextoUsuarioActual _usuario;

    public FacturasModel(ServicioPortal portal, ServicioEmision emision, IAsertaDb db, IContextoUsuarioActual usuario)
    {
        _portal = portal;
        _emision = emision;
        _db = db;
        _usuario = usuario;
    }

    public sealed record Fila(FacturaEmitida Factura, EstadoEnvio? Estado, bool Anulada);
    [BindProperty] public FacturaNueva Datos { get; set; } = new();
    public List<Fila> Filas { get; private set; } = [];
    public List<Destinatario> Destinatarios { get; private set; } = [];
    public bool PuedeEmitir => _usuario.TieneRol(Roles.ClienteAdmin);
    public bool MostrarFormulario { get; private set; }

    public async Task OnGetAsync() { Datos.ClienteEmisorId = _portal.ClienteId; await CargarAsync(); }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!PuedeEmitir) return Forbid();
        Datos.ClienteEmisorId = _portal.ClienteId;
        Datos.FacturaRectificadaId = null;
        Datos.Lineas = Datos.Lineas.Where(l => !string.IsNullOrWhiteSpace(l.Descripcion) || l.PrecioUnitario != 0).ToList();
        try
        {
            var r = await _emision.EmitirAsync(Datos, _usuario.UsuarioId!.Value);
            AvisoOk = $"Factura {r.Factura.NumSerieFactura} emitida. Ya puedes abrir el PDF con su código QR.";
            return RedirectToPage();
        }
        catch (ExcepcionFacturacion ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        catch (ArgumentException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        MostrarFormulario = true;
        await CargarAsync();
        return Page();
    }

    private async Task CargarAsync()
    {
        var facturas = await _db.FacturasEmitidas.AsNoTracking().Where(f => f.ClienteEmisorId == _portal.ClienteId).OrderByDescending(f => f.FechaHoraCreacionUtc).Take(100).ToListAsync();
        var ids = facturas.Select(f => f.Id).ToList();
        var registros = await _db.RegistrosFacturacion.AsNoTracking().Where(r => ids.Contains(r.FacturaEmitidaId)).ToListAsync();
        var estados = await _db.EstadosEnvio.AsNoTracking().Where(e => registros.Select(r => r.Id).Contains(e.RegistroFacturacionId)).ToDictionaryAsync(e => e.RegistroFacturacionId);
        Filas = facturas.Select(f =>
        {
            var alta = registros.FirstOrDefault(r => r.FacturaEmitidaId == f.Id && r.Tipo == TipoRegistro.ALTA);
            return new Fila(f, alta is not null && estados.TryGetValue(alta.Id, out var e) ? e.Estado : null, registros.Any(r => r.FacturaEmitidaId == f.Id && r.Tipo == TipoRegistro.ANULACION));
        }).ToList();
        Destinatarios = await _db.Destinatarios.AsNoTracking().Where(d => d.ClienteEmisorId == _portal.ClienteId && d.Activo).ToListAsync();
    }
}
