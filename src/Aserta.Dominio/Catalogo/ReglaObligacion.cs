namespace Aserta.Dominio.Catalogo;

/// <summary>
/// "Cuando aplica un modelo". Las condiciones son AND; para OR se escriben dos
/// reglas del mismo modelo y el motor deduplica por modelo+periodo conservando la
/// de menor Prioridad como origen. RD-01: las reglas son filas, no codigo.
/// </summary>
public class ReglaObligacion
{
    public int Id { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string ModeloCodigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public Periodicidad Periodicidad { get; set; }
    public short VigenteDesdeEjercicio { get; set; }
    public short? VigenteHastaEjercicio { get; set; }
    public int Prioridad { get; set; } = 100;
    public bool Activa { get; set; } = true;
    public List<ReglaCondicion> Condiciones { get; set; } = [];

    public bool VigenteEn(int ejercicio) =>
        Activa && VigenteDesdeEjercicio <= ejercicio && (VigenteHastaEjercicio is null || VigenteHastaEjercicio >= ejercicio);

    /// <summary>Todas las condiciones se cumplen sobre los atributos dados (AND). Una regla sin condiciones aplica siempre.</summary>
    public bool SeCumple(IReadOnlyDictionary<string, string> atributos) => Condiciones.All(c => c.SeCumple(atributos));
}
