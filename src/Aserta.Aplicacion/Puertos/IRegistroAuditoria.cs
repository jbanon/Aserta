namespace Aserta.Aplicacion.Puertos;

/// <summary>
/// Registro explicito de auditoria para acciones que no son un cambio de fila
/// (inicio de sesion, ejecucion del motor...). Los cambios de entidades los
/// captura solo el interceptor de EF.
/// </summary>
public interface IRegistroAuditoria
{
    /// <summary>Anade la entrada a la unidad de trabajo; se persiste con el siguiente GuardarCambiosAsync.</summary>
    void Registrar(string accion, string entidadTipo, string entidadId, object? detalle = null);

    /// <summary>Persiste inmediatamente (para eventos fuera de una unidad de trabajo, p. ej. login).</summary>
    Task RegistrarYGuardarAsync(string accion, string entidadTipo, string entidadId, object? detalle = null, Guid? gestoriaId = null, Guid? usuarioId = null, CancellationToken ct = default);
}
