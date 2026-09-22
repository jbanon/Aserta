using Aserta.Aplicacion.Comun;
using Aserta.Aplicacion.Documental;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Aplicacion.PortalCliente;

public sealed record ResumenPortal(Cliente Cliente, int TotalFaltantes, IReadOnlyList<EstadoRequisito> Pendientes, IReadOnlyList<EstadoRequisito> Todos,
    IReadOnlyList<Obligacion> PendientesAprobacion, IReadOnlyList<Obligacion> Proximas, int DocumentosRechazados, int MensajesSinLeer);

/// <summary>
/// Portal del cliente (M2): todo se acota al ClienteId del usuario. Aqui vive la
/// aprobacion del borrador (RD-09), que es lo unico que el cliente puede cambiar
/// de una obligacion.
/// </summary>
public sealed class ServicioPortal
{
    private readonly IAsertaDb _db;
    private readonly IContextoUsuarioActual _usuario;
    private readonly IRelojSistema _reloj;
    private readonly ServicioRequisitos _requisitos;
    private readonly IPublicadorEventos _eventos;
    private readonly INotificador _notificador;

    public ServicioPortal(IAsertaDb db, IContextoUsuarioActual usuario, IRelojSistema reloj, ServicioRequisitos requisitos, IPublicadorEventos eventos, INotificador notificador)
    {
        _db = db;
        _usuario = usuario;
        _reloj = reloj;
        _requisitos = requisitos;
        _eventos = eventos;
        _notificador = notificador;
    }

    public Guid ClienteId => _usuario.ClienteId ?? throw new ExcepcionNoAutorizado("Este usuario no pertenece a ningún cliente.");

    public async Task<Cliente> MiEmpresaAsync(CancellationToken ct = default) =>
        await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == ClienteId, ct) ?? throw new ExcepcionNoEncontrado("Cliente", ClienteId);

    public async Task<ResumenPortal> ResumenAsync(CancellationToken ct = default)
    {
        var cliente = await MiEmpresaAsync(ct);
        var hoy = _reloj.Hoy;
        var estados = await _requisitos.EstadoAsync(cliente.Id, null, ct);
        // Solo se reclama lo de periodos ya terminados o en curso, y de los ultimos 12 meses
        var relevantes = estados.Where(e => Periodo.Rango(e.Requisito.Ejercicio, e.Requisito.Periodo).Inicio >= hoy.AddMonths(-12)).ToList();
        var pendientes = relevantes.Where(e => !e.Completo && e.Requisito.Obligatorio).ToList();

        var obligaciones = await _db.Obligaciones.AsNoTracking().Where(o => o.ClienteId == cliente.Id && o.Estado != EstadoObligacion.NoAplica).ToListAsync(ct);
        var aprobacion = obligaciones.Where(o => o.Estado == EstadoObligacion.PendienteAprobacionCliente).OrderBy(Semaforo.FechaDeReferencia).ToList();
        var proximas = obligaciones.Where(o => !o.Estado.EsFinalOPresentado() && Semaforo.DiasRestantes(o, hoy) >= -30).OrderBy(Semaforo.FechaDeReferencia).Take(6).ToList();
        int rechazados = await _db.Documentos.CountAsync(d => d.ClienteId == cliente.Id && d.Estado == EstadoDocumento.Rechazado, ct);
        int sinLeer = await _db.Mensajes.CountAsync(m => m.LeidoPorClienteUtc == null && _db.Hilos.Any(h => h.Id == m.HiloId && h.ClienteId == cliente.Id), ct);
        return new ResumenPortal(cliente, CompletitudDocumental.TotalFaltantes(pendientes), pendientes, relevantes, aprobacion, proximas, rechazados, sinLeer);
    }

    public async Task<Obligacion> ObligacionAsync(Guid id, CancellationToken ct = default) =>
        await _db.Obligaciones.AsNoTracking().Include(o => o.Historial).FirstOrDefaultAsync(o => o.Id == id && o.ClienteId == ClienteId, ct) ?? throw new ExcepcionNoEncontrado("Obligación", id);

    /// <summary>RD-09: conformidad del cliente con fecha y usuario. Solo ClienteAdmin.</summary>
    public async Task AprobarBorradorAsync(Guid obligacionId, CancellationToken ct = default)
    {
        if (!_usuario.TieneRol(Roles.ClienteAdmin)) throw new ExcepcionNoAutorizado("Solo el administrador de la empresa aprueba borradores.");
        var o = await _db.Obligaciones.Include(x => x.Historial).FirstOrDefaultAsync(x => x.Id == obligacionId && x.ClienteId == ClienteId, ct) ?? throw new ExcepcionNoEncontrado("Obligación", obligacionId);
        if (o.Estado != EstadoObligacion.PendienteAprobacionCliente) throw new Dominio.Comun.ExcepcionDominio("Este impuesto no está pendiente de tu aprobación.");
        if (o.FechaAprobacionClienteUtc is not null) throw new Dominio.Comun.ExcepcionDominio("Ya lo habías aprobado.");
        o.RegistrarAprobacionCliente(_usuario.UsuarioId!.Value, _reloj.AhoraUtc);
        await _db.GuardarCambiosAsync(ct);
        await _eventos.PublicarAsync(new BorradorAprobado(o.Id, o.ClienteId, _usuario.UsuarioId!.Value), ct);
        var cliente = await MiEmpresaAsync(ct);
        await _notificador.NotificarAsync(new Notificacion(o.AsesorId ?? cliente.AsesorResponsableId, TiposAviso.AprobacionCliente, $"{cliente.NombreParaMostrar} ha aprobado el {o.Titulo}", $"Conformidad registrada el {_reloj.AhoraUtc.ToLocalTime():dd/MM/yyyy HH:mm} por {_usuario.NombreCompleto}.", nameof(Obligacion), o.Id.ToString(), $"aprobacion:{o.Id}"), ct);
        await _db.GuardarCambiosAsync(ct);
    }

    /// <summary>El cliente rechaza el borrador: la obligacion vuelve a EnPreparacion con su motivo en el historial y en un hilo.</summary>
    public async Task RechazarBorradorAsync(Guid obligacionId, string motivo, CancellationToken ct = default)
    {
        if (!_usuario.TieneRol(Roles.ClienteAdmin)) throw new ExcepcionNoAutorizado("Solo el administrador de la empresa rechaza borradores.");
        if (string.IsNullOrWhiteSpace(motivo)) throw new ExcepcionValidacion("Motivo", "Cuéntanos qué no cuadra para que tu gestoría lo revise.");
        var o = await _db.Obligaciones.Include(x => x.Historial).FirstOrDefaultAsync(x => x.Id == obligacionId && x.ClienteId == ClienteId, ct) ?? throw new ExcepcionNoEncontrado("Obligación", obligacionId);
        var gestoria = await _db.Gestorias.AsNoTracking().FirstAsync(ct);
        o.CambiarEstado(EstadoObligacion.EnPreparacion, gestoria.ExigeAprobacionCliente, _usuario.UsuarioId, _reloj.AhoraUtc, $"El cliente rechaza el borrador: {motivo.Trim()}");
        await _db.GuardarCambiosAsync(ct);
        await _eventos.PublicarAsync(new BorradorRechazado(o.Id, o.ClienteId, _usuario.UsuarioId!.Value, motivo.Trim()), ct);
        var cliente = await MiEmpresaAsync(ct);
        await _notificador.NotificarAsync(new Notificacion(o.AsesorId ?? cliente.AsesorResponsableId, TiposAviso.AprobacionCliente, $"{cliente.NombreParaMostrar} ha rechazado el borrador del {o.Titulo}", motivo.Trim(), nameof(Obligacion), o.Id.ToString(), $"rechazo:{o.Id}:{_reloj.AhoraUtc.Ticks}"), ct);
        await _db.GuardarCambiosAsync(ct);
    }

    public Task<List<Documento>> MisDocumentosAsync(int? ejercicio = null, CancellationToken ct = default)
    {
        var q = _db.Documentos.AsNoTracking().Where(d => d.ClienteId == ClienteId);
        if (ejercicio is int e) q = q.Where(d => d.Ejercicio == e);
        return q.OrderByDescending(d => d.FechaSubidaUtc).Take(200).ToListAsync(ct);
    }
}
