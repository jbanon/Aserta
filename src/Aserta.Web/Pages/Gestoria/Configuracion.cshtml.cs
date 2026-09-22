using Aserta.Aplicacion.Nucleo;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;

namespace Aserta.Web.Pages.Gestoria;

public class ConfiguracionModel : PaginaBase
{
    private readonly ServicioGestoria _gestoria;
    public ConfiguracionModel(ServicioGestoria gestoria) => _gestoria = gestoria;

    [BindProperty] public string Nombre { get; set; } = "";
    [BindProperty] public bool ExigeAprobacionCliente { get; set; }
    [BindProperty] public bool AsesorVeTodosLosClientes { get; set; }
    public string Nif { get; private set; } = "";
    public string ZonaHoraria { get; private set; } = "";

    public async Task OnGetAsync()
    {
        var g = await _gestoria.ActualAsync();
        Nombre = g.Nombre; ExigeAprobacionCliente = g.ExigeAprobacionCliente; AsesorVeTodosLosClientes = g.AsesorVeTodosLosClientes; Nif = g.Nif; ZonaHoraria = g.ZonaHoraria;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var ok = await IntentarAsync(() => _gestoria.ConfigurarAsync(ExigeAprobacionCliente, AsesorVeTodosLosClientes, Nombre));
        if (!ok) { await OnGetAsync(); return Page(); }
        AvisoOk = "Configuración guardada.";
        return RedirectToPage();
    }
}
