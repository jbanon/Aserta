namespace Aserta.Aplicacion.Puertos;

/// <summary>Reloj inyectable: los tests fijan la fecha; la demo puede "viajar en el tiempo".</summary>
public interface IRelojSistema
{
    DateTime AhoraUtc { get; }
    /// <summary>Fecha civil de hoy en la zona horaria de la gestoria (Europe/Madrid por defecto).</summary>
    DateOnly Hoy { get; }
}
