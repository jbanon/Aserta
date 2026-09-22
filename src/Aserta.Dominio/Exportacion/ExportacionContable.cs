namespace Aserta.Dominio.Exportacion;

/// <summary>Una exportacion generada. El fichero se conserva en el almacen: es la prueba de lo que se entrego.</summary>
public class ExportacionContable
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid ClienteId { get; set; }
    public string Formato { get; set; } = string.Empty;
    public short Ejercicio { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public DateTime FechaGeneracionUtc { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid ClaveAlmacen { get; set; }
    public string NombreFichero { get; set; } = string.Empty;
    public int NumRegistros { get; set; }
    public string HashSha256 { get; set; } = string.Empty;
    public bool IncluyoExportados { get; set; }
}

/// <summary>RD-11: un documento ya exportado no se reexporta salvo peticion explicita.</summary>
public class ExportacionDocumento
{
    public Guid ExportacionId { get; set; }
    public Guid DocumentoId { get; set; }
    public Guid GestoriaId { get; set; }
}

public class ExportacionFactura
{
    public Guid ExportacionId { get; set; }
    public Guid FacturaEmitidaId { get; set; }
    public Guid GestoriaId { get; set; }
}

public enum TipoApunte { FacturaEmitida, FacturaRecibida }

/// <summary>Un apunte listo para exportar, independiente del formato de destino.</summary>
public sealed record ApunteExportable(
    TipoApunte Tipo,
    Guid ReferenciaId,
    DateOnly Fecha,
    string NumeroFactura,
    string? NifContraparte,
    string NombreContraparte,
    decimal BaseImponible,
    decimal TipoIva,
    decimal CuotaIva,
    decimal CuotaRecargo,
    decimal Retencion,
    decimal Total,
    string Concepto,
    string Origen,          // Verifactu | OCR simulado | Manual
    bool DatosConfirmados); // false = sugeridos por OCR y no confirmados linea a linea

public sealed record FicheroExportado(string NombreFichero, string TipoMime, byte[] Contenido);

/// <summary>
/// Puerto de exportacion contable (01-mapa-dominio.md §1.3). Vive en el dominio
/// para que Aserta.Exportacion (→ Dominio) lo implemente y Aserta.Aplicacion (→ Dominio) lo use.
/// </summary>
public interface IExportadorContable
{
    string Formato { get; }
    string Nombre { get; }
    string Descripcion { get; }
    /// <summary>false = formato modelado pero sin especificacion oficial disponible (DA-07).</summary>
    bool Disponible { get; }
    FicheroExportado Exportar(IReadOnlyList<ApunteExportable> apuntes, string nifCliente, int ejercicio, string periodo);
}
