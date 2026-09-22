namespace Aserta.Dominio.Nucleo;

public enum EstadoGestoria { Activa, Suspendida, Baja }

/// <summary>El tenant. Unidad de aislamiento (RD-04).</summary>
public class Gestoria
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Nif { get; set; } = string.Empty;
    public EstadoGestoria Estado { get; set; } = EstadoGestoria.Activa;
    public DateOnly FechaAlta { get; set; }
    public string ZonaHoraria { get; set; } = "Europe/Madrid";
    /// <summary>RD-09 como configuracion del tenant.</summary>
    public bool ExigeAprobacionCliente { get; set; } = true;
    /// <summary>RD-07 como configuracion del tenant.</summary>
    public bool AsesorVeTodosLosClientes { get; set; } = true;
}
