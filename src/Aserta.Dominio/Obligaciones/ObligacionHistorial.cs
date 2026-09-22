namespace Aserta.Dominio.Obligaciones;

public static class TiposEventoHistorial
{
    public const string Generada = "Generada";
    public const string CambioEstado = "CambioEstado";
    public const string MarcadaNoAplica = "MarcadaNoAplica";
    public const string Reactivada = "Reactivada";
    public const string PlazoActualizado = "PlazoActualizado";
    public const string Reasignada = "Reasignada";
    public const string DocumentoVinculado = "DocumentoVinculado";
    public const string Mensaje = "Mensaje";
    public const string AprobacionCliente = "AprobacionCliente";
    public const string BorradorRegistrado = "BorradorRegistrado";
    public const string JustificanteArchivado = "JustificanteArchivado";
}

/// <summary>Linea de tiempo solo-anexado de una obligacion (RD-08).</summary>
public class ObligacionHistorial
{
    public long Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid ObligacionId { get; set; }
    public DateTime FechaUtc { get; set; }
    public Guid? UsuarioId { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public EstadoObligacion? EstadoAnterior { get; set; }
    public EstadoObligacion? EstadoNuevo { get; set; }
    public string? Comentario { get; set; }
    public string? ReferenciaId { get; set; }

    public static ObligacionHistorial CambioEstado(Obligacion o, EstadoObligacion anterior, EstadoObligacion nuevo, Guid? usuarioId, DateTime ahoraUtc, string? comentario) => new()
    {
        GestoriaId = o.GestoriaId, ObligacionId = o.Id, FechaUtc = ahoraUtc, UsuarioId = usuarioId,
        TipoEvento = TiposEventoHistorial.CambioEstado, EstadoAnterior = anterior, EstadoNuevo = nuevo, Comentario = comentario
    };
}
