namespace Aserta.Aplicacion.Puertos;

public sealed record UsuarioNuevo(string Email, string Contrasena, string NombreCompleto, Guid GestoriaId, Guid? ClienteId, IReadOnlyList<string> Roles, bool MfaObligatorio = false);

public sealed record ResultadoIdentidad(bool Exito, Guid? UsuarioId, IReadOnlyList<string> Errores)
{
    public static ResultadoIdentidad Ok(Guid id) => new(true, id, []);
    public static ResultadoIdentidad Fallo(params string[] errores) => new(false, null, errores);
}

/// <summary>Puerto hacia ASP.NET Core Identity: la capa de aplicacion no conoce UserManager.</summary>
public interface IGestorIdentidad
{
    Task<ResultadoIdentidad> CrearUsuarioAsync(UsuarioNuevo datos, CancellationToken ct = default);
    Task<bool> ExisteEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<string>> RolesDeAsync(Guid usuarioId, CancellationToken ct = default);
    Task<ResultadoIdentidad> CambiarRolesAsync(Guid usuarioId, IReadOnlyList<string> roles, CancellationToken ct = default);
    Task<ResultadoIdentidad> RestablecerContrasenaAsync(Guid usuarioId, string nuevaContrasena, CancellationToken ct = default);
    Task<string?> EmailDeAsync(Guid usuarioId, CancellationToken ct = default);
}
