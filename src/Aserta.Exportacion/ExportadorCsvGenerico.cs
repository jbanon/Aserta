using System.Globalization;
using System.Text;
using Aserta.Dominio.Exportacion;

namespace Aserta.Exportacion;

/// <summary>
/// Formato propio, documentado y estable (DA-07): CSV UTF-8 con BOM, separador ';',
/// decimales con coma (Excel espanol), una fila por apunte, ordenado por fecha y
/// numero. Cabecera fija: cualquier cambio es una version nueva del formato.
/// </summary>
public sealed class ExportadorCsvGenerico : IExportadorContable
{
    public const string VersionFormato = "1";
    public static readonly string[] Cabecera =
        ["Tipo", "Fecha", "NumeroFactura", "NifContraparte", "NombreContraparte", "BaseImponible", "TipoIva", "CuotaIva", "CuotaRecargo", "Retencion", "Total", "Concepto", "Origen", "DatosConfirmados", "Referencia"];

    public string Formato => "CsvGenerico";
    public string Nombre => "CSV genérico Aserta";
    public string Descripcion => $"Formato propio v{VersionFormato}: UTF-8 con BOM, separador ';', decimales con coma. Importable en cualquier programa contable con asistente de importación.";
    public bool Disponible => true;

    public FicheroExportado Exportar(IReadOnlyList<ApunteExportable> apuntes, string nifCliente, int ejercicio, string periodo)
    {
        var es = CultureInfo.GetCultureInfo("es-ES");
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', Cabecera));
        foreach (var a in apuntes.OrderBy(a => a.Fecha).ThenBy(a => a.NumeroFactura, StringComparer.Ordinal))
        {
            sb.AppendLine(string.Join(';',
                a.Tipo == TipoApunte.FacturaEmitida ? "Emitida" : "Recibida",
                a.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                Campo(a.NumeroFactura), Campo(a.NifContraparte ?? ""), Campo(a.NombreContraparte),
                Num(a.BaseImponible, es), Num(a.TipoIva, es), Num(a.CuotaIva, es), Num(a.CuotaRecargo, es), Num(a.Retencion, es), Num(a.Total, es),
                Campo(a.Concepto), Campo(a.Origen), a.DatosConfirmados ? "S" : "N", a.ReferenciaId.ToString("N")));
        }
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return new FicheroExportado($"aserta-{nifCliente}-{ejercicio}-{periodo}-v{VersionFormato}.csv", "text/csv; charset=utf-8", bytes);
    }

    private static string Num(decimal d, CultureInfo es) => d.ToString("0.00", es);
    private static string Campo(string s) => s.Contains(';') || s.Contains('"') || s.Contains('\n') ? "\"" + s.Replace("\"", "\"\"").Replace("\n", " ") + "\"" : s;
}
