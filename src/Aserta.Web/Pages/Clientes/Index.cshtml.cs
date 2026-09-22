using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Nucleo;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Clientes;

public class IndexModel : PaginaBase
{
    private readonly ServicioClientes _clientes;
    private readonly ServicioUsuarios _usuarios;
    private readonly IAsertaDb _db;
    private readonly IRelojSistema _reloj;

    public IndexModel(ServicioClientes clientes, ServicioUsuarios usuarios, IAsertaDb db, IRelojSistema reloj)
    {
        _clientes = clientes;
        _usuarios = usuarios;
        _db = db;
        _reloj = reloj;
    }

    public sealed record Fila(Cliente Cliente, string Asesor, int Abiertas, Obligacion? Proxima);

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? Asesor { get; set; }
    [BindProperty(SupportsGet = true)] public string? Estado { get; set; }

    public List<Fila> Filas { get; private set; } = [];
    public List<Usuario> Asesores { get; private set; } = [];
    public int Total { get; private set; }
    public DateOnly Hoy => _reloj.Hoy;

    public async Task OnGetAsync()
    {
        Asesores = await _usuarios.AsesoresActivosAsync();
        var q = await _clientes.ConsultaVisiblesAsync();

        if (Estado == "Baja") q = q.Where(c => c.Estado == EstadoCliente.Baja);
        else if (Estado != "Todos") q = q.Where(c => c.Estado == EstadoCliente.Activo);
        if (Asesor is Guid a) q = q.Where(c => c.AsesorResponsableId == a);
        if (!string.IsNullOrWhiteSpace(Q))
        {
            var t = Q.Trim();
            q = q.Where(c => c.RazonSocial.Contains(t) || (c.NombreComercial != null && c.NombreComercial.Contains(t)) || c.Nif.Contains(t));
        }

        var clientes = await q.OrderBy(c => c.RazonSocial).ToListAsync();
        Total = clientes.Count;
        var ids = clientes.Select(c => c.Id).ToList();

        var abiertas = await _db.Obligaciones.AsNoTracking()
            .Where(o => ids.Contains(o.ClienteId) && o.Estado != EstadoObligacion.Cerrado && o.Estado != EstadoObligacion.NoAplica && o.Estado != EstadoObligacion.Presentado)
            .ToListAsync();
        var porCliente = abiertas.GroupBy(o => o.ClienteId).ToDictionary(g => g.Key, g => g.ToList());
        var asesores = Asesores.ToDictionary(u => u.Id, u => u.NombreCompleto);

        Filas = clientes.Select(c =>
        {
            porCliente.TryGetValue(c.Id, out var lista);
            var proxima = lista?.Where(o => Semaforo.FechaDeReferencia(o) >= Hoy.AddDays(-365)).OrderBy(Semaforo.FechaDeReferencia).FirstOrDefault();
            return new Fila(c, asesores.GetValueOrDefault(c.AsesorResponsableId, "—"), lista?.Count ?? 0, proxima);
        }).ToList();
    }
}
