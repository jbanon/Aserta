using Aserta.Dominio.Comun;

namespace Aserta.Dominio.Mensajeria;

public enum EstadoHilo { Abierto, Cerrado }

/// <summary>Conversacion anclada a UNA obligacion o a UN documento. No hay chat suelto (decision de producto).</summary>
public class Hilo
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid? ObligacionId { get; set; }
    public Guid? DocumentoId { get; set; }
    public string Asunto { get; set; } = string.Empty;
    public EstadoHilo Estado { get; set; } = EstadoHilo.Abierto;
    public DateTime FechaUltimoMensajeUtc { get; set; }
    public List<Mensaje> Mensajes { get; set; } = [];

    public static Hilo Nuevo(Guid gestoriaId, Guid clienteId, Guid? obligacionId, Guid? documentoId, string asunto, DateTime ahoraUtc)
    {
        if ((obligacionId is null) == (documentoId is null))
            throw new ExcepcionDominio("Un hilo se ancla exactamente a una obligación o a un documento.");
        if (string.IsNullOrWhiteSpace(asunto)) throw new ExcepcionDominio("El asunto es obligatorio.");
        return new Hilo { Id = Guid.NewGuid(), GestoriaId = gestoriaId, ClienteId = clienteId, ObligacionId = obligacionId, DocumentoId = documentoId, Asunto = asunto.Trim(), FechaUltimoMensajeUtc = ahoraUtc };
    }

    public Mensaje Responder(Guid autorId, bool autorEsCliente, string cuerpo, DateTime ahoraUtc)
    {
        if (string.IsNullOrWhiteSpace(cuerpo)) throw new ExcepcionDominio("El mensaje está vacío.");
        if (Estado == EstadoHilo.Cerrado) Estado = EstadoHilo.Abierto;
        var m = new Mensaje
        {
            GestoriaId = GestoriaId, HiloId = Id, AutorId = autorId, FechaUtc = ahoraUtc, Cuerpo = cuerpo.Trim(),
            LeidoPorClienteUtc = autorEsCliente ? ahoraUtc : null, LeidoPorGestoriaUtc = autorEsCliente ? null : ahoraUtc,
        };
        Mensajes.Add(m);
        FechaUltimoMensajeUtc = ahoraUtc;
        return m;
    }
}

public class Mensaje
{
    public long Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid HiloId { get; set; }
    public Guid AutorId { get; set; }
    public DateTime FechaUtc { get; set; }
    public string Cuerpo { get; set; } = string.Empty;
    public DateTime? LeidoPorClienteUtc { get; set; }
    public DateTime? LeidoPorGestoriaUtc { get; set; }
}
