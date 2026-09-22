using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Mensajeria;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Mensajes;

[Authorize(Policy = Politicas.Gestoria)]
public class IndexModel : PaginaBase
{
    private readonly IAsertaDb _db;
    private readonly ServicioClientes _clientes;

    public IndexModel(IAsertaDb db, ServicioClientes clientes)
    {
        _db = db;
        _clientes = clientes;
    }

    public sealed record Fila(Hilo Hilo, string Cliente, string Sobre, int SinLeer);
    public List<Fila> Filas { get; private set; } = [];
    public int SinLeer { get; private set; }

    public async Task OnGetAsync()
    {
        var clientes = await (await _clientes.ConsultaVisiblesAsync()).ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? c.RazonSocial);
        var ids = clientes.Keys.ToList();
        var hilos = await _db.Hilos.AsNoTracking().Include(h => h.Mensajes).Where(h => ids.Contains(h.ClienteId)).OrderByDescending(h => h.FechaUltimoMensajeUtc).Take(200).ToListAsync();
        var obligaciones = await _db.Obligaciones.AsNoTracking().Where(o => hilos.Select(h => h.ObligacionId).Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Titulo);
        var documentos = await _db.Documentos.AsNoTracking().Where(d => hilos.Select(h => h.DocumentoId).Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.NombreOriginal);
        Filas = hilos.Select(h => new Fila(h, clientes.GetValueOrDefault(h.ClienteId, "—"),
            h.ObligacionId is Guid o ? obligaciones.GetValueOrDefault(o, "obligación") : h.DocumentoId is Guid d ? $"documento «{documentos.GetValueOrDefault(d, "")}»" : "",
            h.Mensajes.Count(m => m.LeidoPorGestoriaUtc is null))).ToList();
        SinLeer = Filas.Sum(f => f.SinLeer);
    }
}
