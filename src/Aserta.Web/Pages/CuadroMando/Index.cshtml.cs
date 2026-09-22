using Aserta.Aplicacion.CuadroMando;
using Aserta.Web.Infraestructura;

namespace Aserta.Web.Pages.CuadroMando;

public class IndexModel : PaginaBase
{
    private readonly ServicioCuadroMando _servicio;
    public IndexModel(ServicioCuadroMando servicio) => _servicio = servicio;

    public Aserta.Aplicacion.CuadroMando.CuadroMando Cuadro { get; private set; } = default!;

    public async Task OnGetAsync() => Cuadro = await _servicio.ObtenerAsync(HttpContext.RequestAborted);
}
