using Aserta.Dominio.Exportacion;

namespace Aserta.Exportacion;

/// <summary>
/// DA-07: el formato de importacion de A3 depende de la version del producto y no
/// es publico de forma fiable. Inventarlo seria peor que no tenerlo: el exportador
/// existe (el contrato, la seleccion en pantalla) pero NO esta disponible hasta
/// disponer de la especificacion oficial del fabricante [VERIFICAR].
/// </summary>
public sealed class ExportadorA3 : IExportadorContable
{
    public string Formato => "A3";
    public string Nombre => "A3 (Wolters Kluwer)";
    public string Descripcion => "Pendiente de la especificación oficial del fabricante [VERIFICAR]. No se genera ningún fichero hasta disponer de ella.";
    public bool Disponible => false;

    public FicheroExportado Exportar(IReadOnlyList<ApunteExportable> apuntes, string nifCliente, int ejercicio, string periodo) =>
        throw new NotSupportedException("El formato A3 no está disponible: falta la especificación oficial del fabricante (DA-07).");
}
