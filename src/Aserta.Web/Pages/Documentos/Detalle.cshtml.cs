using System.Text.Json;
using Aserta.Aplicacion.Documental;
using Aserta.Aplicacion.Mensajeria;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Mensajeria;
using Aserta.Dominio.Obligaciones;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Documentos;

public class DetalleModel : PaginaBase
{
    private readonly ServicioDocumentos _documentos;
    private readonly ServicioMensajeria _mensajeria;
    private readonly IAsertaDb _db;

    public DetalleModel(ServicioDocumentos documentos, ServicioMensajeria mensajeria, IAsertaDb db)
    {
        _documentos = documentos;
        _mensajeria = mensajeria;
        _db = db;
    }

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public Documento Documento { get; private set; } = null!;
    public string Cliente { get; private set; } = "";
    public string SubidoPor { get; private set; } = "";
    public List<KeyValuePair<string, string>> Extraidos { get; private set; } = [];
    public List<Obligacion> Vinculadas { get; private set; } = [];
    public List<Obligacion> Vinculables { get; private set; } = [];
    public List<Hilo> Hilos { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostEnRevisionAsync() => await AccionAsync(() => _documentos.MarcarEnRevisionAsync(Id), "Marcado en revisión.");
    public async Task<IActionResult> OnPostValidarAsync() => await AccionAsync(() => _documentos.ValidarAsync(Id), "Documento validado. Si el periodo queda completo, sus obligaciones pasan a «Documentación completa».");
    public async Task<IActionResult> OnPostRechazarAsync(string? motivo) => await AccionAsync(() => _documentos.RechazarAsync(Id, motivo ?? ""), "Documento rechazado.");

    public async Task<IActionResult> OnPostVincularAsync(Guid obligacionId)
    {
        var ok = await IntentarAsync(() => _documentos.VincularAsync(Id, obligacionId));
        if (ok) AvisoOk = "Documento vinculado.";
        else { await CargarAsync(); return Page(); }
        return RedirectToPage();
    }

    private async Task<IActionResult> AccionAsync(Func<Task<Documento>> accion, string mensaje)
    {
        var ok = await IntentarAsync(async () => await accion());
        if (ok) { AvisoOk = mensaje; return RedirectToPage(); }
        await CargarAsync();
        return Page();
    }

    private async Task CargarAsync()
    {
        Documento = await _documentos.ObtenerAsync(Id);
        Cliente = await _db.Clientes.AsNoTracking().Where(c => c.Id == Documento.ClienteId).Select(c => c.NombreComercial ?? c.RazonSocial).FirstAsync();
        SubidoPor = await _db.Usuarios.AsNoTracking().Where(u => u.Id == Documento.SubidoPorId).Select(u => u.NombreCompleto).FirstOrDefaultAsync() ?? "—";
        var idsVinc = await _db.DocumentosObligacion.AsNoTracking().Where(x => x.DocumentoId == Id).Select(x => x.ObligacionId).ToListAsync();
        var todas = await _db.Obligaciones.AsNoTracking().Where(o => o.ClienteId == Documento.ClienteId && o.Ejercicio == Documento.Ejercicio && o.Estado != EstadoObligacion.NoAplica).ToListAsync();
        Vinculadas = todas.Where(o => idsVinc.Contains(o.Id)).OrderBy(o => o.ModeloCodigo).ToList();
        Vinculables = todas.Where(o => !idsVinc.Contains(o.Id)).OrderBy(o => o.ModeloCodigo).ThenBy(o => Periodo.Orden(o.Periodo)).ToList();
        Hilos = await _mensajeria.DeDocumentoAsync(Id);
        if (!string.IsNullOrEmpty(Documento.DatosExtraidos))
        {
            try
            {
                using var json = JsonDocument.Parse(Documento.DatosExtraidos);
                foreach (var p in json.RootElement.EnumerateObject())
                    if (p.Name != "simulado") Extraidos.Add(new(p.Name, p.Value.ValueKind == JsonValueKind.Number ? p.Value.GetDecimal().ToString("N2") : p.Value.ToString()));
            }
            catch { }
        }
    }
}
