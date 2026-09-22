using Aserta.Aplicacion.Comun;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Mensajeria;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Aplicacion.Mensajeria;

/// <summary>Hilos anclados a una obligacion o a un documento. Cada mensaje nuevo avisa al otro lado.</summary>
public sealed class ServicioMensajeria : IManejadorEvento<MensajeNuevo>
{
    private readonly IAsertaDb _db;
    private readonly IContextoUsuarioActual _usuario;
    private readonly IRelojSistema _reloj;
    private readonly IPublicadorEventos _eventos;
    private readonly INotificador _notificador;

    public ServicioMensajeria(IAsertaDb db, IContextoUsuarioActual usuario, IRelojSistema reloj, IPublicadorEventos eventos, INotificador notificador)
    {
        _db = db;
        _usuario = usuario;
        _reloj = reloj;
        _eventos = eventos;
        _notificador = notificador;
    }

    private bool EsCliente => _usuario.ClienteId is not null;

    public async Task<Hilo> AbrirAsync(Guid clienteId, Guid? obligacionId, Guid? documentoId, string asunto, string primerMensaje, CancellationToken ct = default)
    {
        if (EsCliente && _usuario.ClienteId != clienteId) throw new ExcepcionNoAutorizado("No es su empresa.");
        if (obligacionId is Guid o && !await _db.Obligaciones.AnyAsync(x => x.Id == o && x.ClienteId == clienteId, ct)) throw new ExcepcionNoEncontrado("Obligación", o);
        if (documentoId is Guid d && !await _db.Documentos.AnyAsync(x => x.Id == d && x.ClienteId == clienteId, ct)) throw new ExcepcionNoEncontrado("Documento", d);

        var hilo = Hilo.Nuevo(_usuario.GestoriaId!.Value, clienteId, obligacionId, documentoId, asunto, _reloj.AhoraUtc);
        hilo.Responder(_usuario.UsuarioId!.Value, EsCliente, primerMensaje, _reloj.AhoraUtc);
        _db.Hilos.Add(hilo);
        if (obligacionId is Guid oid)
            _db.ObligacionHistoriales.Add(new ObligacionHistorial { GestoriaId = hilo.GestoriaId, ObligacionId = oid, FechaUtc = _reloj.AhoraUtc, UsuarioId = _usuario.UsuarioId, TipoEvento = TiposEventoHistorial.Mensaje, Comentario = $"Hilo abierto: «{hilo.Asunto}».", ReferenciaId = hilo.Id.ToString() });
        await _db.GuardarCambiosAsync(ct);
        await _eventos.PublicarAsync(new MensajeNuevo(hilo.Id, clienteId, _usuario.UsuarioId!.Value, EsCliente), ct);
        await _db.GuardarCambiosAsync(ct);
        return hilo;
    }

    public async Task<Mensaje> ResponderAsync(Guid hiloId, string cuerpo, CancellationToken ct = default)
    {
        var hilo = await ObtenerAsync(hiloId, ct);
        var m = hilo.Responder(_usuario.UsuarioId!.Value, EsCliente, cuerpo, _reloj.AhoraUtc);
        if (hilo.ObligacionId is Guid oid)
            _db.ObligacionHistoriales.Add(new ObligacionHistorial { GestoriaId = hilo.GestoriaId, ObligacionId = oid, FechaUtc = _reloj.AhoraUtc, UsuarioId = _usuario.UsuarioId, TipoEvento = TiposEventoHistorial.Mensaje, Comentario = $"Mensaje en «{hilo.Asunto}».", ReferenciaId = hilo.Id.ToString() });
        await _db.GuardarCambiosAsync(ct);
        await _eventos.PublicarAsync(new MensajeNuevo(hilo.Id, hilo.ClienteId, _usuario.UsuarioId!.Value, EsCliente), ct);
        await _db.GuardarCambiosAsync(ct);
        return m;
    }

    public async Task<Hilo> ObtenerAsync(Guid hiloId, CancellationToken ct = default)
    {
        var hilo = await _db.Hilos.Include(h => h.Mensajes.OrderBy(m => m.FechaUtc)).FirstOrDefaultAsync(h => h.Id == hiloId, ct) ?? throw new ExcepcionNoEncontrado("Hilo", hiloId);
        if (EsCliente && hilo.ClienteId != _usuario.ClienteId) throw new ExcepcionNoAutorizado("No es su empresa.");
        return hilo;
    }

    /// <summary>Marca como leidos los mensajes del otro lado y devuelve el hilo.</summary>
    public async Task<Hilo> LeerAsync(Guid hiloId, CancellationToken ct = default)
    {
        var hilo = await ObtenerAsync(hiloId, ct);
        var ahora = _reloj.AhoraUtc;
        bool cambios = false;
        foreach (var m in hilo.Mensajes)
        {
            if (EsCliente && m.LeidoPorClienteUtc is null) { m.LeidoPorClienteUtc = ahora; cambios = true; }
            if (!EsCliente && m.LeidoPorGestoriaUtc is null) { m.LeidoPorGestoriaUtc = ahora; cambios = true; }
        }
        if (cambios) await _db.GuardarCambiosAsync(ct);
        return hilo;
    }

    public Task<List<Hilo>> DeObligacionAsync(Guid obligacionId, CancellationToken ct = default) =>
        _db.Hilos.AsNoTracking().Include(h => h.Mensajes.OrderBy(m => m.FechaUtc)).Where(h => h.ObligacionId == obligacionId).OrderByDescending(h => h.FechaUltimoMensajeUtc).ToListAsync(ct);

    public Task<List<Hilo>> DeDocumentoAsync(Guid documentoId, CancellationToken ct = default) =>
        _db.Hilos.AsNoTracking().Include(h => h.Mensajes.OrderBy(m => m.FechaUtc)).Where(h => h.DocumentoId == documentoId).OrderByDescending(h => h.FechaUltimoMensajeUtc).ToListAsync(ct);

    public Task<List<Hilo>> DeClienteAsync(Guid clienteId, CancellationToken ct = default) =>
        _db.Hilos.AsNoTracking().Include(h => h.Mensajes.OrderBy(m => m.FechaUtc)).Where(h => h.ClienteId == clienteId).OrderByDescending(h => h.FechaUltimoMensajeUtc).ToListAsync(ct);

    /// <summary>Mensajes sin leer por el lado del usuario actual (para contadores).</summary>
    public Task<int> SinLeerAsync(CancellationToken ct = default) => EsCliente
        ? _db.Mensajes.CountAsync(m => m.LeidoPorClienteUtc == null && _db.Hilos.Any(h => h.Id == m.HiloId && h.ClienteId == _usuario.ClienteId), ct)
        : _db.Mensajes.CountAsync(m => m.LeidoPorGestoriaUtc == null, ct);

    /// <summary>Evento: avisar al otro lado (asesor del cliente, o usuarios del cliente).</summary>
    public async Task ManejarAsync(MensajeNuevo e, CancellationToken ct)
    {
        var hilo = await _db.Hilos.AsNoTracking().FirstAsync(h => h.Id == e.HiloId, ct);
        var cliente = await _db.Clientes.AsNoTracking().FirstAsync(c => c.Id == e.ClienteId, ct);
        var destinatarios = e.AutorEsCliente
            ? [cliente.AsesorResponsableId]
            : await _db.Usuarios.AsNoTracking().Where(u => u.ClienteId == e.ClienteId && u.Estado == EstadoUsuario.Activo).Select(u => u.Id).ToListAsync(ct);
        var ultimo = await _db.Mensajes.AsNoTracking().Where(m => m.HiloId == e.HiloId).OrderByDescending(m => m.Id).FirstAsync(ct);
        foreach (var d in destinatarios)
            await _notificador.NotificarAsync(new Notificacion(d, TiposAviso.Sistema, (e.AutorEsCliente ? $"{cliente.NombreParaMostrar}: " : "Tu gestoría: ") + hilo.Asunto,
                ultimo.Cuerpo.Length > 200 ? ultimo.Cuerpo[..200] + "…" : ultimo.Cuerpo, nameof(Hilo), hilo.Id.ToString(), $"mensaje:{ultimo.Id}"), ct);
    }
}
