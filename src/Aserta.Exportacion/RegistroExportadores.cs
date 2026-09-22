using Aserta.Dominio.Exportacion;

namespace Aserta.Exportacion;

/// <summary>Catalogo de exportadores. Web lo registra en DI; Aplicacion lo consume por el puerto.</summary>
public static class RegistroExportadores
{
    public static IReadOnlyList<IExportadorContable> Todos() => [new ExportadorCsvGenerico(), new ExportadorA3()];
}
