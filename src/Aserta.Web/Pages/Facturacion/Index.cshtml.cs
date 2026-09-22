using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Facturacion;
using Aserta.Verifactu.Cliente;
using Aserta.Verifactu.Servicios;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Facturacion;

public class IndexModel : PaginaBase
{
    private readonly IAsertaDb _db;
    private readonly ServicioClientes _clientes;
    private readonly OpcionesVerifactu _opciones;
    private readonly IClienteAeatVerifactu _cliente;
    private readonly IWebHostEnvironment _env;

    public IndexModel(IAsertaDb db, ServicioClientes clientes, OpcionesVerifactu opciones, IClienteAeatVerifactu cliente, IWebHostEnvironment env)
    {
        _db = db;
        _clientes = clientes;
        _opciones = opciones;
        _cliente = cliente;
        _env = env;
    }

    public sealed record Fila(FacturaEmitida Factura, string Emisor, EstadoEnvio? Estado, bool PdfListo, bool Anulada);
    [BindProperty(SupportsGet = true)] public Guid? EmisorId { get; set; }
    public List<Fila> Filas { get; private set; } = [];
    public Dictionary<Guid, string> Emisores { get; private set; } = [];
    public int Problemas { get; private set; }
    public bool SimuladorActivo => _opciones.UsarSimulador;
    public bool EsProduccion => _env.IsProduction() && _opciones.Entorno == Aserta.Verifactu.Qr.EntornoAeat.Produccion;
    public string NombreCliente => _cliente.Nombre;

    public async Task OnGetAsync()
    {
        Emisores = await (await _clientes.ConsultaVisiblesAsync()).OrderBy(c => c.RazonSocial).ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? c.RazonSocial);
        var q = _db.FacturasEmitidas.AsNoTracking().Where(f => Emisores.Keys.Contains(f.ClienteEmisorId));
        if (EmisorId is Guid e) q = q.Where(f => f.ClienteEmisorId == e);
        var facturas = await q.OrderByDescending(f => f.FechaHoraCreacionUtc).Take(200).ToListAsync();
        var ids = facturas.Select(f => f.Id).ToList();
        var registros = await _db.RegistrosFacturacion.AsNoTracking().Where(r => ids.Contains(r.FacturaEmitidaId)).ToListAsync();
        var estados = await _db.EstadosEnvio.AsNoTracking().Where(e => registros.Select(r => r.Id).Contains(e.RegistroFacturacionId)).ToDictionaryAsync(e => e.RegistroFacturacionId);
        var pdfs = await _db.FacturasPdf.AsNoTracking().Where(p => ids.Contains(p.FacturaEmitidaId) && p.ClaveAlmacen != null).Select(p => p.FacturaEmitidaId).ToListAsync();
        Filas = facturas.Select(f =>
        {
            var alta = registros.FirstOrDefault(r => r.FacturaEmitidaId == f.Id && r.Tipo == TipoRegistro.ALTA);
            var estado = alta is not null && estados.TryGetValue(alta.Id, out var e) ? e.Estado : (EstadoEnvio?)null;
            return new Fila(f, Emisores.GetValueOrDefault(f.ClienteEmisorId, "—"), estado, pdfs.Contains(f.Id), registros.Any(r => r.FacturaEmitidaId == f.Id && r.Tipo == TipoRegistro.ANULACION));
        }).ToList();
        Problemas = await _db.EstadosEnvio.CountAsync(e => e.Estado == EstadoEnvio.DEAD_LETTER || e.Estado == EstadoEnvio.ACEPTADO_CON_ERRORES || e.Estado == EstadoEnvio.RECHAZADO || e.Estado == EstadoEnvio.BLOQUEADO_SIN_CERTIFICADO || e.Estado == EstadoEnvio.ERROR_VALIDACION);
    }

    public static string ClaseEstado(EstadoEnvio? e) => e switch
    {
        EstadoEnvio.ACEPTADO => "insignia-ok",
        EstadoEnvio.ACEPTADO_CON_ERRORES or EstadoEnvio.RECHAZADO or EstadoEnvio.DEAD_LETTER or EstadoEnvio.ERROR_VALIDACION or EstadoEnvio.BLOQUEADO_SIN_CERTIFICADO => "insignia-error",
        EstadoEnvio.ERROR_TECNICO => "insignia-aviso",
        _ => "insignia-info"
    };
}
