using Aserta.Aplicacion.Puertos;

namespace Aserta.Infraestructura.Tiempo;

/// <summary>Reloj real. En la demo admite un desplazamiento configurable (Demo:DesplazamientoDias) para "viajar" a abril.</summary>
public sealed class RelojSistema : IRelojSistema
{
    private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
    private readonly int _desplazamientoDias;

    public RelojSistema(int desplazamientoDias = 0) => _desplazamientoDias = desplazamientoDias;

    public DateTime AhoraUtc => DateTime.UtcNow.AddDays(_desplazamientoDias);
    public DateOnly Hoy => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(AhoraUtc, Madrid));
}
