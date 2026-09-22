namespace Aserta.Aplicacion.Puertos;

/// <summary>
/// Quien esta ejecutando y en nombre de que gestoria. Lo rellena el middleware
/// web desde los claims; los trabajos de fondo abren un ambito por tenant; el
/// runner y el seed abren un ambito de mantenimiento (ADR-001 §2.4).
/// </summary>
public interface IContextoTenant
{
    Guid? GestoriaId { get; }
    bool EsMantenimiento { get; }

    /// <summary>Ambito temporal en el que RLS y los filtros EF dejan ver todos los tenants. Solo runner, seed y login.</summary>
    IDisposable AbrirAmbitoMantenimiento();

    /// <summary>Ambito temporal para actuar en nombre de un tenant concreto (trabajos de fondo).</summary>
    IDisposable AbrirAmbitoTenant(Guid gestoriaId);
}

/// <summary>El usuario que actua, para auditoria (RD-08) y autorizacion.</summary>
public interface IContextoUsuarioActual
{
    bool EstaAutenticado { get; }
    Guid? UsuarioId { get; }
    Guid? GestoriaId { get; }
    Guid? ClienteId { get; }
    string? NombreCompleto { get; }
    IReadOnlyCollection<string> Roles { get; }
    string? DireccionIp { get; }
    string? AgenteUsuario { get; }

    bool TieneRol(string rol);
}
