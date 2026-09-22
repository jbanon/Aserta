using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Nucleo;
using Aserta.Aplicacion.Obligaciones;
using Aserta.Dominio.Nucleo;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;

namespace Aserta.Web.Pages.Clientes;

public class EditarModel : PaginaBase
{
    private readonly ServicioClientes _clientes;
    private readonly ServicioUsuarios _usuarios;
    private readonly ServicioGeneracionObligaciones _motor;

    public EditarModel(ServicioClientes clientes, ServicioUsuarios usuarios, ServicioGeneracionObligaciones motor)
    {
        _clientes = clientes;
        _usuarios = usuarios;
        _motor = motor;
    }

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public DatosCliente Datos { get; set; } = new();
    public List<Usuario> Asesores { get; private set; } = [];
    public string Nombre { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        Asesores = await _usuarios.AsesoresActivosAsync();
        var c = await _clientes.ObtenerAsync(Id);
        Nombre = c.NombreParaMostrar;
        Datos = new DatosCliente
        {
            Nif = c.Nif, RazonSocial = c.RazonSocial, NombreComercial = c.NombreComercial, FormaJuridica = c.FormaJuridica, Email = c.Email, Telefono = c.Telefono,
            DireccionCalle = c.DireccionCalle, DireccionCodigoPostal = c.DireccionCodigoPostal, DireccionMunicipio = c.DireccionMunicipio, DireccionProvincia = c.DireccionProvincia,
            AsesorResponsableId = c.AsesorResponsableId, FechaAlta = c.FechaAlta, Notas = c.Notas,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Asesores = await _usuarios.AsesoresActivosAsync();
        Nombre = Datos.RazonSocial;
        var ok = await IntentarAsync(() => _clientes.ActualizarDatosAsync(Id, Datos), "Datos");
        if (!ok) return Page();

        // La forma juridica y la fecha de alta afectan a las obligaciones: el motor se reejecuta (es idempotente).
        var r = await _motor.GenerarParaClienteAsync(Id);
        AvisoOk = "Datos guardados." + (r.Any(x => x.HuboCambios) ? $" Obligaciones recalculadas: {string.Join("; ", r.Where(x => x.HuboCambios).Select(x => $"{x.Ejercicio}: {x.Resumen}"))}." : "");
        return RedirectToPage("/Clientes/Ficha", new { id = Id });
    }
}
