using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aserta.Aplicacion.Obligaciones;

/// <summary>
/// Tarea diaria (ADR-001 §2.5 AvisosVencimientoWorker): avisa al asesor de cada
/// obligacion abierta que vence en 7 dias, en 3 dias, hoy, o que ya ha vencido.
/// Un aviso por obligacion y umbral (clave de idempotencia), nunca uno por ejecucion.
/// </summary>
public sealed class ServicioAvisosVencimiento : ITareaDiaria
{
    private readonly IAsertaDb _db;
    private readonly INotificador _notificador;
    private readonly IRelojSistema _reloj;
    private readonly ILogger<ServicioAvisosVencimiento> _log;

    public ServicioAvisosVencimiento(IAsertaDb db, INotificador notificador, IRelojSistema reloj, ILogger<ServicioAvisosVencimiento> log)
    {
        _db = db;
        _notificador = notificador;
        _reloj = reloj;
        _log = log;
    }

    public string Nombre => "AvisosVencimiento";

    public async Task EjecutarParaTenantAsync(CancellationToken ct) => await GenerarAsync(ct);

    public async Task<int> GenerarAsync(CancellationToken ct = default)
    {
        var hoy = _reloj.Hoy;
        var limite = hoy.AddDays(7);
        var abiertas = await _db.Obligaciones.AsNoTracking()
            .Where(o => o.Estado != EstadoObligacion.Presentado && o.Estado != EstadoObligacion.Cerrado && o.Estado != EstadoObligacion.NoAplica
                     && (o.FechaLimiteDomiciliacion ?? o.FechaLimitePresentacion) <= limite)
            .ToListAsync(ct);
        if (abiertas.Count == 0) return 0;

        var clientes = await _db.Clientes.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? c.RazonSocial, ct);
        var socios = await _db.Usuarios.AsNoTracking().Where(u => u.ClienteId == null && u.Estado == EstadoUsuario.Activo).Select(u => u.Id).ToListAsync(ct);

        int creados = 0;
        foreach (var o in abiertas)
        {
            int dias = Semaforo.DiasRestantes(o, hoy);
            var (umbral, tipo, titulo) = dias switch
            {
                < 0 => ("vencida", TiposAviso.Vencida, $"Vencida: {o.Titulo}"),
                0 => ("hoy", TiposAviso.VencimientoProximo, $"Vence hoy: {o.Titulo}"),
                <= 3 => ("3d", TiposAviso.VencimientoProximo, $"Vence en {dias} días: {o.Titulo}"),
                _ => ("7d", TiposAviso.VencimientoProximo, $"Vence en {dias} días: {o.Titulo}"),
            };
            var destinatario = o.AsesorId ?? socios.FirstOrDefault();
            if (destinatario == Guid.Empty) continue;

            var cliente = clientes.GetValueOrDefault(o.ClienteId, "—");
            var cuerpo = $"{cliente} · {o.Estado.Etiqueta()} · fecha de referencia {Semaforo.FechaDeReferencia(o):dd/MM/yyyy}";
            if (await _notificador.NotificarAsync(new Notificacion(destinatario, tipo, titulo, cuerpo, nameof(Obligacion), o.Id.ToString(), $"vencimiento:{o.Id}:{umbral}"), ct))
                creados++;
        }
        if (creados > 0) await _db.GuardarCambiosAsync(ct);
        _log.LogInformation("Avisos de vencimiento: {N} nuevos", creados);
        return creados;
    }
}
