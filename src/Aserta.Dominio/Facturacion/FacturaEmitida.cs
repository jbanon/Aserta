namespace Aserta.Dominio.Facturacion;

/// <summary>Factura emitida. INMUTABLE: nunca se edita ni se borra (RD-06); se rectifica o se anula con un registro nuevo.</summary>
public class FacturaEmitida
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid ClienteEmisorId { get; set; }
    public string NifEmisor { get; set; } = string.Empty;
    public Guid SerieId { get; set; }
    public string NumSerieFactura { get; set; } = string.Empty;
    public DateOnly FechaExpedicion { get; set; }
    public TipoFactura TipoFactura { get; set; }
    public TipoRectificativa? TipoRectificativa { get; set; }
    public Guid? FacturaRectificadaId { get; set; }
    public string? MotivoRectificacion { get; set; }
    public string? DestinatarioNif { get; set; }
    public string? DestinatarioNombre { get; set; }
    public decimal BaseTotal { get; set; }
    public decimal CuotaTotal { get; set; }
    public decimal CuotaRecargoTotal { get; set; }
    public decimal PorcentajeRetencion { get; set; }
    public decimal RetencionTotal { get; set; }
    public decimal ImporteTotal { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaHoraCreacionUtc { get; set; }
    public Guid UsuarioId { get; set; }
    public string UrlQr { get; set; } = string.Empty;

    public List<LineaFactura> Lineas { get; set; } = [];

    public bool EsSimplificada => TipoFactura == TipoFactura.F2;
}

public class LineaFactura
{
    public long Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid FacturaEmitidaId { get; set; }
    public int Orden { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal TipoIva { get; set; }
    public decimal? TipoRecargo { get; set; }
    public bool Exenta { get; set; }
    public decimal BaseLinea { get; set; }
}

/// <summary>Registro de alta o de anulacion, encadenado por NIF emisor. INMUTABLE. Guarda la cadena hasheada y el XML literal: la prueba.</summary>
public class RegistroFacturacion
{
    public long Id { get; set; }
    public Guid GestoriaId { get; set; }
    public string NifEmisor { get; set; } = string.Empty;
    public TipoRegistro Tipo { get; set; }
    public Guid FacturaEmitidaId { get; set; }
    public long NumeroEnCadena { get; set; }
    public bool PrimerRegistro { get; set; }
    public string? HuellaAnterior { get; set; }
    public string Huella { get; set; } = string.Empty;
    public DateTimeOffset FechaHoraHusoGenRegistro { get; set; }
    public string FechaHoraHusoGenRegistroTexto { get; set; } = string.Empty;
    public string CadenaHuella { get; set; } = string.Empty;
    public string XmlRegistro { get; set; } = string.Empty;
    public string IdSistemaInformatico { get; set; } = string.Empty;
    public string VersionSistemaInformatico { get; set; } = string.Empty;
    public string NumeroInstalacion { get; set; } = string.Empty;
}

/// <summary>Estado mutable del envio de un registro.</summary>
public class EstadoEnvioRegistro
{
    public long RegistroFacturacionId { get; set; }
    public Guid GestoriaId { get; set; }
    public EstadoEnvio Estado { get; set; } = EstadoEnvio.GENERADO;
    public int Intentos { get; set; }
    public DateTime? FechaEnvioUtc { get; set; }
    public DateTime? FechaRespuestaUtc { get; set; }
    public string? CsvAeat { get; set; }
    public string? CodigoErrorAeat { get; set; }
    public string? DescripcionErrorAeat { get; set; }
    public long? LoteEnvioId { get; set; }
}

/// <summary>Fila de la cola (outbox). Se inserta en la misma transaccion que el registro.</summary>
public class EnvioPendiente
{
    public long Id { get; set; }
    public long RegistroFacturacionId { get; set; }
    public Guid GestoriaId { get; set; }
    public string NifEmisor { get; set; } = string.Empty;
    public DateTime FechaAltaUtc { get; set; }
    public DateTime ProximoIntentoUtc { get; set; }
    public int Intentos { get; set; }
    public EstadoEnvioPendiente Estado { get; set; } = EstadoEnvioPendiente.PENDIENTE;
    public DateTime? TomadoUtc { get; set; }
    public string? UltimoError { get; set; }
    public long? LoteEnvioId { get; set; }
}

public class LoteEnvio
{
    public long Id { get; set; }
    public Guid GestoriaId { get; set; }
    public string NifEmisor { get; set; } = string.Empty;
    public DateTime FechaEnvioUtc { get; set; }
    public int NumRegistros { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string? RespuestaXml { get; set; }
    public int? TiempoEsperaSegundos { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public string? Error { get; set; }
}

public class FacturaPdf
{
    public Guid FacturaEmitidaId { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid? ClaveAlmacen { get; set; }
    public string? VersionPlantilla { get; set; }
    public string? Motor { get; set; }
    public DateTime? FechaGeneracionUtc { get; set; }
    public int Intentos { get; set; }
    public string? Error { get; set; }

    public bool Generado => ClaveAlmacen.HasValue;
}

/// <summary>
/// Politica de reintentos del envio (envio-y-cola-reintentos.md §5.1): retroceso
/// exponencial con jitter, base 30 s, factor 2, techo 1 h, maximo 8 intentos.
/// </summary>
public static class PoliticaReintentos
{
    public const int MaximoIntentos = 8;
    public static readonly TimeSpan Base = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan Techo = TimeSpan.FromHours(1);

    public static TimeSpan Espera(int intentoRealizado, Random? aleatorio = null)
    {
        var r = aleatorio ?? Random.Shared;
        double segundos = Math.Min(Techo.TotalSeconds, Base.TotalSeconds * Math.Pow(2, Math.Max(0, intentoRealizado - 1)));
        double jitter = 1 + (r.NextDouble() * 0.4 - 0.2);   // ±20 %
        return TimeSpan.FromSeconds(segundos * jitter);
    }
}
