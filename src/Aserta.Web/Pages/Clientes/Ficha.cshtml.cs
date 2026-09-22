using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Documental;
using Aserta.Dominio.Documental;
using Aserta.Aplicacion.Obligaciones;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Aserta.Dominio.Catalogo;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Clientes;

public class FichaModel : PaginaBase
{
    private readonly ServicioClientes _clientes;
    private readonly ServicioGeneracionObligaciones _motor;
    private readonly IAsertaDb _db;
    private readonly IRelojSistema _reloj;
    private readonly ServicioRequisitos _requisitos;

    public FichaModel(ServicioClientes clientes, ServicioGeneracionObligaciones motor, IAsertaDb db, IRelojSistema reloj, ServicioRequisitos requisitos)
    {
        _clientes = clientes;
        _motor = motor;
        _db = db;
        _reloj = reloj;
        _requisitos = requisitos;
    }

    public List<EstadoRequisito> Requisitos { get; private set; } = [];
    public List<Documento> Documentos { get; private set; } = [];
    public int Faltantes { get; private set; }

    public async Task<IActionResult> OnPostAjustarRequisitoAsync(Guid requisitoId, int? cantidad, bool obligatorio, bool noAplica)
    {
        var ok = await IntentarAsync(() => _requisitos.AjustarAsync(requisitoId, cantidad, obligatorio, noAplica));
        if (ok) AvisoOk = "Requisito ajustado.";
        return RedirectToPage(new { id = Id, pestana = "documentacion" });
    }

    public async Task<IActionResult> OnPostAnadirRequisitoAsync(int ejercicio, string periodo, TipoDocumento tipo, int? cantidad, string? descripcion)
    {
        var ok = await IntentarAsync(() => _requisitos.AnadirAsync(Id, ejercicio, periodo, tipo, cantidad, descripcion ?? ""));
        if (ok) AvisoOk = "Requisito añadido.";
        else AvisoError = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        return RedirectToPage(new { id = Id, pestana = "documentacion" });
    }

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true)] public string Pestana { get; set; } = "resumen";

    public Cliente Cliente { get; private set; } = null!;
    public string NombreAsesor { get; private set; } = "—";
    public List<Obligacion> Obligaciones { get; private set; } = [];
    public List<Obligacion> Proximas { get; private set; } = [];
    public List<Aserta.Dominio.Nucleo.Auditoria> Auditoria { get; private set; } = [];
    public Dictionary<Guid, string> NombresUsuarios { get; private set; } = [];
    public DateOnly Hoy => _reloj.Hoy;
    public int Abiertas { get; private set; }
    public int VencenEn30 { get; private set; }
    public int Vencidas { get; private set; }
    public int Cerradas { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Pestana is not ("resumen" or "obligaciones" or "perfil" or "historial" or "documentacion")) Pestana = "resumen";
        Cliente = await _clientes.ObtenerAsync(Id);
        NombresUsuarios = await _db.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.NombreCompleto);
        NombreAsesor = NombresUsuarios.GetValueOrDefault(Cliente.AsesorResponsableId, "—");

        Obligaciones = await _db.Obligaciones.AsNoTracking().Where(o => o.ClienteId == Id).ToListAsync();
        var abiertas = Obligaciones.Where(o => !o.Estado.EsFinalOPresentado()).ToList();
        Abiertas = abiertas.Count;
        VencenEn30 = abiertas.Count(o => Semaforo.DiasRestantes(o, Hoy) is >= 0 and <= 30);
        Vencidas = abiertas.Count(o => Semaforo.DiasRestantes(o, Hoy) < 0);
        Cerradas = Obligaciones.Count(o => o.Estado is EstadoObligacion.Presentado or EstadoObligacion.Cerrado);
        Proximas = abiertas.OrderBy(Semaforo.FechaDeReferencia).Take(8).ToList();

        Requisitos = await _requisitos.EstadoAsync(Id);
        Faltantes = CompletitudDocumental.TotalFaltantes(Requisitos.Where(r => Periodo.Rango(r.Requisito.Ejercicio, r.Requisito.Periodo).Inicio >= Hoy.AddMonths(-12)).ToList());
        if (Pestana == "documentacion")
            Documentos = await _db.Documentos.AsNoTracking().Where(d => d.ClienteId == Id).OrderByDescending(d => d.FechaSubidaUtc).Take(100).ToListAsync();
        if (Pestana == "historial")
            Auditoria = await _db.Auditorias.AsNoTracking()
                .Where(a => (a.EntidadTipo == nameof(Cliente) && a.EntidadId == Id.ToString()) ||
                            (a.EntidadTipo == nameof(PerfilFiscal) && Cliente.Perfiles.Select(p => p.Id.ToString()).Contains(a.EntidadId)))
                .OrderByDescending(a => a.FechaUtc).Take(200).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRegenerarAsync()
    {
        var r = await _motor.GenerarParaClienteAsync(Id);
        AvisoOk = "Motor ejecutado. " + string.Join(" ", r.Select(x => $"{x.Ejercicio}: {x.Resumen}."));
        var avisos = r.SelectMany(x => x.Avisos).ToList();
        if (avisos.Count > 0) AvisoInfo = string.Join(" ", avisos.Take(3));
        return RedirectToPage(new { id = Id, pestana = "obligaciones" });
    }
}
