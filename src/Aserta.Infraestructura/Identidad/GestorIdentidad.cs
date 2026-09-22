using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Microsoft.AspNetCore.Identity;

namespace Aserta.Infraestructura.Identidad;

/// <summary>Adaptador del puerto IGestorIdentidad sobre UserManager. Crea AspNetUsers + dbo.Usuario en una sola operacion logica.</summary>
public sealed class GestorIdentidad : IGestorIdentidad
{
    private readonly UserManager<UsuarioIdentity> _usuarios;
    private readonly IAsertaDb _db;

    public GestorIdentidad(UserManager<UsuarioIdentity> usuarios, IAsertaDb db)
    {
        _usuarios = usuarios;
        _db = db;
    }

    public async Task<ResultadoIdentidad> CrearUsuarioAsync(UsuarioNuevo datos, CancellationToken ct = default)
    {
        var identidad = new UsuarioIdentity
        {
            Id = Guid.NewGuid(),
            UserName = datos.Email,
            Email = datos.Email,
            EmailConfirmed = true,
            LockoutEnabled = true,
        };
        var creado = await _usuarios.CreateAsync(identidad, datos.Contrasena);
        if (!creado.Succeeded)
            return ResultadoIdentidad.Fallo(creado.Errores());

        var roles = await _usuarios.AddToRolesAsync(identidad, datos.Roles);
        if (!roles.Succeeded)
        {
            await _usuarios.DeleteAsync(identidad);
            return ResultadoIdentidad.Fallo(roles.Errores());
        }

        _db.Usuarios.Add(new Usuario
        {
            Id = identidad.Id,
            GestoriaId = datos.GestoriaId,
            ClienteId = datos.ClienteId,
            NombreCompleto = datos.NombreCompleto,
            Estado = EstadoUsuario.Activo,
            MfaObligatorio = datos.MfaObligatorio,
        });
        await _db.GuardarCambiosAsync(ct);
        return ResultadoIdentidad.Ok(identidad.Id);
    }

    public async Task<bool> ExisteEmailAsync(string email, CancellationToken ct = default) =>
        await _usuarios.FindByEmailAsync(email) is not null;

    public async Task<IReadOnlyList<string>> RolesDeAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var u = await _usuarios.FindByIdAsync(usuarioId.ToString());
        return u is null ? [] : (await _usuarios.GetRolesAsync(u)).ToList();
    }

    public async Task<ResultadoIdentidad> CambiarRolesAsync(Guid usuarioId, IReadOnlyList<string> roles, CancellationToken ct = default)
    {
        var u = await _usuarios.FindByIdAsync(usuarioId.ToString());
        if (u is null) return ResultadoIdentidad.Fallo("Usuario no encontrado.");
        var actuales = await _usuarios.GetRolesAsync(u);
        var quitar = await _usuarios.RemoveFromRolesAsync(u, actuales.Except(roles));
        if (!quitar.Succeeded) return ResultadoIdentidad.Fallo(quitar.Errores());
        var poner = await _usuarios.AddToRolesAsync(u, roles.Except(actuales));
        return poner.Succeeded ? ResultadoIdentidad.Ok(usuarioId) : ResultadoIdentidad.Fallo(poner.Errores());
    }

    public async Task<ResultadoIdentidad> RestablecerContrasenaAsync(Guid usuarioId, string nuevaContrasena, CancellationToken ct = default)
    {
        var u = await _usuarios.FindByIdAsync(usuarioId.ToString());
        if (u is null) return ResultadoIdentidad.Fallo("Usuario no encontrado.");
        var token = await _usuarios.GeneratePasswordResetTokenAsync(u);
        var r = await _usuarios.ResetPasswordAsync(u, token, nuevaContrasena);
        return r.Succeeded ? ResultadoIdentidad.Ok(usuarioId) : ResultadoIdentidad.Fallo(r.Errores());
    }

    public async Task<string?> EmailDeAsync(Guid usuarioId, CancellationToken ct = default) =>
        (await _usuarios.FindByIdAsync(usuarioId.ToString()))?.Email;
}

internal static class IdentityResultExtensiones
{
    public static string[] Errores(this IdentityResult r) => r.Errors.Select(e => e.Description).ToArray();
}
