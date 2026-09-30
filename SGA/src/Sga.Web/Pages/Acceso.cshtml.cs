using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Infraestructura;

namespace Sga.Web.Pages;

public class AccesoModel(SgaDb db) : PaginaBase
{
    public List<Sga.Nucleo.Modelo.Cliente> Clientes { get; private set; } = [];
    public List<Sga.Nucleo.Modelo.Gestor> Gestores { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var todos = await db.Clientes.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
        // Cuatro perfiles representativos: arrendador, profesional al que factura SGA, profesional que factura el mismo, sociedad
        Clientes = [todos.First(c => c.Tipo == TipoCliente.Arrendador), todos.First(c => c.Tipo == TipoCliente.Profesional && c.Emisor == QuienEmite.Sga), todos.First(c => c.Tipo == TipoCliente.Profesional && c.Emisor == QuienEmite.Cliente), todos.First(c => c.Tipo == TipoCliente.Sociedad && c.ClaveConsultaBanco)];
        Gestores = await db.Gestores.AsNoTracking().OrderBy(g => g.Id).ToListAsync();
    }

    public string Descripcion(Sga.Nucleo.Modelo.Cliente c) => c.Tipo switch
    {
        TipoCliente.Arrendador => "Arrendador · le emitimos la factura del local",
        TipoCliente.Profesional => c.Emisor == QuienEmite.Sga ? "Profesional · dos actividades, le facturamos nosotros" : "Profesional · factura él mismo, con un hueco de numeración",
        _ => "Sociedad · productora, conciliación bancaria en marcha",
    };

    /// <summary>Entrada directa por URL (demo sin contrasenas): /acceso?handler=Directo&perfil=cliente&id=1. Util para el guion y las capturas.</summary>
    public async Task<IActionResult> OnGetDirectoAsync(string perfil, int id) => perfil == "gestor" ? await OnPostGestorAsync(id) : await OnPostClienteAsync(id);

    public async Task<IActionResult> OnPostClienteAsync(int id)
    {
        var c = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        await Sesion.EntrarAsync(HttpContext, "cliente", c.Id, c.Tipo == TipoCliente.Sociedad ? (c.NombreContacto ?? c.NombreCorto) : c.NombreCorto, c.Tipo.ToString());
        return Redirect("/cliente");
    }

    public async Task<IActionResult> OnPostGestorAsync(int id)
    {
        var g = await db.Gestores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (g is null) return NotFound();
        await Sesion.EntrarAsync(HttpContext, "gestor", g.Id, g.Nombre, null);
        return Redirect("/gestor");
    }
}
