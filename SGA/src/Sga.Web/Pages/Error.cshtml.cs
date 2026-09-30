using Microsoft.AspNetCore.Mvc;

namespace Sga.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PaginaBase
{
    public int Codigo { get; private set; }
    public void OnGet(int? codigo) => Codigo = codigo ?? 500;
}
