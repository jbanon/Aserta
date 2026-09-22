namespace Aserta.Dominio.Catalogo;

/// <summary>
/// Codigos de periodo dentro de un ejercicio: 1T..4T, 01..12, AN y 1P..3P (pagos
/// fraccionados del Impuesto sobre Sociedades, modelo 202: abril, octubre y diciembre).
/// Un periodo tiene un rango de fechas civiles que se usa para (a) decidir que
/// perfil fiscal estaba vigente y (b) comprobar que el cliente estaba de alta (RD-10).
/// </summary>
public static class Periodo
{
    public static readonly IReadOnlyList<string> Trimestres = ["1T", "2T", "3T", "4T"];
    public static readonly IReadOnlyList<string> Meses = ["01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12"];
    public static readonly IReadOnlyList<string> PagosFraccionados = ["1P", "2P", "3P"];
    public const string Anual = "AN";

    public static IReadOnlyList<string> DePeriodicidad(Periodicidad periodicidad) => periodicidad switch
    {
        Periodicidad.Trimestral => Trimestres,
        Periodicidad.Mensual => Meses,
        Periodicidad.Anual => [Anual],
        Periodicidad.PagoFraccionado => PagosFraccionados,
        _ => throw new ArgumentOutOfRangeException(nameof(periodicidad))
    };

    public static bool EsValido(string periodo) =>
        Trimestres.Contains(periodo) || Meses.Contains(periodo) || PagosFraccionados.Contains(periodo) || periodo == Anual;

    /// <summary>Rango de fechas civiles que abarca el periodo dentro del ejercicio.</summary>
    public static (DateOnly Inicio, DateOnly Fin) Rango(int ejercicio, string periodo)
    {
        switch (periodo)
        {
            case "1T": return (new DateOnly(ejercicio, 1, 1), new DateOnly(ejercicio, 3, 31));
            case "2T": return (new DateOnly(ejercicio, 4, 1), new DateOnly(ejercicio, 6, 30));
            case "3T": return (new DateOnly(ejercicio, 7, 1), new DateOnly(ejercicio, 9, 30));
            case "4T": return (new DateOnly(ejercicio, 10, 1), new DateOnly(ejercicio, 12, 31));
            case "AN": return (new DateOnly(ejercicio, 1, 1), new DateOnly(ejercicio, 12, 31));
            // 202: 1P (abril) liquida ene-mar; 2P (octubre) abr-sep; 3P (diciembre) oct-nov [VERIFICAR]
            case "1P": return (new DateOnly(ejercicio, 1, 1), new DateOnly(ejercicio, 3, 31));
            case "2P": return (new DateOnly(ejercicio, 4, 1), new DateOnly(ejercicio, 9, 30));
            case "3P": return (new DateOnly(ejercicio, 10, 1), new DateOnly(ejercicio, 11, 30));
        }
        if (periodo.Length == 2 && int.TryParse(periodo, out int mes) && mes is >= 1 and <= 12)
        {
            var inicio = new DateOnly(ejercicio, mes, 1);
            return (inicio, inicio.AddMonths(1).AddDays(-1));
        }
        throw new ArgumentException($"Periodo desconocido: '{periodo}'.", nameof(periodo));
    }

    /// <summary>Texto para la interfaz: "3T 2026", "julio 2026", "2026".</summary>
    public static string Etiqueta(int ejercicio, string periodo)
    {
        if (periodo == Anual) return $"Anual {ejercicio}";
        if (Trimestres.Contains(periodo)) return $"{periodo} {ejercicio}";
        if (PagosFraccionados.Contains(periodo)) return $"Pago fraccionado {periodo[0]} · {ejercicio}";
        if (int.TryParse(periodo, out int mes))
            return $"{System.Globalization.CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetMonthName(mes)} {ejercicio}";
        return $"{periodo} {ejercicio}";
    }

    /// <summary>Orden natural dentro del ejercicio para listados.</summary>
    public static int Orden(string periodo) => periodo switch
    {
        "1T" => 3, "2T" => 6, "3T" => 9, "4T" => 12, "AN" => 13,
        "1P" => 4, "2P" => 10, "3P" => 12,
        _ => int.TryParse(periodo, out int m) ? m : 99
    };
}
