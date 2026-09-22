using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Facturacion;
using Aserta.Verifactu.Servicios;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Facturacion;

public class NuevaModel : PaginaBase
{
    private readonly ServicioEmision _emision;
    private readonly ServicioClientes _clientes;
    private readonly IAsertaDb _db;
    private readonly IContextoUsuarioActual _usuario;

    public NuevaModel(ServicioEmision emision, ServicioClientes clientes, IAsertaDb db, IContextoUsuarioActual usuario)
    {
        _emision = emision;
        _clientes = clientes;
        _db = db;
        _usuario = usuario;
    }

    [BindProperty] public FacturaNueva Datos { get; set; } = new();
    [BindProperty(SupportsGet = true)] public Guid? EmisorId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? Rectifica { get; set; }
    public string NombreEmisor { get; private set; } = "";
    public string NifEmisor { get; private set; } = "";
    public string? NumRectificada { get; private set; }
    public List<Destinatario> Destinatarios { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (Rectifica is Guid r)
        {
            var original = await _db.FacturasEmitidas.AsNoTracking().FirstOrDefaultAsync(f => f.Id == r);
            if (original is null) return NotFound();
            EmisorId = original.ClienteEmisorId;
            Datos = new FacturaNueva { ClienteEmisorId = original.ClienteEmisorId, FacturaRectificadaId = r, Simplificada = original.TipoFactura == TipoFactura.F2, DestinatarioNif = original.DestinatarioNif, DestinatarioNombre = original.DestinatarioNombre, Descripcion = "Rectificación de " + original.NumSerieFactura, PorcentajeRetencion = original.PorcentajeRetencion, Motivo = MotivoRectificacion.ErrorEnDatos };
            NumRectificada = original.NumSerieFactura;
        }
        else
        {
            if (EmisorId is null) { AvisoError = "Elija el emisor en el listado."; return RedirectToPage("/Facturacion/Index"); }
            Datos.ClienteEmisorId = EmisorId.Value;
        }
        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        EmisorId = Datos.ClienteEmisorId;
        Datos.Lineas = Datos.Lineas.Where(l => !string.IsNullOrWhiteSpace(l.Descripcion) || l.PrecioUnitario != 0).ToList();
        ResultadoEmision? r = null;
        try { r = await _emision.EmitirAsync(Datos, _usuario.UsuarioId!.Value); }
        catch (ExcepcionFacturacion ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        catch (ArgumentException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        if (r is null) { await CargarAsync(); if (Datos.FacturaRectificadaId is Guid rr) NumRectificada = (await _db.FacturasEmitidas.AsNoTracking().FirstOrDefaultAsync(f => f.Id == rr))?.NumSerieFactura; return Page(); }
        AvisoOk = $"Factura {r.Factura.NumSerieFactura} emitida. Huella {r.Huella[..12]}… (registro n.º {r.NumeroEnCadena} de la cadena). {(r.EstadoInicial == EstadoEnvio.EN_COLA ? "Envío a la AEAT en cola." : r.EstadoInicial.Etiqueta() + ".")}";
        return RedirectToPage("/Facturacion/Factura", new { id = r.Factura.Id });
    }

    private async Task CargarAsync()
    {
        var c = await _clientes.ObtenerAsync(Datos.ClienteEmisorId);
        NombreEmisor = c.NombreParaMostrar; NifEmisor = c.Nif;
        Destinatarios = await _db.Destinatarios.AsNoTracking().Where(d => d.ClienteEmisorId == c.Id && d.Activo).OrderBy(d => d.Nombre).ToListAsync();
    }
}
