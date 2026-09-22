namespace Aserta.Aplicacion.Puertos;

/// <summary>
/// Almacen de binarios (04-modelo-datos.md §5.3): nombre opaco (clave), fuera
/// del directorio de despliegue y cifrado en reposo. La base de datos solo
/// guarda la clave y el hash.
/// </summary>
public interface IAlmacenDocumental
{
    Task<Guid> GuardarAsync(Stream contenido, CancellationToken ct = default);
    Task<Stream> AbrirAsync(Guid clave, CancellationToken ct = default);
    Task<bool> ExisteAsync(Guid clave, CancellationToken ct = default);
}

/// <summary>Resultado sugerido por OCR/IA. Nunca autoritativo: lo confirma el gestor.</summary>
public sealed record DatosExtraidos(string OrigenExtraccion, string Json);

/// <summary>Puerto de extraccion de datos de facturas (simulado en la demo, DA-08).</summary>
public interface IExtractorDocumental
{
    Task<DatosExtraidos?> ExtraerAsync(string nombreFichero, string tipoMime, string hashSha256, string tipoDocumento, CancellationToken ct = default);
}

/// <summary>Evento de dominio in-process (ADR-001 §2.3): los modulos se hablan por eventos, no por tipos internos.</summary>
public interface IEventoDominio { }

public interface IManejadorEvento<in TEvento> where TEvento : IEventoDominio
{
    Task ManejarAsync(TEvento evento, CancellationToken ct);
}

public interface IPublicadorEventos
{
    Task PublicarAsync<TEvento>(TEvento evento, CancellationToken ct = default) where TEvento : IEventoDominio;
}

public sealed record DocumentoValidado(Guid DocumentoId, Guid ClienteId, int Ejercicio, string Periodo) : IEventoDominio;
public sealed record DocumentoRecibido(Guid DocumentoId, Guid ClienteId, Guid SubidoPorId, bool DesdePortal) : IEventoDominio;
public sealed record BorradorAprobado(Guid ObligacionId, Guid ClienteId, Guid UsuarioId) : IEventoDominio;
public sealed record BorradorRechazado(Guid ObligacionId, Guid ClienteId, Guid UsuarioId, string Motivo) : IEventoDominio;
public sealed record MensajeNuevo(Guid HiloId, Guid ClienteId, Guid AutorId, bool AutorEsCliente) : IEventoDominio;
