namespace Aserta.Dominio.Documental;

/// <summary>Lo que se espera recibir de un cliente para un periodo: la base de "te faltan 2 facturas de julio".</summary>
public class RequisitoPeriodo
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid ClienteId { get; set; }
    public short Ejercicio { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public TipoDocumento TipoDocumento { get; set; }
    /// <summary>NULL = "al menos uno".</summary>
    public int? CantidadEsperada { get; set; }
    public bool Obligatorio { get; set; } = true;
    public string Descripcion { get; set; } = string.Empty;
    public int? ReglaOrigenId { get; set; }
    public bool NoAplica { get; set; }
}
