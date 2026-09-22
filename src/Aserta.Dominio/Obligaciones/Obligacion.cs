using Aserta.Dominio.Catalogo;

namespace Aserta.Dominio.Obligaciones;

/// <summary>
/// La entidad central: Cliente x Modelo x Ejercicio x Periodo. Una tarjeta del
/// Kanban ES una obligacion. Nunca se borra: pasa a NoAplica.
/// </summary>
public class Obligacion
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid ClienteId { get; set; }
    public string ModeloCodigo { get; set; } = string.Empty;
    public short Ejercicio { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public EstadoObligacion Estado { get; set; } = EstadoObligacion.PendienteDocumentacion;
    public Guid? AsesorId { get; set; }
    public DateOnly? FechaLimiteDomiciliacion { get; set; }
    public DateOnly FechaLimitePresentacion { get; set; }
    public decimal? ImporteResultado { get; set; }
    public SignoResultado? SignoResultado { get; set; }
    public DateTime? FechaAprobacionClienteUtc { get; set; }
    public Guid? UsuarioAprobacionId { get; set; }
    public Guid? JustificanteDocumentoId { get; set; }
    public int OrdenEnColumna { get; set; }
    public int? ReglaOrigenId { get; set; }
    public DateTime FechaGeneracionUtc { get; set; }

    public List<ObligacionHistorial> Historial { get; set; } = [];

    public string EtiquetaPeriodo => Catalogo.Periodo.Etiqueta(Ejercicio, Periodo);
    public string Titulo => $"{ModeloCodigo} · {EtiquetaPeriodo}";

    /// <summary>Cambia de estado validando la maquina de estados y deja rastro en el historial.</summary>
    public void CambiarEstado(EstadoObligacion destino, bool gestoriaExigeAprobacionCliente, Guid? usuarioId, DateTime ahoraUtc, string? comentario = null)
    {
        MaquinaEstadosObligacion.Validar(this, destino, gestoriaExigeAprobacionCliente);
        var anterior = Estado;
        Estado = destino;
        Historial.Add(ObligacionHistorial.CambioEstado(this, anterior, destino, usuarioId, ahoraUtc, comentario));
    }

    public void RegistrarAprobacionCliente(Guid usuarioId, DateTime ahoraUtc)
    {
        FechaAprobacionClienteUtc = ahoraUtc;
        UsuarioAprobacionId = usuarioId;
        Historial.Add(new ObligacionHistorial
        {
            GestoriaId = GestoriaId, ObligacionId = Id, FechaUtc = ahoraUtc, UsuarioId = usuarioId,
            TipoEvento = TiposEventoHistorial.AprobacionCliente, Comentario = "El cliente ha dado su conformidad al borrador."
        });
    }
}
