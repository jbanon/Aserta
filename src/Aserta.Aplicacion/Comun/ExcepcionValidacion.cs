namespace Aserta.Aplicacion.Comun;

/// <summary>Errores de validacion de un caso de uso, por campo, listos para volcarlos en ModelState.</summary>
public class ExcepcionValidacion : Exception
{
    public IReadOnlyDictionary<string, string[]> Errores { get; }

    public ExcepcionValidacion(IReadOnlyDictionary<string, string[]> errores)
        : base(string.Join(" ", errores.SelectMany(e => e.Value)))
    {
        Errores = errores;
    }

    public ExcepcionValidacion(string campo, string mensaje)
        : this(new Dictionary<string, string[]> { [campo] = [mensaje] }) { }
}

public class ExcepcionNoEncontrado : Exception
{
    public ExcepcionNoEncontrado(string entidad, object id) : base($"{entidad} «{id}» no existe o no pertenece a esta gestoría.") { }
}

public class ExcepcionNoAutorizado : Exception
{
    public ExcepcionNoAutorizado(string mensaje) : base(mensaje) { }
}
