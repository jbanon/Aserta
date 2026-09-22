namespace Aserta.Dominio.Obligaciones;

/// <summary>Estados de la maquina de 01-mapa-dominio.md §5.1. Cada uno es una columna del Kanban.</summary>
public enum EstadoObligacion
{
    PendienteDocumentacion,
    DocumentacionCompleta,
    EnPreparacion,
    RevisionInterna,
    PendienteAprobacionCliente,
    Presentado,
    Cerrado,
    NoAplica
}

public enum SignoResultado { Ingresar, Devolver, Compensar, SinActividad }

public static class EstadoObligacionExtensiones
{
    public static string Etiqueta(this EstadoObligacion e) => e switch
    {
        EstadoObligacion.PendienteDocumentacion => "Pendiente documentación",
        EstadoObligacion.DocumentacionCompleta => "Documentación completa",
        EstadoObligacion.EnPreparacion => "En preparación",
        EstadoObligacion.RevisionInterna => "Revisión interna",
        EstadoObligacion.PendienteAprobacionCliente => "Pendiente aprobación cliente",
        EstadoObligacion.Presentado => "Presentado",
        EstadoObligacion.Cerrado => "Cerrado",
        EstadoObligacion.NoAplica => "No aplica",
        _ => e.ToString()
    };

    /// <summary>Estados que ya no se tocan: ni el motor ni el Kanban los mueven (salvo Presentado → Cerrado).</summary>
    public static bool EsFinalOPresentado(this EstadoObligacion e) =>
        e is EstadoObligacion.Presentado or EstadoObligacion.Cerrado or EstadoObligacion.NoAplica;

    /// <summary>Columnas visibles del Kanban, en orden del ciclo de trabajo.</summary>
    public static readonly IReadOnlyList<EstadoObligacion> ColumnasKanban =
    [
        EstadoObligacion.PendienteDocumentacion, EstadoObligacion.DocumentacionCompleta, EstadoObligacion.EnPreparacion,
        EstadoObligacion.RevisionInterna, EstadoObligacion.PendienteAprobacionCliente, EstadoObligacion.Presentado, EstadoObligacion.Cerrado
    ];
}
