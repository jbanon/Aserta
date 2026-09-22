namespace Aserta.Aplicacion.Puertos;

public sealed record Notificacion(Guid DestinatarioId, string Tipo, string Titulo, string? Cuerpo, string? EntidadTipo, string? EntidadId, string Clave);

/// <summary>
/// Puerto de avisos (01-mapa-dominio.md §1.3). En la demo: bandeja en pantalla.
/// En real: correo/SMS. Debe ser idempotente por (destinatario, clave).
/// </summary>
public interface INotificador
{
    /// <summary>Encola el aviso en la unidad de trabajo actual. Devuelve false si ya existia (misma clave).</summary>
    Task<bool> NotificarAsync(Notificacion notificacion, CancellationToken ct = default);
}

/// <summary>Trabajo con cadencia diaria, planificado por el proceso web (ADR-001 §2.5). Se ejecuta una vez por gestoria.</summary>
public interface ITareaDiaria
{
    string Nombre { get; }
    Task EjecutarParaTenantAsync(CancellationToken ct);
}
