using System.Globalization;
using Aserta.Verifactu.Comun;

namespace Aserta.Verifactu.Qr;

/// <summary>Los dos entornos de la AEAT. Una sola bandera para QR y servicio web (Q4).</summary>
public enum EntornoAeat { Pruebas, Produccion }

/// <summary>Configuracion del QR: URLs por entorno (qr-y-pdf.md §3.1, tomadas del PDF oficial 0.5.0). Van en configuracion, nunca en codigo.</summary>
public sealed class OpcionesQr
{
    public EntornoAeat Entorno { get; set; } = EntornoAeat.Pruebas;
    public string UrlPruebas { get; set; } = "https://prewww2.aeat.es/wlpl/TIKE-CONT/ValidarQR";
    public string UrlProduccion { get; set; } = "https://www2.agenciatributaria.gob.es/wlpl/TIKE-CONT/ValidarQR";
    public string UrlBase => Entorno == EntornoAeat.Produccion ? UrlProduccion : UrlPruebas;
}

/// <summary>
/// URL del servicio de cotejo que va dentro del QR (qr-y-pdf.md §3.2): exactamente
/// cuatro parametros, en este orden, con URL encoding UTF-8. El numserie se codifica
/// aqui y NO en la huella (trampa Q3).
/// </summary>
public static class ConstructorUrlQr
{
    public const int LongitudMaximaNumSerie = 60;
    public const int DigitosEnterosMaximosImporte = 12;

    public static string ConstruirUrlQr(OpcionesQr opciones, string nif, string numSerie, DateOnly fecha, decimal importeTotal)
    {
        Validar(nif, numSerie, importeTotal);
        return $"{opciones.UrlBase}?nif={Uri.EscapeDataString(nif.Trim())}&numserie={Uri.EscapeDataString(numSerie.Trim())}" +
               $"&fecha={FormatosVerifactu.Fecha(fecha)}&importe={FormatosVerifactu.Importe(importeTotal)}";
    }

    /// <summary>Solo para nuestro cotejo automatizado. NUNCA en el QR de la factura (formato=json esta prohibido ahi).</summary>
    public static string ConstruirUrlCotejoJson(OpcionesQr opciones, string nif, string numSerie, DateOnly fecha, decimal importeTotal, string idioma = "es") =>
        ConstruirUrlQr(opciones, nif, numSerie, fecha, importeTotal) + $"&idioma={idioma}&formato=json";

    public static void Validar(string nif, string numSerie, decimal importeTotal)
    {
        if (string.IsNullOrWhiteSpace(nif) || nif.Trim().Length != 9) throw new ArgumentException("El NIF del emisor debe tener 9 caracteres.", nameof(nif));
        if (string.IsNullOrWhiteSpace(numSerie)) throw new ArgumentException("El número de serie y factura es obligatorio.", nameof(numSerie));
        if (numSerie.Trim().Length > LongitudMaximaNumSerie) throw new ArgumentException($"El número de serie y factura no puede superar {LongitudMaximaNumSerie} caracteres (límite del QR).", nameof(numSerie));
        if (numSerie.Any(c => c < 32 || c > 126)) throw new ArgumentException("El número de serie solo admite caracteres ASCII 32-126.", nameof(numSerie));
        var entero = Math.Truncate(Math.Abs(importeTotal)).ToString(CultureInfo.InvariantCulture);
        if (entero.Length > DigitosEnterosMaximosImporte) throw new ArgumentException("El importe total supera los 12 dígitos enteros que admite el QR.", nameof(importeTotal));
    }
}
