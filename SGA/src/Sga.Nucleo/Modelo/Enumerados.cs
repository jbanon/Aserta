namespace Sga.Nucleo.Modelo;

/// <summary>Los tres tipos de cliente que lleva SGA (apuntes de la reunion, punto 4 del encargo).</summary>
public enum TipoCliente { Arrendador = 1, Profesional = 2, Sociedad = 3 }

/// <summary>Quien emite las facturas del cliente: SGA por cuenta de el (arrendadores y profesionales B1) o el propio cliente (B2 y sociedades).</summary>
public enum QuienEmite { Sga = 1, Cliente = 2 }

public enum FormaPago { Domiciliacion = 1, Transferencia = 2 }

public enum PeriodicidadHonorarios { Mensual = 1, Trimestral = 2 }

/// <summary>Estado de una celda de la matriz de control (el Excel "CONTROL DE IMPUESTOS").</summary>
public enum EstadoObligacion
{
    /// <summary>NP en el Excel: ese modelo no aplica a este cliente.</summary>
    NoProcede = 0,
    /// <summary>Celda vacia: pendiente de empezar.</summary>
    Pendiente = 1,
    /// <summary>Un nombre en la celda: alguien de SGA lo tiene entre manos.</summary>
    EnCurso = 2,
    /// <summary>Presentado en la AEAT, con justificante.</summary>
    Presentado = 3,
}

public enum OrigenFactura { Sga = 1, Cliente = 2 }

public enum EstadoFacturaEmitida
{
    Borrador = 0,
    Generada = 1,
    EnviadaInquilino = 2,
    CopiaRecibida = 3,
}

public enum TipoDocumento
{
    Ticket = 1,
    FacturaRecibida = 2,
    FacturaEmitida = 3,
    ExtractoBancario = 4,
    Nomina = 5,
    SeguroOCuota = 6,
    Otro = 9,
}

public enum EstadoDocumento { Recibido = 1, Revisado = 2, Incidencia = 3 }

/// <summary>Columnas del libro de gastos del Excel de referencia.</summary>
public enum CategoriaGasto
{
    ConsumosExplotacion = 1,
    SueldosSalarios = 2,
    SeguridadSocial = 3,
    Arrendamientos = 4,
    ServiciosExteriores = 5,
    Tributos = 6,
    GastosFinancieros = 7,
    Amortizaciones = 8,
}

public enum EstadoConciliacion { Pendiente = 0, Conciliado = 1, Ignorado = 2 }

public enum TipoIncidencia { Fraccionamiento = 1, Numeracion = 2, Documento = 3, Consulta = 4, Conciliacion = 5, Aprobacion = 6 }

public enum EstadoIncidencia { Abierta = 1, EnCurso = 2, Resuelta = 3 }

public enum EstadoFraccionamiento { NoDisponible = 0, Disponible = 1, Solicitado = 2, Concedido = 3, Denegado = 4 }

public static class Etiquetas
{
    public static string Texto(this TipoCliente t) => t switch { TipoCliente.Arrendador => "Arrendador", TipoCliente.Profesional => "Autónomo o profesional", TipoCliente.Sociedad => "Sociedad", _ => t.ToString() };
    public static string Texto(this EstadoObligacion e) => e switch { EstadoObligacion.NoProcede => "No procede", EstadoObligacion.Pendiente => "Pendiente", EstadoObligacion.EnCurso => "En curso", EstadoObligacion.Presentado => "Presentado", _ => e.ToString() };
    public static string Texto(this EstadoFacturaEmitida e) => e switch { EstadoFacturaEmitida.Borrador => "Borrador", EstadoFacturaEmitida.Generada => "Generada", EstadoFacturaEmitida.EnviadaInquilino => "Enviada al inquilino", EstadoFacturaEmitida.CopiaRecibida => "Copia recibida", _ => e.ToString() };
    public static string Texto(this TipoDocumento t) => t switch { TipoDocumento.Ticket => "Ticket", TipoDocumento.FacturaRecibida => "Factura recibida", TipoDocumento.FacturaEmitida => "Factura emitida", TipoDocumento.ExtractoBancario => "Extracto bancario", TipoDocumento.Nomina => "Nómina", TipoDocumento.SeguroOCuota => "Seguro o cuota", _ => "Otro documento" };
    public static string Texto(this CategoriaGasto c) => c switch { CategoriaGasto.ConsumosExplotacion => "Consumos de explotación", CategoriaGasto.SueldosSalarios => "Sueldos y salarios", CategoriaGasto.SeguridadSocial => "Seguridad Social", CategoriaGasto.Arrendamientos => "Arrendamientos y cánones", CategoriaGasto.ServiciosExteriores => "Otros servicios exteriores", CategoriaGasto.Tributos => "Tributos deducibles", CategoriaGasto.GastosFinancieros => "Gastos financieros", CategoriaGasto.Amortizaciones => "Amortizaciones", _ => c.ToString() };
    public static string Texto(this TipoIncidencia t) => t switch { TipoIncidencia.Fraccionamiento => "Fraccionamiento", TipoIncidencia.Numeracion => "Numeración", TipoIncidencia.Documento => "Documento", TipoIncidencia.Consulta => "Consulta", TipoIncidencia.Conciliacion => "Conciliación", TipoIncidencia.Aprobacion => "Aprobación", _ => t.ToString() };
    public static string Texto(this EstadoIncidencia e) => e switch { EstadoIncidencia.Abierta => "Abierta", EstadoIncidencia.EnCurso => "En curso", EstadoIncidencia.Resuelta => "Resuelta", _ => e.ToString() };
}
