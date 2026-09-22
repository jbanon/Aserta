using Aserta.Aplicacion.Puertos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aserta.Infraestructura.Eventos;

/// <summary>
/// ADR-001 §2.3: publicador in-process trivial. Resuelve los manejadores del
/// ambito actual y los ejecuta en orden, de forma sincrona y dentro de la misma
/// unidad de trabajo (el llamador guarda cambios despues). Sin MediatR, sin broker.
/// </summary>
public sealed class PublicadorEventosInProcess : IPublicadorEventos
{
    private readonly IServiceProvider _servicios;
    private readonly ILogger<PublicadorEventosInProcess> _log;

    public PublicadorEventosInProcess(IServiceProvider servicios, ILogger<PublicadorEventosInProcess> log)
    {
        _servicios = servicios;
        _log = log;
    }

    public async Task PublicarAsync<TEvento>(TEvento evento, CancellationToken ct = default) where TEvento : IEventoDominio
    {
        foreach (var manejador in _servicios.GetServices<IManejadorEvento<TEvento>>())
        {
            _log.LogDebug("Evento {Evento} → {Manejador}", typeof(TEvento).Name, manejador.GetType().Name);
            await manejador.ManejarAsync(evento, ct);
        }
    }
}
