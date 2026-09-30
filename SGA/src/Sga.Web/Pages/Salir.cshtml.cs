using Microsoft.AspNetCore.Mvc;
using Sga.Web.Infraestructura;

namespace Sga.Web.Pages;

public class SalirModel : PaginaBase
{
    public async Task<IActionResult> OnGetAsync() { await Sesion.SalirAsync(HttpContext); return Redirect("/acceso"); }
}
