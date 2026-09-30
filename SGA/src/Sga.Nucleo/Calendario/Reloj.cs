namespace Sga.Nucleo.Calendario;

/// <summary>La demo usa un reloj fijo (por defecto 6 de octubre de 2026, en plena campana del 3T) para que se vea igual cada vez que se ensena.</summary>
public interface IReloj
{
    DateOnly Hoy { get; }
    DateTime AhoraUtc { get; }
}

public sealed class RelojFijo(DateOnly hoy) : IReloj
{
    public DateOnly Hoy { get; } = hoy;
    public DateTime AhoraUtc => hoy.ToDateTime(new TimeOnly(10, 30), DateTimeKind.Utc);
}

public sealed class RelojReal : IReloj
{
    public DateOnly Hoy => DateOnly.FromDateTime(DateTime.Now);
    public DateTime AhoraUtc => DateTime.UtcNow;
}
