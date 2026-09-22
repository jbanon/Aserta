using Aserta.Dominio.Comun;

namespace Aserta.Dominio.Documental;

public enum TipoDocumento { FacturaRecibida, FacturaEmitida, Ticket, ExtractoBancario, Nomina, ReciboAlquiler, Contrato, Justificante, Otro }

public enum EstadoDocumento { Recibido, EnRevision, Validado, Rechazado }

public static class TipoDocumentoExtensiones
{
    public static string Etiqueta(this TipoDocumento t) => t switch
    {
        TipoDocumento.FacturaRecibida => "Factura recibida",
        TipoDocumento.FacturaEmitida => "Factura emitida",
        TipoDocumento.Ticket => "Ticket",
        TipoDocumento.ExtractoBancario => "Extracto bancario",
        TipoDocumento.Nomina => "Nómina",
        TipoDocumento.ReciboAlquiler => "Recibo de alquiler",
        TipoDocumento.Contrato => "Contrato",
        TipoDocumento.Justificante => "Justificante de presentación",
        _ => "Otro"
    };

    /// <summary>Plural en lenguaje llano para la frase "te faltan N …".</summary>
    public static string Plural(this TipoDocumento t, int n) => (t, n == 1) switch
    {
        (TipoDocumento.FacturaRecibida, true) => "factura de compra", (TipoDocumento.FacturaRecibida, false) => "facturas de compra",
        (TipoDocumento.FacturaEmitida, true) => "factura emitida", (TipoDocumento.FacturaEmitida, false) => "facturas emitidas",
        (TipoDocumento.Ticket, true) => "ticket", (TipoDocumento.Ticket, false) => "tickets",
        (TipoDocumento.ExtractoBancario, true) => "extracto bancario", (TipoDocumento.ExtractoBancario, false) => "extractos bancarios",
        (TipoDocumento.Nomina, true) => "nómina", (TipoDocumento.Nomina, false) => "nóminas",
        (TipoDocumento.ReciboAlquiler, true) => "recibo de alquiler", (TipoDocumento.ReciboAlquiler, false) => "recibos de alquiler",
        (TipoDocumento.Contrato, true) => "contrato", (TipoDocumento.Contrato, false) => "contratos",
        (TipoDocumento.Justificante, true) => "justificante", (TipoDocumento.Justificante, false) => "justificantes",
        (_, true) => "documento", _ => "documentos"
    };

    public static string Etiqueta(this EstadoDocumento e) => e switch
    {
        EstadoDocumento.Recibido => "Recibido",
        EstadoDocumento.EnRevision => "En revisión",
        EstadoDocumento.Validado => "Validado",
        EstadoDocumento.Rechazado => "Rechazado",
        _ => e.ToString()
    };
}

/// <summary>Fichero aportado por el cliente o la gestoria. El binario vive en IAlmacenDocumental bajo ClaveAlmacen.</summary>
public class Documento
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid ClienteId { get; set; }
    public TipoDocumento Tipo { get; set; }
    public short Ejercicio { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public string NombreOriginal { get; set; } = string.Empty;
    public Guid ClaveAlmacen { get; set; }
    public string HashSha256 { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string TipoMime { get; set; } = string.Empty;
    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Recibido;
    public string? MotivoRechazo { get; set; }
    public Guid SubidoPorId { get; set; }
    public DateTime FechaSubidaUtc { get; set; }
    public Guid? RevisadoPorId { get; set; }
    public DateTime? FechaRevisionUtc { get; set; }
    public string? DatosExtraidos { get; set; }
    public string? OrigenExtraccion { get; set; }
    public string? Notas { get; set; }

    public bool EsImagen => TipoMime.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    public bool EsPdf => TipoMime.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    /// <summary>Cuenta para los requisitos mientras no este rechazado.</summary>
    public bool CuentaParaRequisitos => Estado != EstadoDocumento.Rechazado;

    public void MarcarEnRevision(Guid revisorId, DateTime ahoraUtc)
    {
        if (Estado is EstadoDocumento.Validado or EstadoDocumento.Rechazado) throw new ExcepcionDominio("El documento ya está revisado.");
        Estado = EstadoDocumento.EnRevision;
        RevisadoPorId = revisorId;
        FechaRevisionUtc = ahoraUtc;
    }

    public void Validar(Guid revisorId, DateTime ahoraUtc)
    {
        if (Estado == EstadoDocumento.Validado) throw new ExcepcionDominio("El documento ya está validado.");
        Estado = EstadoDocumento.Validado;
        MotivoRechazo = null;
        RevisadoPorId = revisorId;
        FechaRevisionUtc = ahoraUtc;
    }

    public void Rechazar(Guid revisorId, DateTime ahoraUtc, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new ExcepcionDominio("Indique el motivo del rechazo: el cliente tiene que saber qué corregir.");
        Estado = EstadoDocumento.Rechazado;
        MotivoRechazo = motivo.Trim();
        RevisadoPorId = revisorId;
        FechaRevisionUtc = ahoraUtc;
    }
}

/// <summary>Vinculo N:M entre documentos y obligaciones.</summary>
public class DocumentoObligacion
{
    public Guid DocumentoId { get; set; }
    public Guid ObligacionId { get; set; }
    public Guid GestoriaId { get; set; }
}
