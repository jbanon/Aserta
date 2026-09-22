namespace Aserta.Dominio.Comun;

/// <summary>Violacion de una regla de dominio. El mensaje es apto para mostrar al usuario.</summary>
public class ExcepcionDominio : Exception
{
    public ExcepcionDominio(string mensaje) : base(mensaje) { }
}
