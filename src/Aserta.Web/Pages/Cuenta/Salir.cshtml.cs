using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Aserta.Web.Pages.Cuenta;

public class SalirModel : PageModel
{
    private readonly SignInManager<UsuarioIdentity> _signIn;
    private readonly IRegistroAuditoria _auditoria;
    private readonly IContextoUsuarioActual _usuario;

    public SalirModel(SignInManager<UsuarioIdentity> signIn, IRegistroAuditoria auditoria, IContextoUsuarioActual usuario)
    {
        _signIn = signIn;
        _auditoria = auditoria;
        _usuario = usuario;
    }

    public IActionResult OnGet() => RedirectToPage("/Cuenta/Entrar");

    public async Task<IActionResult> OnPostAsync()
    {
        if (_usuario.EstaAutenticado)
            await _auditoria.RegistrarYGuardarAsync(AccionesAuditoria.CierreSesion, nameof(Usuario), _usuario.UsuarioId!.Value.ToString());
        await _signIn.SignOutAsync();
        return RedirectToPage("/Cuenta/Entrar");
    }
}
