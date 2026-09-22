using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aserta.Infraestructura.Trabajos;

/// <summary>
/// Planificador trivial (ADR-001 §2.5): un PeriodicTimer comprueba cada hora si
/// cada ITareaDiaria ya se ejecuto hoy (dbo.EjecucionProgramada) y, si no, la
/// ejecuta una vez por gestoria abriendo un ambito de tenant explicito.
/// Sin Hangfire, sin Quartz. Un reinicio no duplica ni se salta un dia.
/// </summary>
public sealed class PlanificadorDiario : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IRelojSistema _reloj;
    private readonly ILogger<PlanificadorDiario> _log;
    private readonly TimeSpan _intervalo;

    public PlanificadorDiario(IServiceScopeFactory scopes, IRelojSistema reloj, ILogger<PlanificadorDiario> log, TimeSpan? intervalo = null)
    {
        _scopes = scopes;
        _reloj = reloj;
        _log = log;
        _intervalo = intervalo ?? TimeSpan.FromHours(1);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Pequeña espera para no competir con el arranque (migraciones + seed)
        try { await Task.Delay(TimeSpan.FromSeconds(15), ct); } catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(_intervalo);
        do
        {
            try { await EjecutarPendientesAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _log.LogError(ex, "Planificador diario: error"); }
        } while (await timer.WaitForNextTickAsync(ct));
    }

    public async Task EjecutarPendientesAsync(CancellationToken ct)
    {
        using var raiz = _scopes.CreateScope();
        var nombres = raiz.ServiceProvider.GetServices<ITareaDiaria>().Select(t => t.Nombre).Distinct().ToList();
        if (nombres.Count == 0) return;

        var hoy = _reloj.Hoy;
        List<Guid> gestorias;
        using (var ambito = raiz.ServiceProvider.GetRequiredService<ContextoEjecucion>().AbrirAmbitoMantenimiento())
        {
            var db = raiz.ServiceProvider.GetRequiredService<AsertaDbContext>();
            gestorias = await db.Gestorias.AsNoTracking().Where(g => g.Estado == EstadoGestoria.Activa).Select(g => g.Id).ToListAsync(ct);

            foreach (var nombre in nombres)
            {
                var registro = await db.EjecucionesProgramadas.FirstOrDefaultAsync(e => e.Tarea == nombre, ct);
                if (registro?.UltimaEjecucionUtc is DateTime ultima && DateOnly.FromDateTime(ultima) >= hoy) continue;

                _log.LogInformation("Tarea diaria {Tarea}: ejecutando para {N} gestorías", nombre, gestorias.Count);
                foreach (var gestoriaId in gestorias)
                    await EjecutarParaAsync(nombre, gestoriaId, ct);

                registro ??= db.EjecucionesProgramadas.Add(new EjecucionProgramada { Tarea = nombre }).Entity;
                registro.UltimaEjecucionUtc = _reloj.AhoraUtc;
                registro.ProximaEjecucionUtc = hoy.AddDays(1).ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc);
                registro.Estado = "Correcto";
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private async Task EjecutarParaAsync(string nombre, Guid gestoriaId, CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<ContextoEjecucion>();
        using var ambito = contexto.AbrirAmbitoTenant(gestoriaId);
        var tarea = scope.ServiceProvider.GetServices<ITareaDiaria>().First(t => t.Nombre == nombre);
        try { await tarea.EjecutarParaTenantAsync(ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { _log.LogError(ex, "Tarea {Tarea} falló para la gestoría {Gestoria}", nombre, gestoriaId); }
    }
}
