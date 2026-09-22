namespace Aserta.Dominio.Catalogo;

public class DiaInhabil
{
    public DateOnly Fecha { get; set; }
    public string Ambito { get; set; } = "Nacional";
    public string Descripcion { get; set; } = string.Empty;
}
