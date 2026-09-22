using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Facturacion;
using Aserta.Infraestructura.Facturacion;
using Aserta.Verifactu.Cliente;
using Aserta.Verifactu.Servicios;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Facturacion;

public class EnviosModel : PaginaBase
{
    private readonly IAsertaDb _db;
    private readonly ServicioEnvio _envio;
    private readonly EnvioVerifactuWorker _worker;
    private readonly OpcionesVerifactu _opciones;
    private readonly EstadoSimuladorAeat _simulador;

    public EnviosModel(IAsertaDb db, ServicioEnvio envio, EnvioVerifactuWorker worker, OpcionesVerifactu opciones, EstadoSimuladorAeat simulador)
    {
        _db = db;
        _envio = envio;
        _worker = worker;
        _opciones = opciones;
        _simulador = simulador;
    }

    public sealed record Fila(RegistroFacturacion Registro, EstadoEnvioRegistro Estado, EnvioPendiente? Cola, string NumFactura, string Emisor);
    [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
    public List<Fila> Filas { get; private set; } = [];
    public List<LoteEnvio> Lotes { get; private set; } = [];
    public int EnCola, Aceptados, AceptadosConErrores, Rechazados, DeadLetter, Bloqueados;
    public bool SimuladorActivo => _opciones.UsarSimulador;
    public string ModoSimulador => _simulador.Modo.ToString();

    public async Task OnGetAsync()
    {
        var todos = await _db.EstadosEnvio.AsNoTracking().ToListAsync();
        EnCola = todos.Count(e => e.Estado is EstadoEnvio.EN_COLA or EstadoEnvio.ENVIADO or EstadoEnvio.ERROR_TECNICO or EstadoEnvio.GENERADO);
        Aceptados = todos.Count(e => e.Estado == EstadoEnvio.ACEPTADO);
        AceptadosConErrores = todos.Count(e => e.Estado == EstadoEnvio.ACEPTADO_CON_ERRORES);
        Rechazados = todos.Count(e => e.Estado is EstadoEnvio.RECHAZADO or EstadoEnvio.ERROR_VALIDACION);
        DeadLetter = todos.Count(e => e.Estado == EstadoEnvio.DEAD_LETTER);
        Bloqueados = todos.Count(e => e.Estado == EstadoEnvio.BLOQUEADO_SIN_CERTIFICADO);

        IEnumerable<EstadoEnvioRegistro> sel = todos;
        if (string.IsNullOrEmpty(Estado)) sel = todos.Where(e => e.Estado != EstadoEnvio.ACEPTADO);
        else if (Estado != "Todos" && Enum.TryParse<EstadoEnvio>(Estado, out var est)) sel = todos.Where(e => e.Estado == est);
        var ids = sel.Select(e => e.RegistroFacturacionId).ToList();
        var registros = await _db.RegistrosFacturacion.AsNoTracking().Where(r => ids.Contains(r.Id)).ToDictionaryAsync(r => r.Id);
        var colas = await _db.EnviosPendientes.AsNoTracking().Where(p => ids.Contains(p.RegistroFacturacionId)).GroupBy(p => p.RegistroFacturacionId).Select(g => g.OrderByDescending(p => p.Id).First()).ToDictionaryAsync(p => p.RegistroFacturacionId);
        var facturas = await _db.FacturasEmitidas.AsNoTracking().Where(f => registros.Values.Select(r => r.FacturaEmitidaId).Contains(f.Id)).ToDictionaryAsync(f => f.Id);
        var emisores = await _db.Clientes.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? c.RazonSocial);
        Filas = sel.Where(e => registros.ContainsKey(e.RegistroFacturacionId)).OrderByDescending(e => e.RegistroFacturacionId).Take(300)
            .Select(e => { var r = registros[e.RegistroFacturacionId]; var f = facturas.GetValueOrDefault(r.FacturaEmitidaId); return new Fila(r, e, colas.GetValueOrDefault(e.RegistroFacturacionId), f?.NumSerieFactura ?? "—", f is null ? "—" : emisores.GetValueOrDefault(f.ClienteEmisorId, "—")); }).ToList();
        Lotes = await _db.LotesEnvio.AsNoTracking().OrderByDescending(l => l.Id).Take(15).ToListAsync();
    }

    public async Task<IActionResult> OnPostCicloAsync([FromServices] IRepositorioFacturacion repo, [FromServices] IRelojVerifactu reloj)
    {
        int adelantados = await repo.AdelantarPendientesAsync(reloj.AhoraUtc, HttpContext.RequestAborted);
        await _worker.CicloAsync(HttpContext.RequestAborted);
        AvisoOk = $"Ciclo del worker ejecutado ({adelantados} reintentos adelantados).";
        return RedirectToPage(new { estado = Estado });
    }

    public async Task<IActionResult> OnPostReenviarAsync(long registroId)
    {
        try { await _envio.ReencolarAsync(registroId); AvisoOk = "Registro devuelto a la cola."; }
        catch (ExcepcionFacturacion ex) { AvisoError = ex.Message; }
        return RedirectToPage(new { estado = Estado });
    }
}
