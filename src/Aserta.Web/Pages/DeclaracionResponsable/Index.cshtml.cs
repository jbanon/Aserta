using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Facturacion;
using Aserta.Infraestructura.Persistencia;
using Aserta.Verifactu.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.DeclaracionResponsable;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly OpcionesVerifactu _opciones;
    private readonly AsertaDbContext _db;
    private readonly ContextoEjecucion _contexto;
    private readonly IWebHostEnvironment _env;

    public IndexModel(OpcionesVerifactu opciones, AsertaDbContext db, ContextoEjecucion contexto, IWebHostEnvironment env)
    {
        _opciones = opciones;
        _db = db;
        _contexto = contexto;
        _env = env;
    }

    public DeclaracionResponsableOpciones Declaracion => _opciones.DeclaracionResponsable;
    public string NumeroInstalacion => _opciones.Sistema.NumeroInstalacion;
    public bool EsProduccion => _env.IsProduction();
    public bool Autenticado => User.Identity?.IsAuthenticated == true;
    public List<DeclaracionResponsableHistorico> Historico { get; private set; } = [];

    public async Task OnGetAsync()
    {
        using (_contexto.AbrirAmbitoMantenimiento())
            Historico = await _db.DeclaracionesResponsables.AsNoTracking().OrderByDescending(h => h.Id).ToListAsync();
    }
}
