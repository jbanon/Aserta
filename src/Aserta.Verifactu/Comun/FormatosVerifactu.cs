using System.Globalization;

namespace Aserta.Verifactu.Comun;

/// <summary>
/// Formatos de texto exigidos por las especificaciones de la AEAT. TODA conversion
/// de numero o fecha de este modulo pasa por aqui con InvariantCulture explicita
/// (huella-y-vectores-prueba.md §3: el fallo del locale es el mas silencioso).
/// </summary>
public static class FormatosVerifactu
{
    public const string VersionEspecHuella = "0.1.2 (27/08/2024)";
    public const string VersionEspecQr = "0.5.0";

    /// <summary>Fecha de expedicion: DD-MM-AAAA (no ISO).</summary>
    public static string Fecha(DateOnly f) => f.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    /// <summary>Importes: siempre dos decimales y punto (H3 / Q6).</summary>
    public static string Importe(decimal d) => d.ToString("F2", CultureInfo.InvariantCulture);

    /// <summary>Instante de generacion del registro: ISO 8601 con desfase, sin normalizar a UTC (ej. 2024-01-01T19:20:30+01:00).</summary>
    public static string FechaHoraHuso(DateTimeOffset t) => t.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

    public static DateOnly ParsearFecha(string ddMMyyyy) => DateOnly.ParseExact(ddMMyyyy, "dd-MM-yyyy", CultureInfo.InvariantCulture);
}
