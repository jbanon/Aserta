using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Infraestructura.Identidad;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aserta.Web.Pages.Cuenta;

public class PerfilModel : PaginaBase
{
    private readonly UserManager<UsuarioIdentity> _usuarios;
    private readonly SignInManager<UsuarioIdentity> _signIn;
    private readonly IContextoUsuarioActual _actual;
    private readonly IRegistroAuditoria _auditoria;

    public PerfilModel(UserManager<UsuarioIdentity> usuarios, SignInManager<UsuarioIdentity> signIn, IContextoUsuarioActual actual, IRegistroAuditoria auditoria)
    {
        _usuarios = usuarios;
        _signIn = signIn;
        _actual = actual;
        _auditoria = auditoria;
    }

    public string Email { get; private set; } = "";
    public IReadOnlyCollection<string> Roles => _actual.Roles;
    public bool MfaActivo { get; private set; }
    public bool MfaObligatorio { get; private set; }
    public int CodigosRestantes { get; private set; }
    public IEnumerable<string>? CodigosNuevos { get; private set; }

    [BindProperty] public string Actual { get; set; } = "";
    [BindProperty] public string Nueva { get; set; } = "";
    [BindProperty] public string Repetir { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        if (await _usuarios.GetUserAsync(User) is null) return Challenge();
        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostContrasenaAsync()
    {
        var u = await _usuarios.GetUserAsync(User) ?? throw new InvalidOperationException();
        if (Nueva != Repetir) ModelState.AddModelError(nameof(Repetir), "Las contraseñas no coinciden.");
        if (ModelState.IsValid)
        {
            var r = await _usuarios.ChangePasswordAsync(u, Actual, Nueva);
            if (r.Succeeded)
            {
                await _signIn.RefreshSignInAsync(u);
                await _auditoria.RegistrarYGuardarAsync(AccionesAuditoria.Configuracion, nameof(Usuario), u.Id.ToString(), new { contrasena = "cambiada" });
                AvisoOk = "Contraseña cambiada.";
                return RedirectToPage();
            }
            foreach (var e in r.Errors) ModelState.AddModelError(string.Empty, e.Description);
        }
        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDesactivarAsync()
    {
        var u = await _usuarios.GetUserAsync(User) ?? throw new InvalidOperationException();
        if (User.FindFirst(ClaimsAserta.MfaObligatorio)?.Value == "1") return Forbid();
        await _usuarios.SetTwoFactorEnabledAsync(u, false);
        await _usuarios.ResetAuthenticatorKeyAsync(u);
        await _auditoria.RegistrarYGuardarAsync(AccionesAuditoria.Configuracion, nameof(Usuario), u.Id.ToString(), new { mfa = "desactivado" });
        await _signIn.RefreshSignInAsync(u);
        AvisoOk = "Segundo factor desactivado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRegenerarCodigosAsync()
    {
        var u = await _usuarios.GetUserAsync(User) ?? throw new InvalidOperationException();
        CodigosNuevos = await _usuarios.GenerateNewTwoFactorRecoveryCodesAsync(u, 8);
        await _auditoria.RegistrarYGuardarAsync(AccionesAuditoria.Configuracion, nameof(Usuario), u.Id.ToString(), new { mfa = "codigos regenerados" });
        AvisoInfo = "Códigos nuevos generados; los anteriores ya no sirven.";
        await CargarAsync();
        return Page();
    }

    private async Task CargarAsync()
    {
        var u = await _usuarios.GetUserAsync(User) ?? throw new InvalidOperationException();
        Email = u.Email ?? "";
        MfaActivo = u.TwoFactorEnabled;
        MfaObligatorio = User.FindFirst(ClaimsAserta.MfaObligatorio)?.Value == "1";
        CodigosRestantes = await _usuarios.CountRecoveryCodesAsync(u);
    }
}
