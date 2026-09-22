using Aserta.Aplicacion.Puertos;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Auditoria;

public class IndexModel : PaginaBase
{
    private readonly IAsertaDb _db;
    public IndexModel(IAsertaDb db) => _db = db;

    public const int PorPagina = 50;
    [BindProperty(SupportsGet = true)] public int Pagina { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string? Entidad { get; set; }
    [BindProperty(SupportsGet = true)] public string? Accion { get; set; }

    public List<Aserta.Dominio.Nucleo.Auditoria> Filas { get; private set; } = [];
    public List<string> Entidades { get; private set; } = [];
    public List<string> Acciones { get; private set; } = [];
    public Dictionary<Guid, string> NombresUsuarios { get; private set; } = [];
    public int Total { get; private set; }

    public async Task OnGetAsync()
    {
        if (Pagina < 1) Pagina = 1;
        var q = _db.Auditorias.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(Entidad)) q = q.Where(a => a.EntidadTipo == Entidad);
        if (!string.IsNullOrEmpty(Accion)) q = q.Where(a => a.Accion == Accion);
        Total = await q.CountAsync();
        Filas = await q.OrderByDescending(a => a.FechaUtc).ThenByDescending(a => a.Id).Skip((Pagina - 1) * PorPagina).Take(PorPagina).ToListAsync();
        Entidades = await _db.Auditorias.Select(a => a.EntidadTipo).Distinct().OrderBy(e => e).ToListAsync();
        Acciones = await _db.Auditorias.Select(a => a.Accion).Distinct().OrderBy(e => e).ToListAsync();
        NombresUsuarios = await _db.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.NombreCompleto);
    }
}
