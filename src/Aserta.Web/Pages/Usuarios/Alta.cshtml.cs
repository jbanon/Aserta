using Aserta.Aplicacion.Nucleo;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;

namespace Aserta.Web.Pages.Usuarios;

public class AltaModel : PaginaBase
{
    private readonly ServicioUsuarios _usuarios;
    public AltaModel(ServicioUsuarios usuarios) => _usuarios = usuarios;

    [BindProperty] public DatosUsuarioNuevo Datos { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var ok = await IntentarAsync(() => _usuarios.CrearAsync(Datos), "Datos");
        if (!ok) return Page();
        AvisoOk = $"Usuario {Datos.Email} creado.";
        return RedirectToPage("/Usuarios/Index");
    }
}
