using Aserta.Aplicacion.Mensajeria;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Mensajeria;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Mensajes;

public class HiloModel : PaginaBase
{
    private readonly ServicioMensajeria _mensajeria;
    private readonly IContextoUsuarioActual _usuario;
    private readonly IAsertaDb _db;

    public HiloModel(ServicioMensajeria mensajeria, IContextoUsuarioActual usuario, IAsertaDb db)
    {
        _mensajeria = mensajeria;
        _usuario = usuario;
        _db = db;
    }

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public string Cuerpo { get; set; } = "";
    public Hilo Hilo { get; private set; } = null!;
    public Dictionary<Guid, string> Nombres { get; private set; } = [];
    public bool EsCliente => _usuario.ClienteId is not null;
    public Guid UsuarioActualId => _usuario.UsuarioId ?? Guid.Empty;
    public string Contexto { get; private set; } = "";
    public string? AnclaUrl { get; private set; }
    public string AnclaTexto { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        Hilo = await _mensajeria.LeerAsync(Id);
        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var ok = await IntentarAsync(() => _mensajeria.ResponderAsync(Id, Cuerpo));
        if (ok) return RedirectToPage(new { id = Id });
        Hilo = await _mensajeria.ObtenerAsync(Id);
        await CargarAsync();
        return Page();
    }

    private async Task CargarAsync()
    {
        Nombres = await _db.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.NombreCompleto);
        var cliente = await _db.Clientes.AsNoTracking().Where(c => c.Id == Hilo.ClienteId).Select(c => c.NombreComercial ?? c.RazonSocial).FirstAsync();
        if (Hilo.ObligacionId is Guid o)
        {
            var ob = await _db.Obligaciones.AsNoTracking().FirstAsync(x => x.Id == o);
            Contexto = $"{cliente} · sobre el {ob.Titulo}";
            AnclaUrl = EsCliente ? $"/Portal/Impuesto/{o}" : $"/Obligaciones/Detalle/{o}";
            AnclaTexto = EsCliente ? "Ver el impuesto" : "Ver la obligación";
        }
        else if (Hilo.DocumentoId is Guid d)
        {
            var doc = await _db.Documentos.AsNoTracking().FirstAsync(x => x.Id == d);
            Contexto = $"{cliente} · sobre el documento «{doc.NombreOriginal}»";
            AnclaUrl = EsCliente ? "/Portal/Documentos" : $"/Documentos/Detalle/{d}";
            AnclaTexto = EsCliente ? "Mis documentos" : "Ver el documento";
        }
    }
}
