using Aserta.Dominio.Comun;

namespace Aserta.Dominio.Obligaciones;

/// <summary>
/// Transiciones validas (01-mapa-dominio.md §5.1). El servidor valida SIEMPRE
/// contra esta tabla; la interfaz solo es optimista (ADR-003 §2.3).
/// </summary>
public static class MaquinaEstadosObligacion
{
    private static readonly IReadOnlyDictionary<EstadoObligacion, EstadoObligacion[]> Transiciones =
        new Dictionary<EstadoObligacion, EstadoObligacion[]>
        {
            [EstadoObligacion.PendienteDocumentacion]     = [EstadoObligacion.DocumentacionCompleta, EstadoObligacion.NoAplica],
            [EstadoObligacion.DocumentacionCompleta]      = [EstadoObligacion.PendienteDocumentacion, EstadoObligacion.EnPreparacion],
            [EstadoObligacion.EnPreparacion]              = [EstadoObligacion.RevisionInterna],
            [EstadoObligacion.RevisionInterna]            = [EstadoObligacion.EnPreparacion, EstadoObligacion.PendienteAprobacionCliente],
            [EstadoObligacion.PendienteAprobacionCliente] = [EstadoObligacion.EnPreparacion, EstadoObligacion.Presentado],
            [EstadoObligacion.Presentado]                 = [EstadoObligacion.Cerrado],
            [EstadoObligacion.Cerrado]                    = [],
            [EstadoObligacion.NoAplica]                   = [],
        };

    public static IReadOnlyList<EstadoObligacion> DestinosDesde(EstadoObligacion origen) => Transiciones[origen];

    public static bool EsTransicionValida(EstadoObligacion origen, EstadoObligacion destino) =>
        Transiciones[origen].Contains(destino);

    /// <summary>
    /// Comprueba la transicion, incluida RD-09: no se pasa a Presentado sin
    /// aprobacion del cliente registrada, salvo que la gestoria no la exija.
    /// </summary>
    public static void Validar(Obligacion obligacion, EstadoObligacion destino, bool gestoriaExigeAprobacionCliente)
    {
        if (obligacion.Estado == destino)
            throw new ExcepcionDominio($"La obligación ya está en el estado «{destino.Etiqueta()}».");
        if (!EsTransicionValida(obligacion.Estado, destino))
            throw new ExcepcionDominio($"No se puede pasar de «{obligacion.Estado.Etiqueta()}» a «{destino.Etiqueta()}».");
        if (destino == EstadoObligacion.Presentado && gestoriaExigeAprobacionCliente && obligacion.FechaAprobacionClienteUtc is null)
            throw new ExcepcionDominio("No se puede marcar como presentada sin la aprobación del cliente registrada (RD-09). La gestoría puede desactivar este requisito en su configuración.");
    }
}
