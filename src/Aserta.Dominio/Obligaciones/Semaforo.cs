namespace Aserta.Dominio.Obligaciones;

public enum ColorSemaforo { Gris, Verde, Ambar, Rojo, Vencido }

/// <summary>
/// RD-02: el semaforo se calcula sobre la fecha limite de domiciliacion (la que
/// de verdad aprieta) y no es una columna. Si el modelo no tiene domiciliacion
/// (informativos), se usa la de presentacion.
/// </summary>
public static class Semaforo
{
    public const int DiasAmbar = 7;
    public const int DiasRojo = 3;

    public static DateOnly FechaDeReferencia(Obligacion o) => o.FechaLimiteDomiciliacion ?? o.FechaLimitePresentacion;

    public static ColorSemaforo Calcular(Obligacion o, DateOnly hoy)
    {
        if (o.Estado.EsFinalOPresentado()) return ColorSemaforo.Gris;
        int dias = FechaDeReferencia(o).DayNumber - hoy.DayNumber;
        if (dias < 0) return ColorSemaforo.Vencido;
        if (dias <= DiasRojo) return ColorSemaforo.Rojo;
        if (dias <= DiasAmbar) return ColorSemaforo.Ambar;
        return ColorSemaforo.Verde;
    }

    public static int DiasRestantes(Obligacion o, DateOnly hoy) => FechaDeReferencia(o).DayNumber - hoy.DayNumber;
}
