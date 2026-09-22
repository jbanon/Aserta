using Aserta.Aplicacion.Obligaciones;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Avisos;

public class IndexModel : PaginaBase
{
    private readonly IAsertaDb _db;
    private readonly IContextoUsuarioActual _usuario;
    private readonly IRelojSistema _reloj;
    private readonly ServicioAvisosVencimiento _avisos;

    public IndexModel(IAsertaDb db, IContextoUsuarioActual usuario, IRelojSistema reloj, ServicioAvisosVencimiento avisos)
    {
        _db = db;
        _usuario = usuario;
        _reloj = reloj;
        _avisos = avisos;
    }

    public List<Aviso> Filas { get; private set; } = [];
    public int NoLeidos { get; private set; }
    public bool PuedeGenerar => _usuario.TieneRol(Roles.SocioDirector);

    public async Task OnGetAsync()
    {
        Filas = await _db.Avisos.AsNoTracking().Where(a => a.UsuarioDestinoId == _usuario.UsuarioId).OrderByDescending(a => a.FechaUtc).Take(200).ToListAsync();
        NoLeidos = Filas.Count(a => !a.Leido);
    }

    public async Task<IActionResult> OnPostLeerAsync(long id)
    {
        var a = await _db.Avisos.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioDestinoId == _usuario.UsuarioId);
        if (a is not null && !a.Leido) { a.LeidoUtc = _reloj.AhoraUtc; await _db.GuardarCambiosAsync(); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarcarTodosAsync()
    {
        var pendientes = await _db.Avisos.Where(x => x.UsuarioDestinoId == _usuario.UsuarioId && x.LeidoUtc == null).ToListAsync();
        foreach (var a in pendientes) a.LeidoUtc = _reloj.AhoraUtc;
        await _db.GuardarCambiosAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGenerarAsync()
    {
        if (!PuedeGenerar) return Forbid();
        int n = await _avisos.GenerarAsync();
        AvisoOk = $"Tarea ejecutada: {n} avisos nuevos.";
        return RedirectToPage();
    }
}
