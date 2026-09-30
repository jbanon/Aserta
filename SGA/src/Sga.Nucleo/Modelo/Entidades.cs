namespace Sga.Nucleo.Modelo;

/// <summary>Persona de SGA. Un nombre en una celda del Excel de control es un gestor.</summary>
public class Gestor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Iniciales { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nif { get; set; } = string.Empty;
    /// <summary>Color de la etiqueta en la matriz (token del sistema de diseno, p. ej. "morado", "verde").</summary>
    public string Color { get; set; } = "morado";
}

public class Cliente
{
    public int Id { get; set; }
    public TipoCliente Tipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string NombreCorto { get; set; } = string.Empty;
    public string Nif { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Localidad { get; set; } = string.Empty;
    public string? Iban { get; set; }
    public QuienEmite Emisor { get; set; }
    public FormaPago FormaPago { get; set; } = FormaPago.Domiciliacion;
    public bool TieneEmpleados { get; set; }
    public bool Presenta130 { get; set; }
    public bool OperacionesIntracomunitarias { get; set; }
    public bool AlquilaLocal { get; set; }
    public PeriodicidadHonorarios PeriodicidadHonorarios { get; set; } = PeriodicidadHonorarios.Trimestral;
    public decimal Honorarios { get; set; }
    public int GestorId { get; set; }
    public Gestor? Gestor { get; set; }
    public DateOnly FechaAlta { get; set; }
    /// <summary>Sociedades: SGA tiene una clave de solo consulta del banco para bajar el extracto. Nunca se guarda la clave.</summary>
    public bool ClaveConsultaBanco { get; set; }
    public DateTime? UltimaDescargaBancoUtc { get; set; }
    public string? Notas { get; set; }
    public string? NombreContacto { get; set; }
    public List<Actividad> Actividades { get; set; } = [];
    public List<ContratoAlquiler> Contratos { get; set; } = [];

    public string Iniciales => string.Concat(NombreCorto.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
}

/// <summary>Actividad economica (epigrafe IAE). Un profesional puede tener varias; los gastos se llevan por actividad.</summary>
public class Actividad
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Iae { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    /// <summary>La de mayor actividad: a ella se imputan las facturas comunes.</summary>
    public bool Principal { get; set; }
}

/// <summary>Arrendadores: cada local alquilado. SGA emite la factura mensual al inquilino.</summary>
public class ContratoAlquiler
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Inmueble { get; set; } = string.Empty;
    public string InquilinoNombre { get; set; } = string.Empty;
    public string InquilinoNif { get; set; } = string.Empty;
    public string InquilinoEmail { get; set; } = string.Empty;
    public string InquilinoDireccion { get; set; } = string.Empty;
    public decimal RentaMensual { get; set; }
    public decimal TipoIva { get; set; } = 21m;
    public decimal TipoRetencion { get; set; } = 19m;
    public bool Activo { get; set; } = true;
}

/// <summary>Una celda de la matriz de control: cliente x modelo x periodo.</summary>
public class Obligacion
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public short Ejercicio { get; set; }
    /// <summary>1T..4T, 1P..3P (pagos fraccionados del 202), AN (anual).</summary>
    public string Periodo { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public EstadoObligacion Estado { get; set; }
    public int? GestorId { get; set; }
    public Gestor? Gestor { get; set; }
    public DateOnly? FechaPresentacion { get; set; }
    public decimal? Resultado { get; set; }
    /// <summary>Solo 303: cuota repercutida y soportada que quedaron liquidadas con esta presentacion (base del calculo acumulado del trimestre siguiente).</summary>
    public decimal? RepercutidoLiquidado { get; set; }
    public decimal? SoportadoLiquidado { get; set; }
    public string? Observaciones { get; set; }
    public EstadoFraccionamiento Fraccionamiento { get; set; }
    public DateTime? FraccionamientoSolicitadoUtc { get; set; }
    public Presentacion? Presentacion { get; set; }
}

/// <summary>Justificante simulado de presentacion (mismos campos que el de la AEAT; sin conexion real).</summary>
public class Presentacion
{
    public int Id { get; set; }
    public int ObligacionId { get; set; }
    public DateTime FechaHora { get; set; }
    public string Expediente { get; set; } = string.Empty;
    public string Csv { get; set; } = string.Empty;
    public string NumeroJustificante { get; set; } = string.Empty;
    public string PresentadorNombre { get; set; } = string.Empty;
    public string PresentadorNif { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string? Iban { get; set; }
    public string ViaEntrada { get; set; } = "Presentación por Internet (simulada)";
}

public class FacturaEmitida
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int? ContratoId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public string DestinatarioNombre { get; set; } = string.Empty;
    public string DestinatarioNif { get; set; } = string.Empty;
    public string Concepto { get; set; } = string.Empty;
    public decimal Base { get; set; }
    public decimal TipoIva { get; set; }
    public decimal CuotaIva { get; set; }
    public decimal TipoRetencion { get; set; }
    public decimal CuotaRetencion { get; set; }
    public decimal Total { get; set; }
    public OrigenFactura Origen { get; set; }
    public EstadoFacturaEmitida Estado { get; set; }
    public DateTime? EnviadaUtc { get; set; }
    public bool RegistradaMonitor { get; set; }
    public int? DocumentoId { get; set; }
}

public class FacturaRecibida
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public DateOnly Fecha { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public string ProveedorNif { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Concepto { get; set; } = string.Empty;
    public decimal Base { get; set; }
    public decimal TipoIva { get; set; }
    public decimal CuotaIva { get; set; }
    public bool Deducible { get; set; } = true;
    public decimal Total { get; set; }
    public CategoriaGasto Categoria { get; set; } = CategoriaGasto.ServiciosExteriores;
    public int? ActividadId { get; set; }
    /// <summary>Factura comun a varias actividades: se imputa a la principal (regla de SGA).</summary>
    public bool Comun { get; set; }
    public int? DocumentoId { get; set; }
    public bool EsHonorariosSga { get; set; }
}

public class Documento
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public TipoDocumento Tipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Ruta { get; set; }
    public string TipoMime { get; set; } = "application/octet-stream";
    public long Tamano { get; set; }
    public DateTime SubidoUtc { get; set; }
    public string SubidoPor { get; set; } = string.Empty;
    public short Ejercicio { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Recibido;
    public string? Nota { get; set; }
    public decimal? ImporteDetectado { get; set; }
}

public class MovimientoBancario
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public DateOnly Fecha { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public decimal Saldo { get; set; }
    public int? FacturaEmitidaId { get; set; }
    public int? FacturaRecibidaId { get; set; }
    public EstadoConciliacion Estado { get; set; }
}

public class Incidencia
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public TipoIncidencia Tipo { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;
    public DateTime CreadaUtc { get; set; }
    public int? GestorId { get; set; }
    public int? ObligacionId { get; set; }
    public List<Mensaje> Mensajes { get; set; } = [];
}

public class Mensaje
{
    public int Id { get; set; }
    public int IncidenciaId { get; set; }
    public string Autor { get; set; } = string.Empty;
    public bool EsGestor { get; set; }
    public string Texto { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; }
    public bool LeidoPorCliente { get; set; }
    public bool LeidoPorGestor { get; set; }
}

/// <summary>Facturas de SGA al cliente por sus honorarios (se muestran; no se generan en la demo).</summary>
public class FacturaHonorarios
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public decimal Base { get; set; }
    public decimal CuotaIva { get; set; }
    public decimal Total { get; set; }
    public bool Pagada { get; set; }
}

public class Aviso
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string? Enlace { get; set; }
    public DateTime FechaUtc { get; set; }
    public bool Leido { get; set; }
}
