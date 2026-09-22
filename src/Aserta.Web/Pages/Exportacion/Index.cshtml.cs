using Aserta.Aplicacion.Exportacion;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Exportacion;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Exportacion;

public class IndexModel : PaginaBase
{
    private readonly ServicioExportacion _servicio;
    private readonly IAsertaDb _db;
    private readonly IRelojSistema _reloj;

    public IndexModel(ServicioExportacion servicio, IAsertaDb db, IRelojSistema reloj)
    {
        _servicio = servicio;
        _db = db;
        _reloj = reloj;
    }

    public sealed class Formulario
    {
        public Guid ClienteId { get; set; }
        public int Ejercicio { get; set; }
        public string Periodo { get; set; } = "3T";
        public string Formato { get; set; } = "CsvGenerico";
        public bool IncluirExportados { get; set; }
    }

    [BindProperty(SupportsGet = true)] public Formulario Datos { get; set; } = new();
    public List<Cliente> Clientes { get; private set; } = [];
    public IReadOnlyList<IExportadorContable> Exportadores => _servicio.Exportadores;
    public IReadOnlyList<string> Periodos => [.. Periodo.Trimestres, .. Periodo.Meses];
    public VistaPreviaExportacion? Previa { get; private set; }
    public List<ExportacionContable> Historico { get; private set; } = [];
    public Dictionary<Guid, string> NombresClientes { get; private set; } = [];
    public Dictionary<Guid, string> NombresUsuarios { get; private set; } = [];

    public async Task OnGetAsync(bool previa = false)
    {
        await CargarAsync();
        if (Datos.Ejercicio == 0) Datos.Ejercicio = _reloj.Hoy.Year;
        if (previa && Datos.ClienteId != Guid.Empty && Periodo.EsValido(Datos.Periodo))
            Previa = await _servicio.VistaPreviaAsync(Datos.ClienteId, Datos.Ejercicio, Datos.Periodo, Datos.IncluirExportados);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ExportacionContable? e = null;
        if (await IntentarAsync(async () => e = await _servicio.ExportarAsync(Datos.ClienteId, Datos.Ejercicio, Datos.Periodo, Datos.Formato, Datos.IncluirExportados), "Datos"))
        {
            AvisoOk = $"Exportación generada: {e!.NumRegistros} registros en {e.NombreFichero}.";
            return RedirectToPage(new Microsoft.AspNetCore.Routing.RouteValueDictionary { ["Datos.ClienteId"] = Datos.ClienteId, ["Datos.Ejercicio"] = Datos.Ejercicio, ["Datos.Periodo"] = Datos.Periodo, ["Datos.Formato"] = Datos.Formato });
        }
        await CargarAsync();
        Previa = Datos.ClienteId != Guid.Empty && Periodo.EsValido(Datos.Periodo) ? await _servicio.VistaPreviaAsync(Datos.ClienteId, Datos.Ejercicio, Datos.Periodo, Datos.IncluirExportados) : null;
        return Page();
    }

    public async Task<IActionResult> OnGetDescargarAsync(Guid id)
    {
        try
        {
            var (e, contenido, tipo) = await _servicio.DescargarAsync(id);
            return File(contenido, tipo, e.NombreFichero);
        }
        catch (Aserta.Aplicacion.Comun.ExcepcionNoEncontrado) { return NotFound(); }
    }

    private async Task CargarAsync()
    {
        Clientes = await _db.Clientes.AsNoTracking().Where(c => c.Estado == EstadoCliente.Activo).OrderBy(c => c.RazonSocial).ToListAsync();
        NombresClientes = Clientes.ToDictionary(c => c.Id, c => c.NombreComercial ?? c.RazonSocial);
        Historico = await _servicio.HistoricoAsync(Datos.ClienteId == Guid.Empty ? null : Datos.ClienteId);
        NombresUsuarios = await _db.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.NombreCompleto);
    }
}
