using System.ComponentModel.DataAnnotations;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Infraestructura.Identidad;
using Aserta.Infraestructura.Persistencia;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aserta.Web.Pages.Cuenta.Mfa;

[AllowAnonymous]
public class VerificarModel : PaginaBase
{
    private readonly SignInManager<UsuarioIdentity> _signIn;
    private readonly IRegistroAuditoria _auditoria;
    private readonly ContextoEjecucion _contexto;

    public VerificarModel(SignInManager<UsuarioIdentity> signIn, IRegistroAuditoria auditoria, ContextoEjecucion contexto)
    {
        _signIn = signIn;
        _auditoria = auditoria;
        _contexto = contexto;
    }

    [BindProperty, Required(ErrorMessage = "Introduzca el código.")] public string Codigo { get; set; } = "";
    [BindProperty] public bool RecordarEquipo { get; set; }
    [BindProperty(SupportsGet = true)] public string? Volver { get; set; }
    [BindProperty(SupportsGet = true)] public bool Recordar { get; set; }
    [BindProperty(SupportsGet = true)] public bool UsarRecuperacion { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (await _signIn.GetTwoFactorAuthenticationUserAsync() is null) return RedirectToPage("/Cuenta/Entrar");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var usuario = await _signIn.GetTwoFactorAuthenticationUserAsync();
        if (usuario is null) return RedirectToPage("/Cuenta/Entrar");
        if (!ModelState.IsValid) return Page();

        // Los codigos TOTP se escriben a veces con espacios o guion; los de recuperacion llevan guion propio.
        var r = UsarRecuperacion
            ? await _signIn.TwoFactorRecoveryCodeSignInAsync(Codigo.Trim())
            : await _signIn.TwoFactorAuthenticatorSignInAsync(Codigo.Replace(" ", "").Replace("-", ""), Recordar, RecordarEquipo);

        if (r.Succeeded)
        {
            var claims = await _signIn.ClaimsFactory.CreateAsync(usuario);
            var gestoria = Guid.Parse(claims.FindFirst(ClaimsAserta.GestoriaId)!.Value);
            using (_contexto.AbrirAmbitoTenant(gestoria))
                await _auditoria.RegistrarYGuardarAsync(AccionesAuditoria.InicioSesion, nameof(Usuario), usuario.Id.ToString(), new { mfa = true, recuperacion = UsarRecuperacion }, gestoria, usuario.Id);
            return LocalRedirect(string.IsNullOrEmpty(Volver) || !Url.IsLocalUrl(Volver) ? "/" : Volver);
        }
        if (r.IsLockedOut) ModelState.AddModelError(string.Empty, "Demasiados intentos. La cuenta queda bloqueada unos minutos.");
        else ModelState.AddModelError(nameof(Codigo), "Código no válido.");
        return Page();
    }
}
