using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Facturacion;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Facturacion;

public class CertificadosModel : PaginaBase
{
    private readonly IAsertaDb _db;
    private readonly IRelojSistema _reloj;
    private readonly IContextoUsuarioActual _usuario;

    public CertificadosModel(IAsertaDb db, IRelojSistema reloj, IContextoUsuarioActual usuario)
    {
        _db = db;
        _reloj = reloj;
        _usuario = usuario;
    }

    public sealed record FilaApoderamiento(Apoderamiento Apoderamiento, string Cliente);
    public List<Certificado> Certificados { get; private set; } = [];
    public List<FilaApoderamiento> Apoderamientos { get; private set; } = [];
    public Dictionary<Guid, string> ClientesSinApoderamiento { get; private set; } = [];
    public DateOnly Hoy => _reloj.Hoy;

    public async Task OnGetAsync()
    {
        Certificados = await _db.Certificados.AsNoTracking().OrderBy(c => c.Tipo).ToListAsync();
        var clientes = await _db.Clientes.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? c.RazonSocial);
        var apod = await _db.Apoderamientos.AsNoTracking().OrderBy(a => a.FechaAlta).ToListAsync();
        Apoderamientos = apod.Select(a => new FilaApoderamiento(a, clientes.GetValueOrDefault(a.ClienteId, "—"))).ToList();
        var conVigente = apod.Where(a => a.VigenteEn(Hoy)).Select(a => a.ClienteId).ToHashSet();
        ClientesSinApoderamiento = clientes.Where(c => !conVigente.Contains(c.Key)).OrderBy(c => c.Value).ToDictionary(c => c.Key, c => c.Value);
    }

    public async Task<IActionResult> OnPostApoderarAsync(Guid clienteId)
    {
        _db.Apoderamientos.Add(new Apoderamiento { Id = Guid.NewGuid(), GestoriaId = _usuario.GestoriaId!.Value, ClienteId = clienteId, FechaAlta = Hoy });
        await _db.GuardarCambiosAsync();
        AvisoOk = "Apoderamiento registrado. Los registros bloqueados de este cliente se pueden reenviar desde la bandeja.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevocarAsync(Guid id)
    {
        var a = await _db.Apoderamientos.FirstOrDefaultAsync(x => x.Id == id);
        if (a is not null) { a.FechaFin = Hoy.AddDays(-1); await _db.GuardarCambiosAsync(); AvisoOk = "Apoderamiento revocado: las próximas emisiones de este cliente quedarán bloqueadas."; }
        return RedirectToPage();
    }
}
