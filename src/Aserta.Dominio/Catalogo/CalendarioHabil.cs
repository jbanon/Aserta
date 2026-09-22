namespace Aserta.Dominio.Catalogo;

/// <summary>
/// RD-03: un plazo que cae en sabado, domingo o dia inhabil se traslada al
/// siguiente dia habil. Misma logica que cat.fn_SiguienteDiaHabil en SQL.
/// </summary>
public sealed class CalendarioHabil
{
    private readonly HashSet<DateOnly> _inhabiles;

    public CalendarioHabil(IEnumerable<DateOnly> diasInhabiles)
    {
        _inhabiles = [.. diasInhabiles];
    }

    public bool EsHabil(DateOnly fecha) =>
        fecha.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_inhabiles.Contains(fecha);

    public DateOnly SiguienteHabil(DateOnly fecha)
    {
        var f = fecha;
        for (int i = 0; i < 30 && !EsHabil(f); i++) f = f.AddDays(1);
        return f;
    }
}
