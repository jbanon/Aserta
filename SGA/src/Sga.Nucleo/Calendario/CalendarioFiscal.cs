using Aserta.Dominio.Catalogo;

namespace Sga.Nucleo.Calendario;

public sealed record Plazo(string Modelo, short Ejercicio, string Periodo, DateOnly Inicio, DateOnly Fin, DateOnly? Domiciliacion, bool Confirmado)
{
    public string Etiqueta => Periodo switch { "AN" => $"{Modelo} anual {Ejercicio}", _ => $"{Modelo} {Periodo} {Ejercicio}" };
}

/// <summary>
/// Plazos de 2026 y 2027 copiados del catalogo normativo de Aserta (script 0004), que a su vez
/// esta marcado [VERIFICAR] contra el calendario oficial de la AEAT: por eso todos llevan
/// Confirmado = false y la interfaz los muestra como "pendiente de confirmar".
/// Regla RD-03: si el fin cae en sabado, domingo o festivo nacional, pasa al siguiente dia habil.
/// </summary>
public static class CalendarioFiscal
{
    /// <summary>Festivos nacionales [VERIFICAR contra el calendario laboral del BOE] (mismos que Aserta).</summary>
    public static readonly IReadOnlyDictionary<DateOnly, string> Festivos = new Dictionary<DateOnly, string>
    {
        [new(2026, 1, 1)] = "Año Nuevo", [new(2026, 1, 6)] = "Epifanía del Señor", [new(2026, 4, 3)] = "Viernes Santo",
        [new(2026, 5, 1)] = "Fiesta del Trabajo", [new(2026, 8, 15)] = "Asunción de la Virgen", [new(2026, 10, 12)] = "Fiesta Nacional de España",
        [new(2026, 11, 1)] = "Todos los Santos", [new(2026, 12, 8)] = "Inmaculada Concepción", [new(2026, 12, 25)] = "Natividad del Señor",
        [new(2027, 1, 1)] = "Año Nuevo", [new(2027, 1, 6)] = "Epifanía del Señor", [new(2027, 3, 26)] = "Viernes Santo",
        [new(2027, 5, 1)] = "Fiesta del Trabajo", [new(2027, 8, 15)] = "Asunción de la Virgen", [new(2027, 10, 12)] = "Fiesta Nacional de España",
        [new(2027, 11, 1)] = "Todos los Santos", [new(2027, 12, 6)] = "Día de la Constitución", [new(2027, 12, 8)] = "Inmaculada Concepción", [new(2027, 12, 25)] = "Natividad del Señor",
    };

    public static readonly CalendarioHabil Habil = new(Festivos.Keys);

    // (modelo, periodo, offset ano inicio, mes inicio, dia inicio, offset ano fin, mes fin, dia fin (0 = ultimo del mes), dia domiciliacion o null)
    private static readonly (string Modelo, string Periodo, int OffIni, int MesIni, int DiaIni, int OffFin, int MesFin, int DiaFin, int? DiaDom)[] Patrones =
    [
        // Trimestrales de retenciones: 1-20 del mes siguiente; 4T en enero. Domiciliacion hasta el 15.
        ("111", "1T", 0, 4, 1, 0, 4, 20, 15), ("111", "2T", 0, 7, 1, 0, 7, 20, 15), ("111", "3T", 0, 10, 1, 0, 10, 20, 15), ("111", "4T", 1, 1, 1, 1, 1, 20, 15),
        ("115", "1T", 0, 4, 1, 0, 4, 20, 15), ("115", "2T", 0, 7, 1, 0, 7, 20, 15), ("115", "3T", 0, 10, 1, 0, 10, 20, 15), ("115", "4T", 1, 1, 1, 1, 1, 20, 15),
        ("123", "1T", 0, 4, 1, 0, 4, 20, 15), ("123", "2T", 0, 7, 1, 0, 7, 20, 15), ("123", "3T", 0, 10, 1, 0, 10, 20, 15), ("123", "4T", 1, 1, 1, 1, 1, 20, 15),
        // 349 informativo: sin domiciliacion; 4T hasta el 30 de enero
        ("349", "1T", 0, 4, 1, 0, 4, 20, null), ("349", "2T", 0, 7, 1, 0, 7, 20, null), ("349", "3T", 0, 10, 1, 0, 10, 20, null), ("349", "4T", 1, 1, 1, 1, 1, 30, null),
        // 303 y 130: 1T-3T hasta el 20; 4T hasta el 30 de enero (domiciliacion 25)
        ("303", "1T", 0, 4, 1, 0, 4, 20, 15), ("303", "2T", 0, 7, 1, 0, 7, 20, 15), ("303", "3T", 0, 10, 1, 0, 10, 20, 15), ("303", "4T", 1, 1, 1, 1, 1, 30, 25),
        ("130", "1T", 0, 4, 1, 0, 4, 20, 15), ("130", "2T", 0, 7, 1, 0, 7, 20, 15), ("130", "3T", 0, 10, 1, 0, 10, 20, 15), ("130", "4T", 1, 1, 1, 1, 1, 30, 25),
        // Anuales (se presentan el ano siguiente)
        ("390", "AN", 1, 1, 1, 1, 1, 30, null), ("190", "AN", 1, 1, 1, 1, 1, 0, null), ("180", "AN", 1, 1, 1, 1, 1, 0, null),
        ("347", "AN", 1, 2, 1, 1, 2, 0, null), ("200", "AN", 1, 7, 1, 1, 7, 25, 20), ("CCAA", "AN", 1, 7, 1, 1, 7, 30, null), ("LIBROS", "AN", 1, 4, 1, 1, 4, 30, null),
        ("100", "AN", 1, 4, 7, 1, 6, 30, 25),
        // 202 pagos fraccionados del Impuesto de Sociedades: abril, octubre y diciembre
        ("202", "1P", 0, 4, 1, 0, 4, 20, 15), ("202", "2P", 0, 10, 1, 0, 10, 20, 15), ("202", "3P", 0, 12, 1, 0, 12, 20, 15),
    ];

    private static readonly Lazy<IReadOnlyList<Plazo>> _plazos = new(() =>
    {
        var lista = new List<Plazo>();
        foreach (short ej in new short[] { 2025, 2026, 2027 })
            foreach (var p in Patrones)
            {
                var inicio = new DateOnly(ej + p.OffIni, p.MesIni, p.DiaIni);
                var diaFin = p.DiaFin == 0 ? DateTime.DaysInMonth(ej + p.OffFin, p.MesFin) : p.DiaFin;
                var fin = Habil.SiguienteHabil(new DateOnly(ej + p.OffFin, p.MesFin, diaFin));
                DateOnly? dom = p.DiaDom is int d ? Habil.SiguienteHabil(new DateOnly(ej + p.OffFin, p.MesFin, d)) : null;
                lista.Add(new Plazo(p.Modelo, ej, p.Periodo, inicio, fin, dom, Confirmado: false));
            }
        return lista.OrderBy(p => p.Fin).ThenBy(p => p.Modelo).ToList();
    });

    public static IReadOnlyList<Plazo> Plazos => _plazos.Value;

    public static Plazo? Buscar(string modelo, short ejercicio, string periodo) =>
        Plazos.FirstOrDefault(p => p.Modelo == modelo && p.Ejercicio == ejercicio && p.Periodo == periodo);

    /// <summary>Trimestre en campana para una fecha: el que tiene el plazo de presentacion abierto; si no hay ninguno, el trimestre natural en curso.</summary>
    public static (short Ejercicio, string Periodo) TrimestreEnCampana(DateOnly hoy)
    {
        var abierto = Plazos.Where(p => p.Modelo == "303" && p.Periodo.EndsWith('T') && p.Inicio <= hoy && p.Fin >= hoy).OrderBy(p => p.Fin).FirstOrDefault();
        if (abierto is not null) return (abierto.Ejercicio, abierto.Periodo);
        return ((short)hoy.Year, $"{(hoy.Month - 1) / 3 + 1}T");
    }

    public static string NombrePeriodo(string periodo) => periodo switch
    {
        "1T" => "primer trimestre", "2T" => "segundo trimestre", "3T" => "tercer trimestre", "4T" => "cuarto trimestre",
        "1P" => "primer pago fraccionado", "2P" => "segundo pago fraccionado", "3P" => "tercer pago fraccionado", "AN" => "anual", _ => periodo,
    };
}
