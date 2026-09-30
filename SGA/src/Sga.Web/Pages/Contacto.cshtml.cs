using Microsoft.AspNetCore.Mvc;

namespace Sga.Web.Pages;

public class ContactoModel : PaginaBase
{
    public bool Enviado { get; private set; }
    public void OnGet() { }
    public IActionResult OnPost() { Enviado = true; return Page(); }
}
