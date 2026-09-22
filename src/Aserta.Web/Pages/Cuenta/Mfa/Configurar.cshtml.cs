using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Infraestructura.Identidad;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aserta.Web.Pages.Cuenta.Mfa;

public class ConfigurarModel : PaginaBase
{
    private readonly UserManager<UsuarioIdentity> _usuarios;
    private readonly SignInManager<UsuarioIdentity> _signIn;
    private readonly IRegistroAuditoria _auditoria;

    public ConfigurarModel(UserManager<UsuarioIdentity> usuarios, SignInManager<UsuarioIdentity> signIn, IRegistroAuditoria auditoria)
    {
        _usuarios = usuarios;
        _signIn = signIn;
        _auditoria = auditoria;
    }

    [BindProperty, Required(ErrorMessage = "Introduzca el código.")] public string Codigo { get; set; } = "";
    [BindProperty(SupportsGet = true)] public bool Obligatorio { get; set; }
    public string ClaveFormateada { get; private set; } = "";
    public string QrDataUri { get; private set; } = "";
    public IEnumerable<string>? CodigosRecuperacion { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var u = await _usuarios.GetUserAsync(User) ?? throw new InvalidOperationException();
        await PrepararAsync(u);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var u = await _usuarios.GetUserAsync(User) ?? throw new InvalidOperationException();
        var codigo = Codigo.Replace(" ", "").Replace("-", "");
        var valido = ModelState.IsValid && await _usuarios.VerifyTwoFactorTokenAsync(u, _usuarios.Options.Tokens.AuthenticatorTokenProvider, codigo);
        if (!valido)
        {
            ModelState.AddModelError(nameof(Codigo), "El código no es válido. Compruebe la hora del dispositivo e inténtelo de nuevo.");
            await PrepararAsync(u);
            return Page();
        }

        await _usuarios.SetTwoFactorEnabledAsync(u, true);
        CodigosRecuperacion = await _usuarios.GenerateNewTwoFactorRecoveryCodesAsync(u, 8);
        await _auditoria.RegistrarYGuardarAsync(AccionesAuditoria.Configuracion, nameof(Usuario), u.Id.ToString(), new { mfa = "activado" });
        await _signIn.RefreshSignInAsync(u); // renueva los claims (aserta:mfa-activo = 1)
        return Page();
    }

    private async Task PrepararAsync(UsuarioIdentity u)
    {
        var clave = await _usuarios.GetAuthenticatorKeyAsync(u);
        if (string.IsNullOrEmpty(clave))
        {
            await _usuarios.ResetAuthenticatorKeyAsync(u);
            clave = await _usuarios.GetAuthenticatorKeyAsync(u)!;
        }
        ClaveFormateada = string.Join(" ", Enumerable.Range(0, (clave!.Length + 3) / 4).Select(i => clave.Substring(i * 4, Math.Min(4, clave.Length - i * 4)))).ToLowerInvariant();
        var uri = $"otpauth://totp/{UrlEncoder.Default.Encode("Aserta")}:{UrlEncoder.Default.Encode(u.Email!)}?secret={clave}&issuer={UrlEncoder.Default.Encode("Aserta")}&digits=6";
        QrDataUri = GeneradorQr.DataUriPng(uri, 5);
    }
}
