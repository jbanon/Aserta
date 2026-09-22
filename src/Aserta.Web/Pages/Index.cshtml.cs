using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Aserta.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IContextoUsuarioActual _usuario;
    public IndexModel(IContextoUsuarioActual usuario) => _usuario = usuario;

    public IActionResult OnGet()
    {
        if (_usuario.TieneRol(Roles.ClienteAdmin) || _usuario.TieneRol(Roles.ClienteUsuario))
            return RedirectToPage("/Portal/Index");
        return RedirectToPage("/Clientes/Index");
    }
}
