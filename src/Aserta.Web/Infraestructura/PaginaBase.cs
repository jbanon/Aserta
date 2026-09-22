using Aserta.Aplicacion.Comun;
using Aserta.Dominio.Comun;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Aserta.Web.Infraestructura;

/// <summary>Utilidades comunes: avisos por TempData, deteccion de htmx y volcado de errores de validacion a ModelState.</summary>
public abstract class PaginaBase : PageModel
{
    public bool EsHtmx => Request.Headers.ContainsKey("HX-Request");

    [TempData] public string? AvisoOk { get; set; }
    [TempData] public string? AvisoError { get; set; }
    [TempData] public string? AvisoInfo { get; set; }

    /// <summary>Ejecuta un caso de uso y traduce sus excepciones de validacion/dominio a ModelState. Devuelve true si fue bien.</summary>
    protected async Task<bool> IntentarAsync(Func<Task> accion, string prefijo = "")
    {
        try
        {
            await accion();
            return true;
        }
        catch (ExcepcionValidacion ex)
        {
            foreach (var (campo, mensajes) in ex.Errores)
                foreach (var m in mensajes)
                    ModelState.AddModelError(string.IsNullOrEmpty(prefijo) ? campo : $"{prefijo}.{campo}", m);
            return false;
        }
        catch (ExcepcionDominio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return false;
        }
        catch (ExcepcionNoAutorizado ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return false;
        }
    }

    protected IActionResult AnunciarHtmx(string texto)
    {
        Response.Headers["X-Aserta-Anuncio"] = Uri.EscapeDataString(texto);
        return new EmptyResult();
    }
}
