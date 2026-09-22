using System.Security.Cryptography;
using System.Text;

namespace Aserta.Verifactu.Huella;

/// <summary>Datos que entran en la huella de un registro de alta (los 8 campos, en orden).</summary>
public sealed record DatosHuellaAlta(
    string IdEmisorFactura,
    string NumSerieFactura,
    string FechaExpedicionFactura,      // DD-MM-AAAA
    string TipoFactura,                 // F1, F2, R1...
    string CuotaTotal,                  // texto ya formateado (F2, punto decimal)
    string ImporteTotal,
    string? HuellaAnterior,             // null o vacio en el primer registro
    string FechaHoraHusoGenRegistro);   // ISO 8601 con huso

/// <summary>Datos que entran en la huella de un registro de anulacion (5 campos).</summary>
public sealed record DatosHuellaAnulacion(
    string IdEmisorFacturaAnulada,
    string NumSerieFacturaAnulada,
    string FechaExpedicionFacturaAnulada,
    string? HuellaAnterior,
    string FechaHoraHusoGenRegistro);

/// <summary>
/// Huella SHA-256 encadenada (AEAT, especificacion de huella v0.1.2). Se separa la
/// construccion de la cadena del calculo del hash para que los tests comparen la
/// cadena y el error diga que caracter sobra, no solo "el hash no coincide".
/// </summary>
public static class CalculadoraHuella
{
    public static string ConstruirCadenaAlta(DatosHuellaAlta d) =>
        $"IDEmisorFactura={T(d.IdEmisorFactura)}&NumSerieFactura={T(d.NumSerieFactura)}&FechaExpedicionFactura={T(d.FechaExpedicionFactura)}" +
        $"&TipoFactura={T(d.TipoFactura)}&CuotaTotal={T(d.CuotaTotal)}&ImporteTotal={T(d.ImporteTotal)}" +
        $"&Huella={T(d.HuellaAnterior)}&FechaHoraHusoGenRegistro={T(d.FechaHoraHusoGenRegistro)}";

    public static string ConstruirCadenaAnulacion(DatosHuellaAnulacion d) =>
        $"IDEmisorFacturaAnulada={T(d.IdEmisorFacturaAnulada)}&NumSerieFacturaAnulada={T(d.NumSerieFacturaAnulada)}" +
        $"&FechaExpedicionFacturaAnulada={T(d.FechaExpedicionFacturaAnulada)}&Huella={T(d.HuellaAnterior)}&FechaHoraHusoGenRegistro={T(d.FechaHoraHusoGenRegistro)}";

    /// <summary>SHA-256 sobre UTF-8 (sin BOM), hexadecimal en MAYUSCULAS, 64 caracteres.</summary>
    public static string CalcularHuella(string cadena) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cadena)));

    public static string HuellaAlta(DatosHuellaAlta d) => CalcularHuella(ConstruirCadenaAlta(d));
    public static string HuellaAnulacion(DatosHuellaAnulacion d) => CalcularHuella(ConstruirCadenaAnulacion(d));

    /// <summary>Valor: mismo contenido que el XML, sin espacios al inicio y al final. Campo ausente = nombre y '=' sin valor.</summary>
    private static string T(string? v) => (v ?? string.Empty).Trim();
}
