using Aserta.Aplicacion.Documental;
using Aserta.Aplicacion.PortalCliente;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Documental;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aserta.Web.Pages.Portal;

[Authorize(Policy = Politicas.Cliente)]
[RequestSizeLimit(25 * 1024 * 1024)]
public class DocumentosModel : PaginaBase
{
    private readonly ServicioPortal _portal;
    private readonly ServicioDocumentos _documentos;
    private readonly ServicioRequisitos _requisitos;
    private readonly IRelojSistema _reloj;

    public DocumentosModel(ServicioPortal portal, ServicioDocumentos documentos, ServicioRequisitos requisitos, IRelojSistema reloj)
    {
        _portal = portal;
        _documentos = documentos;
        _requisitos = requisitos;
        _reloj = reloj;
    }

    [BindProperty(SupportsGet = true)] public TipoDocumento Tipo { get; set; } = TipoDocumento.FacturaRecibida;
    [BindProperty(SupportsGet = true)] public int Ejercicio { get; set; }
    [BindProperty(SupportsGet = true)] public string Periodo { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
    [BindProperty] public IFormFile? Fichero { get; set; }

    public List<Documento> Documentos { get; private set; } = [];
    public IReadOnlyList<EstadoRequisito> Pendientes { get; private set; } = [];
    public static IReadOnlyList<TipoDocumento> Tipos => [TipoDocumento.FacturaRecibida, TipoDocumento.Ticket, TipoDocumento.FacturaEmitida, TipoDocumento.ExtractoBancario, TipoDocumento.Nomina, TipoDocumento.ReciboAlquiler, TipoDocumento.Contrato, TipoDocumento.Otro];
    public List<KeyValuePair<string, string>> Periodos { get; private set; } = [];

    public async Task OnGetAsync() => await CargarAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        await CargarAsync();
        if (Fichero is null || Fichero.Length == 0) { ModelState.AddModelError("Fichero", "Haz una foto o elige un fichero."); return Page(); }
        var ok = await IntentarAsync(async () =>
        {
            await using var s = Fichero.OpenReadStream();
            await _documentos.SubirAsync(new SubidaDocumento { ClienteId = _portal.ClienteId, Tipo = Tipo, Ejercicio = Ejercicio, Periodo = Periodo, NombreOriginal = Fichero.FileName, TipoMime = Fichero.ContentType, TamanoBytes = Fichero.Length, Contenido = s });
        });
        if (!ok) return Page();
        AvisoOk = "¡Recibido! Tu gestoría lo revisará en breve.";
        return RedirectToPage();
    }

    private async Task CargarAsync()
    {
        var hoy = _reloj.Hoy;
        if (Ejercicio == 0) Ejercicio = hoy.Year;
        if (string.IsNullOrEmpty(Periodo)) Periodo = hoy.AddMonths(-1).Month.ToString("00");
        var cultura = System.Globalization.CultureInfo.GetCultureInfo("es-ES");
        // Ultimos 12 meses como opciones (mes + ejercicio en la clave "yyyy-MM" se resuelve por Ejercicio oculto: mantenemos meses del ejercicio elegido)
        Periodos = [.. Dominio.Catalogo.Periodo.Meses.Select(m => new KeyValuePair<string, string>(m, $"{cultura.DateTimeFormat.GetMonthName(int.Parse(m))} {Ejercicio}"))];
        var docs = await _portal.MisDocumentosAsync();
        Documentos = Estado == "Rechazado" ? docs.Where(d => d.Estado == EstadoDocumento.Rechazado).ToList() : docs;
        var estados = await _requisitos.EstadoAsync(_portal.ClienteId);
        Pendientes = estados.Where(e => !e.Completo && e.Requisito.Obligatorio && Dominio.Catalogo.Periodo.Rango(e.Requisito.Ejercicio, e.Requisito.Periodo).Inicio >= hoy.AddMonths(-12)).ToList();
    }
}
