using System.Text.Json;
using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Documental;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Documental;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Documentos;

public sealed record FilaDocumento(Documento Documento, string Cliente, string SubidoPor, string ResumenExtraccion)
{
    public static string Resumir(string? json)
    {
        if (string.IsNullOrEmpty(json)) return "—";
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            string prov = r.TryGetProperty("proveedor", out var p) ? p.GetString() ?? "" : "";
            string total = r.TryGetProperty("total", out var t) ? t.GetDecimal().ToString("N2") + " €" : "";
            return string.IsNullOrEmpty(prov) ? "—" : $"{prov} · {total} (simulado)";
        }
        catch { return "—"; }
    }
}

public class IndexModel : PaginaBase
{
    private readonly IAsertaDb _db;
    private readonly ServicioDocumentos _documentos;
    private readonly ServicioClientes _clientes;

    public IndexModel(IAsertaDb db, ServicioDocumentos documentos, ServicioClientes clientes)
    {
        _db = db;
        _documentos = documentos;
        _clientes = clientes;
    }

    [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? ClienteId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Tipo { get; set; }
    public List<FilaDocumento> Filas { get; private set; } = [];
    public Dictionary<Guid, string> Clientes { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Clientes = await (await _clientes.ConsultaVisiblesAsync()).OrderBy(c => c.RazonSocial).ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? c.RazonSocial);
        var ids = Clientes.Keys.ToList();
        var q = _db.Documentos.AsNoTracking().Where(d => ids.Contains(d.ClienteId));
        if (string.IsNullOrEmpty(Estado)) q = q.Where(d => d.Estado == EstadoDocumento.Recibido || d.Estado == EstadoDocumento.EnRevision);
        else if (Estado != "Todos" && Enum.TryParse<EstadoDocumento>(Estado, out var e)) q = q.Where(d => d.Estado == e);
        if (ClienteId is Guid c) q = q.Where(d => d.ClienteId == c);
        if (!string.IsNullOrEmpty(Tipo) && Enum.TryParse<TipoDocumento>(Tipo, out var t)) q = q.Where(d => d.Tipo == t);
        var docs = await q.OrderByDescending(d => d.FechaSubidaUtc).Take(300).ToListAsync();
        var usuarios = await _db.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.NombreCompleto);
        Filas = docs.Select(d => new FilaDocumento(d, Clientes.GetValueOrDefault(d.ClienteId, "—"), usuarios.GetValueOrDefault(d.SubidoPorId, "—"), FilaDocumento.Resumir(d.DatosExtraidos))).ToList();
    }

    public async Task<IActionResult> OnPostValidarAsync(Guid id) => await AccionAsync(id, () => _documentos.ValidarAsync(id), "Documento validado.");

    public async Task<IActionResult> OnPostRechazarAsync(Guid id, string? motivo) => await AccionAsync(id, () => _documentos.RechazarAsync(id, motivo ?? ""), "Documento rechazado; el cliente verá el motivo.");

    private async Task<IActionResult> AccionAsync(Guid id, Func<Task<Documento>> accion, string mensaje)
    {
        try
        {
            var d = await accion();
            if (!EsHtmx) { AvisoOk = mensaje; return RedirectToPage(new { estado = Estado, clienteId = ClienteId, tipo = Tipo }); }
            Response.Headers["X-Aserta-Anuncio"] = Uri.EscapeDataString(mensaje);
            var cliente = await _db.Clientes.AsNoTracking().Where(c => c.Id == d.ClienteId).Select(c => c.NombreComercial ?? c.RazonSocial).FirstAsync();
            var subidoPor = await _db.Usuarios.AsNoTracking().Where(u => u.Id == d.SubidoPorId).Select(u => u.NombreCompleto).FirstOrDefaultAsync() ?? "—";
            return Partial("_FilaDocumento", new FilaDocumento(d, cliente, subidoPor, FilaDocumento.Resumir(d.DatosExtraidos)));
        }
        catch (Aserta.Dominio.Comun.ExcepcionDominio ex)
        {
            if (EsHtmx) return BadRequest(ex.Message);
            AvisoError = ex.Message;
            return RedirectToPage(new { estado = Estado, clienteId = ClienteId, tipo = Tipo });
        }
    }
}
