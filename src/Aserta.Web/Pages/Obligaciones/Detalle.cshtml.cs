using Aserta.Aplicacion.Documental;
using Aserta.Aplicacion.Mensajeria;
using Aserta.Aplicacion.Obligaciones;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Mensajeria;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Obligaciones;
using Aserta.Dominio.Catalogo;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Obligaciones;

public class DetalleModel : PaginaBase
{
    private readonly ServicioObligaciones _obligaciones;
    private readonly IAsertaDb _db;
    private readonly IRelojSistema _reloj;
    private readonly IContextoUsuarioActual _usuario;
    private readonly ServicioDocumentos _documentos;
    private readonly ServicioRequisitos _requisitos;
    private readonly ServicioMensajeria _mensajeria;

    public DetalleModel(ServicioObligaciones obligaciones, IAsertaDb db, IRelojSistema reloj, IContextoUsuarioActual usuario, ServicioDocumentos documentos, ServicioRequisitos requisitos, ServicioMensajeria mensajeria)
    {
        _obligaciones = obligaciones;
        _db = db;
        _reloj = reloj;
        _usuario = usuario;
        _documentos = documentos;
        _requisitos = requisitos;
        _mensajeria = mensajeria;
    }

    public List<Documento> Documentos { get; private set; } = [];
    public List<EstadoRequisito> Requisitos { get; private set; } = [];
    public List<Hilo> Hilos { get; private set; } = [];

    [Microsoft.AspNetCore.Mvc.RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> OnPostJustificanteAsync(IFormFile? justificante)
    {
        if (justificante is null || justificante.Length == 0) { AvisoError = "Seleccione el justificante."; return RedirectToPage(new { id = Id }); }
        var gestoria = await _db.Gestorias.AsNoTracking().FirstAsync();
        var ok = await IntentarAsync(async () =>
        {
            await using var s = justificante.OpenReadStream();
            await _documentos.ArchivarJustificanteAsync(Id, new SubidaDocumento { NombreOriginal = justificante.FileName, TipoMime = justificante.ContentType, TamanoBytes = justificante.Length, Contenido = s }, gestoria.ExigeAprobacionCliente);
        });
        if (ok) AvisoOk = "Justificante archivado y obligación cerrada.";
        else AvisoError = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        return RedirectToPage(new { id = Id });
    }

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public Obligacion Obligacion { get; private set; } = null!;
    public string NombreCliente { get; private set; } = "";
    public string NombreModelo { get; private set; } = "";
    public string NombreAsesor { get; private set; } = "—";
    public string? ReglaOrigen { get; private set; }
    public bool PlazoConfirmado { get; private set; }
    public IReadOnlyList<EstadoObligacion> Destinos { get; private set; } = [];
    public Dictionary<Guid, string> NombresUsuarios { get; private set; } = [];
    public DateOnly Hoy => _reloj.Hoy;

    public async Task<IActionResult> OnGetAsync()
    {
        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMoverAsync(EstadoObligacion destino, string? comentario)
    {
        var ok = await IntentarAsync(() => _obligaciones.CambiarEstadoAsync(Id, destino, comentario));
        if (!ok) { await CargarAsync(); return Page(); }
        AvisoOk = $"Obligación movida a «{destino.Etiqueta()}».";
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRegistrarAprobacionAsync()
    {
        var o = await _obligaciones.ObtenerAsync(Id);
        o.RegistrarAprobacionCliente(_usuario.UsuarioId!.Value, _reloj.AhoraUtc);
        await _db.GuardarCambiosAsync();
        AvisoOk = "Aprobación del cliente registrada.";
        return RedirectToPage(new { id = Id });
    }

    private async Task CargarAsync()
    {
        Obligacion = await _obligaciones.ObtenerAsync(Id);
        var cliente = await _db.Clientes.AsNoTracking().FirstAsync(c => c.Id == Obligacion.ClienteId);
        NombreCliente = cliente.NombreParaMostrar;
        NombreModelo = await _db.ModelosTributarios.AsNoTracking().Where(m => m.Codigo == Obligacion.ModeloCodigo).Select(m => m.Nombre).FirstOrDefaultAsync() ?? "";
        NombresUsuarios = await _db.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.NombreCompleto);
        if (Obligacion.AsesorId is Guid a) NombreAsesor = NombresUsuarios.GetValueOrDefault(a, "—");
        if (Obligacion.ReglaOrigenId is int r) ReglaOrigen = await _db.ReglasObligacion.AsNoTracking().Where(x => x.Id == r).Select(x => x.Descripcion).FirstOrDefaultAsync();
        PlazoConfirmado = await _db.PlazosModelo.AsNoTracking().AnyAsync(p => p.ModeloCodigo == Obligacion.ModeloCodigo && p.Ejercicio == Obligacion.Ejercicio && p.Periodo == Obligacion.Periodo && p.Confirmado);
        Destinos = MaquinaEstadosObligacion.DestinosDesde(Obligacion.Estado);
        var idsDocs = await _db.DocumentosObligacion.AsNoTracking().Where(x => x.ObligacionId == Id).Select(x => x.DocumentoId).ToListAsync();
        Documentos = await _db.Documentos.AsNoTracking().Where(d => idsDocs.Contains(d.Id)).OrderByDescending(d => d.FechaSubidaUtc).ToListAsync();
        var (oi, of) = Periodo.Rango(Obligacion.Ejercicio, Obligacion.Periodo);
        Requisitos = (await _requisitos.EstadoAsync(Obligacion.ClienteId, Obligacion.Ejercicio)).Where(r => Periodo.Rango(r.Requisito.Ejercicio, r.Requisito.Periodo).Inicio >= oi && Periodo.Rango(r.Requisito.Ejercicio, r.Requisito.Periodo).Fin <= of && Obligacion.Periodo != "AN").ToList();
        Hilos = await _mensajeria.DeObligacionAsync(Id);
    }
}
