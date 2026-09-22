namespace Aserta.Dominio.Nucleo;

/// <summary>Un script SQL aplicado por el runner, con su hash. Sin esta tabla la aplicacion no arranca.</summary>
public class MigracionAplicada
{
    public string Nombre { get; set; } = string.Empty;
    public string HashSha256 { get; set; } = string.Empty;
    public DateTime FechaAplicacionUtc { get; set; }
    public int DuracionMs { get; set; }
}
