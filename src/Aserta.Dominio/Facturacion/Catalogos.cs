namespace Aserta.Dominio.Facturacion;

/// <summary>Serie de numeracion de un emisor y ejercicio. Mutable: solo alimenta el registro inalterable.</summary>
public class SerieFacturacion
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid ClienteEmisorId { get; set; }
    public string Codigo { get; set; } = "A";
    public string Descripcion { get; set; } = string.Empty;
    public short Ejercicio { get; set; }
    public int UltimoNumero { get; set; }
    public bool Activa { get; set; } = true;

    /// <summary>Numero de serie + numero de factura tal como va al registro y al QR (max. 60 caracteres, ASCII 32-126).</summary>
    public string SiguienteNumSerieFactura() => $"{Codigo}-{Ejercicio}-{UltimoNumero + 1:0000}";
}

public class Destinatario
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid ClienteEmisorId { get; set; }
    public string? Nif { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string Pais { get; set; } = "ES";
    public bool Activo { get; set; } = true;
}

public class ArticuloServicio
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid ClienteEmisorId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public decimal TipoIva { get; set; } = 21m;
    public decimal? TipoRecargo { get; set; }
    public bool Exento { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>Una fila por NIF emisor: cadena de huellas (RD-05) y planificador de envio. Se lee con UPDLOCK, HOLDLOCK al emitir.</summary>
public class CadenaEmisor
{
    public string NifEmisor { get; set; } = string.Empty;
    public Guid GestoriaId { get; set; }
    public Guid ClienteEmisorId { get; set; }
    public string? UltimaHuella { get; set; }
    public long UltimoNumero { get; set; }
    public DateTime? FechaUltimoRegistroUtc { get; set; }
    public DateTime? ProximoEnvioPermitidoUtc { get; set; }
}

public class Certificado
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public string NifTitular { get; set; } = string.Empty;
    public TipoCertificado Tipo { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string HuellaDigital { get; set; } = string.Empty;
    public DateOnly ValidoDesde { get; set; }
    public DateOnly ValidoHasta { get; set; }
    public byte[]? MaterialCifrado { get; set; }
    public EstadoCertificado Estado { get; set; } = EstadoCertificado.Vigente;

    public bool VigenteEn(DateOnly fecha) => Estado == EstadoCertificado.Vigente && ValidoDesde <= fecha && ValidoHasta >= fecha;
    public int DiasParaCaducar(DateOnly hoy) => ValidoHasta.DayNumber - hoy.DayNumber;
}

public class Apoderamiento
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid ClienteId { get; set; }
    public string Alcance { get; set; } = "Remisión de registros de facturación Veri*Factu";
    public DateOnly FechaAlta { get; set; }
    public DateOnly? FechaFin { get; set; }
    public Guid? DocumentoRespaldoId { get; set; }

    public bool VigenteEn(DateOnly fecha) => FechaAlta <= fecha && (FechaFin is null || FechaFin >= fecha);
}

public class AccesoCertificadoLog
{
    public long Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid CertificadoId { get; set; }
    public Guid? UsuarioId { get; set; }
    public DateTime FechaUtc { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string NifObligado { get; set; } = string.Empty;
}

public class DeclaracionResponsableHistorico
{
    public int Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateOnly FechaSuscripcion { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public string HashSha256 { get; set; } = string.Empty;
    public DateTime FechaRegistroUtc { get; set; }
}
