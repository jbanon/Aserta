using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Obligaciones;
using Aserta.Aplicacion.Puertos;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;

namespace Aserta.Web.Pages.Clientes;

public class PerfilFiscalModel : PaginaBase
{
    private readonly ServicioClientes _clientes;
    private readonly ServicioGeneracionObligaciones _motor;
    private readonly IRelojSistema _reloj;

    public PerfilFiscalModel(ServicioClientes clientes, ServicioGeneracionObligaciones motor, IRelojSistema reloj)
    {
        _clientes = clientes;
        _motor = motor;
        _reloj = reloj;
    }

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public DatosPerfilFiscal Perfil { get; set; } = new();
    public string Nombre { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var c = await _clientes.ObtenerAsync(Id);
        Nombre = c.NombreParaMostrar;
        Perfil = c.PerfilActual is { } p ? DatosPerfilFiscal.Desde(p) : new DatosPerfilFiscal();
        Perfil.VigenteDesde = _reloj.Hoy;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var c = await _clientes.ObtenerAsync(Id);
        Nombre = c.NombreParaMostrar;
        var ok = await IntentarAsync(() => _clientes.NuevaVersionPerfilAsync(Id, Perfil), "Perfil");
        if (!ok) return Page();

        var r = await _motor.GenerarParaClienteAsync(Id);
        AvisoOk = $"Perfil actualizado con vigencia desde {Perfil.VigenteDesde:dd/MM/yyyy}. " +
                  string.Join(" ", r.Select(x => $"{x.Ejercicio}: {x.Resumen}."));
        return RedirectToPage("/Clientes/Ficha", new { id = Id, pestana = "obligaciones" });
    }
}
