using Aserta.Aplicacion.PortalCliente;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Obligaciones;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Portal;

[Authorize(Policy = Politicas.Cliente)]
public class ImpuestosModel : PaginaBase
{
    private readonly ServicioPortal _portal;
    private readonly IAsertaDb _db;
    private readonly IRelojSistema _reloj;
    private Dictionary<string, string> _modelos = [];

    public ImpuestosModel(ServicioPortal portal, IAsertaDb db, IRelojSistema reloj)
    {
        _portal = portal;
        _db = db;
        _reloj = reloj;
    }

    public List<IGrouping<string, Obligacion>> Grupos { get; private set; } = [];

    public async Task OnGetAsync()
    {
        _modelos = await _db.ModelosTributarios.AsNoTracking().ToDictionaryAsync(m => m.Codigo, m => m.Nombre);
        var hoy = _reloj.Hoy;
        var todas = await _db.Obligaciones.AsNoTracking().Where(o => o.ClienteId == _portal.ClienteId && o.Estado != EstadoObligacion.NoAplica).ToListAsync();
        Grupos = todas.OrderBy(Semaforo.FechaDeReferencia)
            .GroupBy(o => o.Estado == EstadoObligacion.PendienteAprobacionCliente ? "Esperan tu aprobación"
                        : o.Estado.EsFinalOPresentado() ? "Presentados"
                        : Semaforo.DiasRestantes(o, hoy) < 0 ? "Fuera de plazo"
                        : "En curso")
            .OrderBy(g => g.Key switch { "Esperan tu aprobación" => 0, "En curso" => 1, "Fuera de plazo" => 2, _ => 3 })
            .Select(g => (IGrouping<string, Obligacion>)(g.Key == "Presentados" ? new Grupo(g.Key, g.OrderByDescending(Semaforo.FechaDeReferencia).Take(12)) : g))
            .ToList();
    }

    public string NombreModelo(string codigo) => _modelos.TryGetValue(codigo, out var n) ? $"{codigo} · {n}" : codigo;

    private sealed class Grupo(string clave, IEnumerable<Obligacion> items) : IGrouping<string, Obligacion>
    {
        public string Key => clave;
        public IEnumerator<Obligacion> GetEnumerator() => items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
