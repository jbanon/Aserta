namespace Aserta.Dominio.Clientes;

/// <summary>
/// Version temporal del perfil fiscal de un cliente. Sus atributos son los
/// interruptores que evalua el motor de reglas (01-mapa-dominio.md §2.1).
/// </summary>
public class PerfilFiscal
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid ClienteId { get; set; }
    public DateOnly VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
    public RegimenIrpf RegimenIrpf { get; set; } = RegimenIrpf.NoAplica;
    public RegimenIva RegimenIva { get; set; } = RegimenIva.NoAplica;
    public PeriodicidadIva PeriodicidadIva { get; set; } = PeriodicidadIva.NoAplica;
    public bool TieneEmpleados { get; set; }
    public bool PagaProfesionalesConRetencion { get; set; }
    public bool AlquilaLocal { get; set; }
    public bool RepartePagosCapitalMobiliario { get; set; }
    public bool OperacionesIntracomunitarias { get; set; }
    public bool SuperaUmbral347 { get; set; }
    public Territorio Territorio { get; set; } = Territorio.Comun;
    public byte CierreEjercicioMes { get; set; } = 12;
    public byte CierreEjercicioDia { get; set; } = 31;

    /// <summary>Nombres de atributo que admiten las condiciones de cat.ReglaCondicion.</summary>
    public static readonly IReadOnlyList<string> AtributosEvaluables =
    [
        "FormaJuridica", nameof(RegimenIrpf), nameof(RegimenIva), nameof(PeriodicidadIva), nameof(TieneEmpleados),
        nameof(PagaProfesionalesConRetencion), nameof(AlquilaLocal), nameof(RepartePagosCapitalMobiliario),
        nameof(OperacionesIntracomunitarias), nameof(SuperaUmbral347), nameof(Territorio)
    ];

    /// <summary>
    /// Proyeccion del perfil (mas la forma juridica del cliente) como pares
    /// atributo/valor de texto, que es lo que comparan las reglas del catalogo.
    /// Los booleanos se representan como "1" / "0".
    /// </summary>
    public IReadOnlyDictionary<string, string> ComoAtributos(FormaJuridica formaJuridica) =>
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FormaJuridica"] = formaJuridica.ToString(),
            [nameof(RegimenIrpf)] = RegimenIrpf.ToString(),
            [nameof(RegimenIva)] = RegimenIva.ToString(),
            [nameof(PeriodicidadIva)] = PeriodicidadIva.ToString(),
            [nameof(TieneEmpleados)] = Bit(TieneEmpleados),
            [nameof(PagaProfesionalesConRetencion)] = Bit(PagaProfesionalesConRetencion),
            [nameof(AlquilaLocal)] = Bit(AlquilaLocal),
            [nameof(RepartePagosCapitalMobiliario)] = Bit(RepartePagosCapitalMobiliario),
            [nameof(OperacionesIntracomunitarias)] = Bit(OperacionesIntracomunitarias),
            [nameof(SuperaUmbral347)] = Bit(SuperaUmbral347),
            [nameof(Territorio)] = Territorio.ToString(),
        };

    private static string Bit(bool b) => b ? "1" : "0";

    public PerfilFiscal Clonar() => (PerfilFiscal)MemberwiseClone();
}
