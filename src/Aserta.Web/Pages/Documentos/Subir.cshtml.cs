using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Documental;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Documental;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Documentos;

[RequestSizeLimit(25 * 1024 * 1024)]
public class SubirModel : PaginaBase
{
    private readonly ServicioDocumentos _documentos;
    private readonly ServicioClientes _clientes;
    private readonly IRelojSistema _reloj;

    public SubirModel(ServicioDocumentos documentos, ServicioClientes clientes, IRelojSistema reloj)
    {
        _documentos = documentos;
        _clientes = clientes;
        _reloj = reloj;
    }

    [BindProperty(SupportsGet = true)] public Guid? ClienteId { get; set; }
    [BindProperty(SupportsGet = true)] public TipoDocumento Tipo { get; set; } = TipoDocumento.FacturaRecibida;
    [BindProperty(SupportsGet = true)] public int Ejercicio { get; set; }
    [BindProperty(SupportsGet = true)] public string Periodo { get; set; } = "";
    [BindProperty] public string? Notas { get; set; }
    [BindProperty] public IFormFile? Fichero { get; set; }
    public Dictionary<Guid, string> Clientes { get; private set; } = [];
    public static IReadOnlyList<KeyValuePair<string, string>> Periodos =>
        [.. Dominio.Catalogo.Periodo.Meses.Select(m => new KeyValuePair<string, string>(m, System.Globalization.CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetMonthName(int.Parse(m)))),
         .. Dominio.Catalogo.Periodo.Trimestres.Select(t => new KeyValuePair<string, string>(t, t)), new("AN", "Anual")];

    public async Task OnGetAsync()
    {
        await CargarAsync();
        if (Ejercicio == 0) Ejercicio = _reloj.Hoy.Year;
        if (string.IsNullOrEmpty(Periodo)) Periodo = _reloj.Hoy.AddMonths(-1).Month.ToString("00");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await CargarAsync();
        if (Fichero is null || Fichero.Length == 0) { ModelState.AddModelError("Fichero", "Seleccione un fichero."); return Page(); }
        if (ClienteId is null) { ModelState.AddModelError("ClienteId", "Seleccione un cliente."); return Page(); }
        Documento? doc = null;
        var ok = await IntentarAsync(async () =>
        {
            await using var s = Fichero.OpenReadStream();
            doc = await _documentos.SubirAsync(new SubidaDocumento { ClienteId = ClienteId.Value, Tipo = Tipo, Ejercicio = Ejercicio, Periodo = Periodo, NombreOriginal = Fichero.FileName, TipoMime = Fichero.ContentType, TamanoBytes = Fichero.Length, Contenido = s, Notas = Notas });
        });
        if (!ok) return Page();
        AvisoOk = $"Documento «{doc!.NombreOriginal}» registrado.";
        return RedirectToPage("/Documentos/Detalle", new { id = doc.Id });
    }

    private async Task CargarAsync() =>
        Clientes = await (await _clientes.ConsultaVisiblesAsync()).OrderBy(c => c.RazonSocial).ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? c.RazonSocial);
}
