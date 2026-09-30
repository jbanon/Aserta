using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Pages.Cliente;

/// <summary>Base de las paginas del area de clientes: el cliente de la sesion y su layout.</summary>
public abstract class PaginaCliente(SgaDb db) : PaginaBase
{
    protected SgaDb Db => db;
    public Sga.Nucleo.Modelo.Cliente Cliente { get; private set; } = default!;
    protected int ClienteId => IdActual;

    public override async Task OnPageHandlerExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutionDelegate next)
    {
        Cliente = await db.Clientes.AsNoTracking().Include(c => c.Gestor).Include(c => c.Contratos).Include(c => c.Actividades).FirstOrDefaultAsync(c => c.Id == ClienteId) ?? throw new InvalidOperationException("Cliente de la sesión no encontrado.");
        ViewData["Layout"] = "_LayoutApp";
        await next();
    }
}
