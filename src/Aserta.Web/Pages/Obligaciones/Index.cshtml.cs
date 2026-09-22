using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Nucleo;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Obligaciones;

public class IndexModel : PaginaBase
{
    private readonly IAsertaDb _db;
    private readonly ServicioClientes _clientes;
    private readonly ServicioUsuarios _usuarios;
    private readonly IRelojSistema _reloj;

    public IndexModel(IAsertaDb db, ServicioClientes clientes, ServicioUsuarios usuarios, IRelojSistema reloj)
    {
        _db = db;
        _clientes = clientes;
        _usuarios = usuarios;
        _reloj = reloj;
    }

    [BindProperty(SupportsGet = true)] public int? Ejercicio { get; set; }
    [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? Asesor { get; set; }
    [BindProperty(SupportsGet = true)] public string? Modelo { get; set; }

    public List<Obligacion> Filas { get; private set; } = [];
    public List<int> Ejercicios { get; private set; } = [];
    public List<Usuario> Asesores { get; private set; } = [];
    public Dictionary<Guid, string> NombresClientes { get; private set; } = [];
    public Dictionary<Guid, string> NombresAsesores { get; private set; } = [];
    public DateOnly Hoy => _reloj.Hoy;

    public async Task OnGetAsync()
    {
        Ejercicio ??= Hoy.Year;
        Ejercicios = [Hoy.Year - 1, Hoy.Year, Hoy.Year + 1];
        Asesores = await _usuarios.AsesoresActivosAsync();
        NombresAsesores = Asesores.ToDictionary(a => a.Id, a => a.NombreCompleto);

        var visibles = await _clientes.ConsultaVisiblesAsync();
        NombresClientes = await visibles.ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? c.RazonSocial);
        var idsVisibles = NombresClientes.Keys.ToList();

        var q = _db.Obligaciones.AsNoTracking().Where(o => o.Ejercicio == Ejercicio && idsVisibles.Contains(o.ClienteId));
        if (string.IsNullOrEmpty(Estado)) q = q.Where(o => o.Estado != EstadoObligacion.Cerrado && o.Estado != EstadoObligacion.NoAplica && o.Estado != EstadoObligacion.Presentado);
        else if (Estado != "Todas" && Enum.TryParse<EstadoObligacion>(Estado, out var e)) q = q.Where(o => o.Estado == e);
        if (Asesor is Guid a) q = q.Where(o => o.AsesorId == a);
        if (!string.IsNullOrWhiteSpace(Modelo)) q = q.Where(o => o.ModeloCodigo == Modelo.Trim());

        Filas = (await q.ToListAsync()).OrderBy(Semaforo.FechaDeReferencia).ThenBy(o => o.ModeloCodigo).ToList();
    }
}
