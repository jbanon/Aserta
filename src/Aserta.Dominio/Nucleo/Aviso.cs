namespace Aserta.Dominio.Nucleo;

public static class TiposAviso
{
    public const string VencimientoProximo = "VencimientoProximo";
    public const string Vencida = "Vencida";
    public const string DocumentacionRecibida = "DocumentacionRecibida";
    public const string AprobacionCliente = "AprobacionCliente";
    public const string Sistema = "Sistema";
}

/// <summary>Aviso en la bandeja en pantalla de un usuario (implementacion de INotificador en la demo).</summary>
public class Aviso
{
    public long Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid UsuarioDestinoId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Cuerpo { get; set; }
    public string? EntidadTipo { get; set; }
    public string? EntidadId { get; set; }
    /// <summary>Clave de idempotencia por destinatario: el mismo aviso no se genera dos veces.</summary>
    public string Clave { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; }
    public DateTime? LeidoUtc { get; set; }

    public bool Leido => LeidoUtc.HasValue;
}
