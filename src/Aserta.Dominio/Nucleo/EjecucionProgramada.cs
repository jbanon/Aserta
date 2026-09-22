namespace Aserta.Dominio.Nucleo;

/// <summary>Registro de la ultima ejecucion de cada trabajo diario para que un reinicio no duplique ni se salte un dia.</summary>
public class EjecucionProgramada
{
    public string Tarea { get; set; } = string.Empty;
    public DateTime? UltimaEjecucionUtc { get; set; }
    public DateTime? ProximaEjecucionUtc { get; set; }
    public string Estado { get; set; } = "Pendiente";
}
