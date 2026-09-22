using System.Threading.Channels;
using Aserta.Infraestructura.Persistencia;
using Aserta.Verifactu.Servicios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aserta.Infraestructura.Facturacion;

/// <summary>Cola de PDF: Channel acotado con UN consumidor (ADR-001 §2.5 ColaPdfWorker). Al llenarse, Encolar devuelve false y el PDF se genera al abrirlo.</summary>
public sealed class ColaPdf : IColaPdf
{
    private readonly Channel<Guid> _canal = Channel.CreateBounded<Guid>(new BoundedChannelOptions(200) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });
    private int _pendientes;
    public ChannelReader<Guid> Lector => _canal.Reader;
    public int Pendientes => _pendientes;
    public bool Encolar(Guid facturaId)
    {
        if (!_canal.Writer.TryWrite(facturaId)) return false;
        Interlocked.Increment(ref _pendientes);
        return true;
    }
    internal void Consumido() => Interlocked.Decrement(ref _pendientes);
}

public sealed class ColaPdfWorker : BackgroundService
{
    private readonly ColaPdf _cola;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ColaPdfWorker> _log;

    public ColaPdfWorker(ColaPdf cola, IServiceScopeFactory scopes, ILogger<ColaPdfWorker> log)
    {
        _cola = cola;
        _scopes = scopes;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var facturaId in _cola.Lector.ReadAllAsync(ct))
        {
            _cola.Consumido();
            try
            {
                using var scope = _scopes.CreateScope();
                using var ambito = scope.ServiceProvider.GetRequiredService<ContextoEjecucion>().AbrirAmbitoMantenimiento();
                await scope.ServiceProvider.GetRequiredService<ServicioPdfFactura>().GenerarSiFaltaAsync(facturaId, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _log.LogError(ex, "No se pudo generar el PDF de la factura {Id}", facturaId); }
        }
    }
}

/// <summary>
/// EnvioVerifactuWorker (ADR-001 §2.5): consume la cola outbox por emisor. Abre un
/// ambito de mantenimiento para descubrir emisores con pendientes y un ambito de
/// tenant por emisor para procesarlos (RLS incluido). Libera filas EN_CURSO caducadas.
/// </summary>
public sealed class EnvioVerifactuWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly OpcionesVerifactu _opciones;
    private readonly ILogger<EnvioVerifactuWorker> _log;
    public int CiclosEjecutados { get; private set; }

    public EnvioVerifactuWorker(IServiceScopeFactory scopes, OpcionesVerifactu opciones, ILogger<EnvioVerifactuWorker> log)
    {
        _scopes = scopes;
        _opciones = opciones;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_opciones.WorkerActivo) { _log.LogWarning("Worker de envío Veri*Factu desactivado por configuración (Verifactu:WorkerActivo = false)"); return; }
        try { await Task.Delay(TimeSpan.FromSeconds(10), ct); } catch (OperationCanceledException) { return; }
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(_opciones.SegundosEntreCiclosWorker, 1, 300)));
        do
        {
            try { await CicloAsync(ct); CiclosEjecutados++; }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _log.LogError(ex, "Worker de envío Veri*Factu: error en el ciclo"); }
        } while (await timer.WaitForNextTickAsync(ct));
    }

    public async Task CicloAsync(CancellationToken ct)
    {
        List<EmisorConPendientes> emisores;
        using (var raiz = _scopes.CreateScope())
        {
            using var mantenimiento = raiz.ServiceProvider.GetRequiredService<ContextoEjecucion>().AbrirAmbitoMantenimiento();
            var repo = raiz.ServiceProvider.GetRequiredService<IRepositorioFacturacion>();
            var reloj = raiz.ServiceProvider.GetRequiredService<IRelojVerifactu>();
            int liberadas = await repo.LiberarTomadasCaducadasAsync(TimeSpan.FromMinutes(10), reloj.AhoraUtc, ct);
            if (liberadas > 0) _log.LogWarning("Worker de envío: {N} envíos EN_CURSO caducados devueltos a PENDIENTE", liberadas);
            emisores = (await repo.EmisoresConPendientesAsync(reloj.AhoraUtc, ct)).ToList();
        }

        foreach (var emisor in emisores)
        {
            using var scope = _scopes.CreateScope();
            using var ambito = scope.ServiceProvider.GetRequiredService<ContextoEjecucion>().AbrirAmbitoTenant(emisor.GestoriaId);
            var servicio = scope.ServiceProvider.GetRequiredService<ServicioEnvio>();
            var r = await servicio.ProcesarEmisorAsync(emisor, ct);
            if (r is null) continue;
            if (r.Error is null) _log.LogInformation("Envío {Nif}: {Enviados} enviados · {Ok} aceptados · {ConErrores} con errores · {Rechazados} rechazados", r.NifEmisor, r.Enviados, r.Aceptados, r.AceptadosConErrores, r.Rechazados);
            else _log.LogWarning("Envío {Nif}: {Enviados} registros no enviados: {Error}", r.NifEmisor, r.Enviados, r.Error);
        }
    }
}
