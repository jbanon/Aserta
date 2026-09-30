using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Pages.Gestor;

public abstract class PaginaGestor(SgaDb db) : PaginaBase
{
    protected SgaDb Db => db;
    public Sga.Nucleo.Modelo.Gestor Gestor { get; private set; } = default!;
    protected int GestorId => IdActual;

    public override async Task OnPageHandlerExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutionDelegate next)
    {
        Gestor = await db.Gestores.AsNoTracking().FirstOrDefaultAsync(g => g.Id == GestorId) ?? throw new InvalidOperationException("Gestor de la sesión no encontrado.");
        await next();
    }
}
