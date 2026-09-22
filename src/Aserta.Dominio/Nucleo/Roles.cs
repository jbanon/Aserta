namespace Aserta.Dominio.Nucleo;

/// <summary>Roles tecnicos (01-mapa-dominio.md §1). Los nombres son los de las tablas de Identity.</summary>
public static class Roles
{
    public const string SocioDirector = "SocioDirector";
    public const string Asesor = "Asesor";
    public const string Administrativo = "Administrativo";
    public const string ClienteAdmin = "ClienteAdmin";
    public const string ClienteUsuario = "ClienteUsuario";

    public static readonly IReadOnlyList<string> Todos = [SocioDirector, Asesor, Administrativo, ClienteAdmin, ClienteUsuario];
    public static readonly IReadOnlyList<string> DeGestoria = [SocioDirector, Asesor, Administrativo];
    public static readonly IReadOnlyList<string> DeCliente = [ClienteAdmin, ClienteUsuario];

    public static bool EsDeGestoria(string rol) => DeGestoria.Contains(rol);
    public static bool EsDeCliente(string rol) => DeCliente.Contains(rol);
}
