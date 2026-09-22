namespace Aserta.Dominio.Catalogo;

public enum Organismo { AEAT, RegistroMercantil }

/// <summary>Catalogo: 303, 130, 111... Dato compartido, sin tenant.</summary>
public class ModeloTributario
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string DescripcionCliente { get; set; } = string.Empty;
    public Periodicidad Periodicidad { get; set; }
    public bool EsInformativo { get; set; }
    public string? EsResumenAnualDe { get; set; }
    public Organismo Organismo { get; set; } = Organismo.AEAT;
}
