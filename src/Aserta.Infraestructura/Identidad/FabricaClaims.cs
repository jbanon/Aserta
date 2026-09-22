using System.Security.Claims;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Aserta.Infraestructura.Identidad;

/// <summary>Nombres de los claims propios que viajan en la cookie.</summary>
public static class ClaimsAserta
{
    public const string GestoriaId = "aserta:gestoria";
    public const string ClienteId = "aserta:cliente";
    public const string NombreCompleto = "aserta:nombre";
    public const string MfaObligatorio = "aserta:mfa";
    public const string MfaActivo = "aserta:mfa-activo";
}

/// <summary>
/// Al iniciar sesion anade a la identidad el tenant, el cliente y el nombre desde
/// dbo.Usuario. Es el UNICO sitio donde se consulta un usuario sin tenant en el
/// contexto: se hace en ambito de mantenimiento y de forma explicita.
/// </summary>
public sealed class FabricaClaims : UserClaimsPrincipalFactory<UsuarioIdentity, RolIdentity>
{
    private readonly IAsertaDb _db;
    private readonly IContextoTenant _tenant;

    public FabricaClaims(UserManager<UsuarioIdentity> userManager, RoleManager<RolIdentity> roleManager, IOptions<IdentityOptions> options, IAsertaDb db, IContextoTenant tenant)
        : base(userManager, roleManager, options)
    {
        _db = db;
        _tenant = tenant;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(UsuarioIdentity user)
    {
        var identidad = await base.GenerateClaimsAsync(user);
        Usuario? u;
        using (_tenant.AbrirAmbitoMantenimiento())
        {
            u = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        }
        if (u is null) return identidad;

        identidad.AddClaim(new Claim(ClaimsAserta.GestoriaId, u.GestoriaId.ToString()));
        if (u.ClienteId is Guid c) identidad.AddClaim(new Claim(ClaimsAserta.ClienteId, c.ToString()));
        identidad.AddClaim(new Claim(ClaimsAserta.NombreCompleto, u.NombreCompleto));
        identidad.AddClaim(new Claim(ClaimsAserta.MfaObligatorio, u.MfaObligatorio ? "1" : "0"));
        identidad.AddClaim(new Claim(ClaimsAserta.MfaActivo, user.TwoFactorEnabled ? "1" : "0"));
        return identidad;
    }
}
