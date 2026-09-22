using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Aserta.Web.Pages;

public class ErrorModel : PageModel
{
    public int? Codigo { get; private set; }
    public string Titulo { get; private set; } = "Algo ha ido mal";
    public string Mensaje { get; private set; } = "Se ha producido un error inesperado. Inténtelo de nuevo en unos instantes.";

    public void OnGet(int? codigo)
    {
        Codigo = codigo;
        (Titulo, Mensaje) = codigo switch
        {
            404 => ("Página no encontrada", "La dirección no existe o ha cambiado."),
            403 => ("Sin permiso", "Su usuario no tiene acceso a esta sección."),
            400 => ("Petición incorrecta", "La petición no se ha podido procesar (¿ha caducado el formulario? Recargue la página)."),
            _ => (Titulo, Mensaje)
        };
    }
}
