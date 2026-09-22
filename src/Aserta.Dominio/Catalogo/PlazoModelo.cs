namespace Aserta.Dominio.Catalogo;

/// <summary>Plazo de un modelo para un ejercicio y periodo. Fechas ya trasladadas por dia inhabil (RD-03).</summary>
public class PlazoModelo
{
    public int Id { get; set; }
    public string ModeloCodigo { get; set; } = string.Empty;
    public short Ejercicio { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public DateOnly FechaInicioPresentacion { get; set; }
    public DateOnly? FechaLimiteDomiciliacion { get; set; }
    public DateOnly FechaLimitePresentacion { get; set; }
    public string Fuente { get; set; } = string.Empty;
    /// <summary>El [VERIFICAR] hecho dato: false hasta contrastar con el calendario oficial de la AEAT.</summary>
    public bool Confirmado { get; set; }
}
