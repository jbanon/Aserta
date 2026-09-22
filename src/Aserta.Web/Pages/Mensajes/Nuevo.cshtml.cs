using Aserta.Aplicacion.Mensajeria;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Mensajeria;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Mensajes;

public class NuevoModel : PaginaBase
{
    private readonly ServicioMensajeria _mensajeria;
    private readonly IContextoUsuarioActual _usuario;
    private readonly IAsertaDb _db;

    public NuevoModel(ServicioMensajeria mensajeria, IContextoUsuarioActual usuario, IAsertaDb db)
    {
        _mensajeria = mensajeria;
        _usuario = usuario;
        _db = db;
    }

    [BindProperty(SupportsGet = true)] public Guid? ObligacionId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? DocumentoId { get; set; }
    [BindProperty] public string Asunto { get; set; } = "";
    [BindProperty] public string Cuerpo { get; set; } = "";
    public bool EsCliente => _usuario.ClienteId is not null;
    public string Contexto { get; private set; } = "";
    public string Volver { get; private set; } = "/";
    private Guid _clienteId;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await CargarAsync()) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await CargarAsync()) return NotFound();
        Hilo? hilo = null;
        var ok = await IntentarAsync(async () => hilo = await _mensajeria.AbrirAsync(_clienteId, ObligacionId, DocumentoId, Asunto, Cuerpo));
        if (!ok) return Page();
        return RedirectToPage("/Mensajes/Hilo", new { id = hilo!.Id });
    }

    private async Task<bool> CargarAsync()
    {
        if (ObligacionId is Guid o)
        {
            var ob = await _db.Obligaciones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == o);
            if (ob is null) return false;
            _clienteId = ob.ClienteId;
            Contexto = $"Sobre el {ob.Titulo}";
            Volver = EsCliente ? $"/Portal/Impuesto/{o}" : $"/Obligaciones/Detalle/{o}";
            if (string.IsNullOrEmpty(Asunto)) Asunto = ob.Titulo;
            return true;
        }
        if (DocumentoId is Guid d)
        {
            var doc = await _db.Documentos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == d);
            if (doc is null) return false;
            _clienteId = doc.ClienteId;
            Contexto = $"Sobre el documento «{doc.NombreOriginal}»";
            Volver = EsCliente ? "/Portal/Documentos" : $"/Documentos/Detalle/{d}";
            if (string.IsNullOrEmpty(Asunto)) Asunto = doc.NombreOriginal;
            return true;
        }
        return false;
    }
}
