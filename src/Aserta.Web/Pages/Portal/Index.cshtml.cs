using Aserta.Aplicacion.PortalCliente;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Obligaciones;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Portal;

[Authorize(Policy = Politicas.Cliente)]
public class IndexModel : PaginaBase
{
    private readonly ServicioPortal _portal;
    private readonly IAsertaDb _db;
    private readonly IContextoUsuarioActual _usuario;
    private readonly IRelojSistema _reloj;
    private Dictionary<string, string> _modelos = [];

    public IndexModel(ServicioPortal portal, IAsertaDb db, IContextoUsuarioActual usuario, IRelojSistema reloj)
    {
        _portal = portal;
        _db = db;
        _usuario = usuario;
        _reloj = reloj;
    }

    public ResumenPortal Resumen { get; private set; } = null!;
    public string Nombre => (_usuario.NombreCompleto ?? "").Split(' ').FirstOrDefault() ?? "";
    public string NombreGestoria { get; private set; } = "";
    public DateOnly Hoy => _reloj.Hoy;

    public async Task OnGetAsync()
    {
        Resumen = await _portal.ResumenAsync();
        _modelos = await _db.ModelosTributarios.AsNoTracking().ToDictionaryAsync(m => m.Codigo, m => m.Nombre);
        NombreGestoria = await _db.Gestorias.AsNoTracking().Select(g => g.Nombre).FirstOrDefaultAsync() ?? "";
    }

    public string NombreModelo(string codigo) => _modelos.TryGetValue(codigo, out var n) ? $"{codigo} · {n}" : codigo;

    public static string EstadoLlano(EstadoObligacion e) => e switch
    {
        EstadoObligacion.PendienteDocumentacion => "Esperando tus documentos",
        EstadoObligacion.DocumentacionCompleta => "Documentación recibida",
        EstadoObligacion.EnPreparacion or EstadoObligacion.RevisionInterna => "Tu gestoría lo está preparando",
        EstadoObligacion.PendienteAprobacionCliente => "Esperando tu aprobación",
        EstadoObligacion.Presentado => "Presentado",
        EstadoObligacion.Cerrado => "Presentado y archivado",
        _ => e.Etiqueta()
    };
}
