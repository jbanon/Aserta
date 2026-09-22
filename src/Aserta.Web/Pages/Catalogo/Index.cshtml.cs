using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Catalogo;

public class IndexModel : PaginaBase
{
    private readonly IAsertaDb _db;
    private readonly IRelojSistema _reloj;

    public IndexModel(IAsertaDb db, IRelojSistema reloj)
    {
        _db = db;
        _reloj = reloj;
    }

    [BindProperty(SupportsGet = true)] public string Seccion { get; set; } = "reglas";
    [BindProperty(SupportsGet = true)] public int? Ejercicio { get; set; }

    public List<ReglaObligacion> Reglas { get; private set; } = [];
    public List<ModeloTributario> Modelos { get; private set; } = [];
    public List<PlazoModelo> Plazos { get; private set; } = [];
    public List<DiaInhabil> Festivos { get; private set; } = [];
    public List<int> EjerciciosDisponibles { get; private set; } = [];
    public int SinConfirmar { get; private set; }
    public int TotalPlazos { get; private set; }

    public async Task OnGetAsync()
    {
        if (Seccion is not ("reglas" or "plazos" or "modelos" or "festivos")) Seccion = "reglas";
        Ejercicio ??= _reloj.Hoy.Year;
        Reglas = await _db.ReglasObligacion.AsNoTracking().Include(r => r.Condiciones).OrderBy(r => r.ModeloCodigo).ThenBy(r => r.Prioridad).ToListAsync();
        Modelos = await _db.ModelosTributarios.AsNoTracking().OrderBy(m => m.Codigo).ToListAsync();
        TotalPlazos = await _db.PlazosModelo.CountAsync();
        SinConfirmar = await _db.PlazosModelo.CountAsync(p => !p.Confirmado);
        EjerciciosDisponibles = await _db.PlazosModelo.Select(p => (int)p.Ejercicio).Distinct().OrderBy(e => e).ToListAsync();
        if (Seccion == "plazos")
            Plazos = (await _db.PlazosModelo.AsNoTracking().Where(p => p.Ejercicio == Ejercicio).ToListAsync())
                .OrderBy(p => p.FechaLimitePresentacion).ThenBy(p => p.ModeloCodigo).ToList();
        if (Seccion == "festivos")
            Festivos = await _db.DiasInhabiles.AsNoTracking().OrderBy(d => d.Fecha).ToListAsync();
    }
}
