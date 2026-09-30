using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Sga.Web.Infraestructura;

namespace Sga.Web.Pages;

public abstract class PaginaBase : PageModel
{
    [TempData] public string? AvisoOk { get; set; }
    [TempData] public string? AvisoError { get; set; }
    [TempData] public string? AvisoInfo { get; set; }
    public bool EsHtmx => Request.Headers.ContainsKey("HX-Request");
    protected int IdActual => User.IdActual();
}
