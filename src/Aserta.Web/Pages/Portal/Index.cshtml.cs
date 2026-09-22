using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Aserta.Web.Pages.Portal;

[Authorize(Policy = Aserta.Web.Infraestructura.Politicas.Cliente)]
public class IndexModel : PageModel
{
    public void OnGet() { }
}
