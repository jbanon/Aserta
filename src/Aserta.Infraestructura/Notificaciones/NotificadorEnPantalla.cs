using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Infraestructura.Notificaciones;

/// <summary>Adaptador de INotificador para la demo: bandeja en pantalla (dbo.Aviso). Idempotente por (destinatario, clave).</summary>
public sealed class NotificadorEnPantalla : INotificador
{
    private readonly IAsertaDb _db;
    private readonly IRelojSistema _reloj;
    private readonly IContextoTenant _tenant;

    public NotificadorEnPantalla(IAsertaDb db, IRelojSistema reloj, IContextoTenant tenant)
    {
        _db = db;
        _reloj = reloj;
        _tenant = tenant;
    }

    public async Task<bool> NotificarAsync(Notificacion n, CancellationToken ct = default)
    {
        var gestoriaId = _tenant.GestoriaId ?? throw new InvalidOperationException("No hay tenant en el contexto para notificar.");
        bool existe = await _db.Avisos.AnyAsync(a => a.UsuarioDestinoId == n.DestinatarioId && a.Clave == n.Clave, ct)
                      || _db.Avisos.Local.Any(a => a.UsuarioDestinoId == n.DestinatarioId && a.Clave == n.Clave);
        if (existe) return false;

        _db.Avisos.Add(new Aviso
        {
            GestoriaId = gestoriaId,
            UsuarioDestinoId = n.DestinatarioId,
            Tipo = n.Tipo,
            Titulo = n.Titulo.Length > 200 ? n.Titulo[..200] : n.Titulo,
            Cuerpo = n.Cuerpo is { Length: > 1000 } c ? c[..1000] : n.Cuerpo,
            EntidadTipo = n.EntidadTipo,
            EntidadId = n.EntidadId,
            Clave = n.Clave,
            FechaUtc = _reloj.AhoraUtc,
        });
        return true;
    }
}
