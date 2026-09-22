namespace Aserta.Dominio.Nucleo;

/// <summary>Registro de auditoria solo-anexado (RD-08): quien, que, cuando, desde que IP.</summary>
public class Auditoria
{
    public long Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid? UsuarioId { get; set; }
    public DateTime FechaUtc { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string EntidadTipo { get; set; } = string.Empty;
    public string EntidadId { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public string? DireccionIp { get; set; }
    public string? AgenteUsuario { get; set; }
}

public static class AccionesAuditoria
{
    public const string Alta = "Alta";
    public const string Modificacion = "Modificacion";
    public const string Baja = "Baja";
    public const string InicioSesion = "InicioSesion";
    public const string InicioSesionFallido = "InicioSesionFallido";
    public const string CierreSesion = "CierreSesion";
    public const string CambioEstado = "CambioEstado";
    public const string GeneracionObligaciones = "GeneracionObligaciones";
    public const string Configuracion = "Configuracion";
}
