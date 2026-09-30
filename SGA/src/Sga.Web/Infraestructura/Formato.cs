using System.Globalization;

namespace Sga.Web.Infraestructura;

/// <summary>Formatos en espanol para importes y fechas, pensados para leerse de un vistazo.</summary>
public static class Formato
{
    public static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");
    public static string Euros(decimal v) => v.ToString("N2", Es) + " €";
    public static string EurosSinDecimales(decimal v) => v.ToString("N0", Es) + " €";
    public static string Numero(decimal v) => v.ToString("N2", Es);
    public static string Porcentaje(decimal v) => v.ToString("0.##", Es) + " %";
    public static string Fecha(DateOnly f) => f.ToString("d 'de' MMMM", Es);
    public static string FechaCorta(DateOnly f) => f.ToString("dd/MM/yyyy", Es);
    public static string FechaLarga(DateOnly f) => f.ToString("dddd d 'de' MMMM 'de' yyyy", Es);
    public static string Mes(DateOnly f) => Es.TextInfo.ToTitleCase(f.ToString("MMMM yyyy", Es));
    public static string MesCorto(int mes) => Es.DateTimeFormat.GetAbbreviatedMonthName(mes).TrimEnd('.');
    public static string NombreMes(int mes) => Es.DateTimeFormat.GetMonthName(mes);
    public static string FechaHora(DateTime utc) => utc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Es);
    public static string Relativa(DateTime utc, DateTime ahoraUtc)
    {
        var d = ahoraUtc - utc;
        if (d.TotalMinutes < 1) return "ahora mismo";
        if (d.TotalHours < 1) return $"hace {(int)d.TotalMinutes} min";
        if (d.TotalDays < 1) return $"hace {(int)d.TotalHours} h";
        if (d.TotalDays < 7) return $"hace {(int)d.TotalDays} días";
        return FechaCorta(DateOnly.FromDateTime(utc.ToLocalTime()));
    }
    public static string Iban(string? iban) => string.IsNullOrEmpty(iban) ? "—" : iban.Length > 8 ? $"{iban[..4]} •••• •••• {iban[^4..]}" : iban;
    public static string Bytes(long b) => b < 1024 ? $"{b} B" : b < 1024 * 1024 ? $"{b / 1024.0:0} KB" : $"{b / 1024.0 / 1024.0:0.0} MB";
}
