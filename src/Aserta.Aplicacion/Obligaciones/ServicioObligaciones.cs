using Aserta.Aplicacion.Comun;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Aplicacion.Obligaciones;

/// <summary>Acciones sobre una obligacion concreta (cambio de estado, reasignacion, borrador).</summary>
public sealed class ServicioObligaciones
{
    private readonly IAsertaDb _db;
    private readonly IContextoUsuarioActual _usuario;
    private readonly IRelojSistema _reloj;

    public ServicioObligaciones(IAsertaDb db, IContextoUsuarioActual usuario, IRelojSistema reloj)
    {
        _db = db;
        _usuario = usuario;
        _reloj = reloj;
    }

    public async Task<Obligacion> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        await _db.Obligaciones.Include(o => o.Historial.OrderByDescending(h => h.FechaUtc)).FirstOrDefaultAsync(o => o.Id == id, ct)
        ?? throw new ExcepcionNoEncontrado("Obligación", id);

    /// <summary>El servidor valida SIEMPRE la transicion (ADR-003): la interfaz solo es optimista.</summary>
    public async Task<Obligacion> CambiarEstadoAsync(Guid id, EstadoObligacion destino, string? comentario = null, CancellationToken ct = default)
    {
        var obligacion = await ObtenerAsync(id, ct);
        if (destino == EstadoObligacion.Presentado && _usuario.TieneRol(Roles.Administrativo) && !_usuario.TieneRol(Roles.Asesor) && !_usuario.TieneRol(Roles.SocioDirector))
            throw new ExcepcionNoAutorizado("Un administrativo no puede marcar una obligación como presentada.");

        var gestoria = await _db.Gestorias.AsNoTracking().FirstAsync(ct);
        obligacion.CambiarEstado(destino, gestoria.ExigeAprobacionCliente, _usuario.UsuarioId, _reloj.AhoraUtc, comentario);
        await _db.GuardarCambiosAsync(ct);
        return obligacion;
    }

    public async Task ReasignarAsync(Guid id, Guid? asesorId, CancellationToken ct = default)
    {
        var obligacion = await ObtenerAsync(id, ct);
        if (asesorId.HasValue)
        {
            bool valido = await _db.Usuarios.AnyAsync(u => u.Id == asesorId && u.ClienteId == null && u.Estado == EstadoUsuario.Activo, ct);
            if (!valido) throw new ExcepcionValidacion("AsesorId", "El asesor indicado no existe o no está activo.");
        }
        var anterior = obligacion.AsesorId;
        obligacion.AsesorId = asesorId;
        obligacion.Historial.Add(new ObligacionHistorial
        {
            GestoriaId = obligacion.GestoriaId, ObligacionId = obligacion.Id, FechaUtc = _reloj.AhoraUtc, UsuarioId = _usuario.UsuarioId,
            TipoEvento = TiposEventoHistorial.Reasignada, Comentario = $"Asesor cambiado.", ReferenciaId = asesorId?.ToString()
        });
        await _db.GuardarCambiosAsync(ct);
    }

    public async Task RegistrarBorradorAsync(Guid id, decimal importe, SignoResultado signo, CancellationToken ct = default)
    {
        var obligacion = await ObtenerAsync(id, ct);
        obligacion.ImporteResultado = importe;
        obligacion.SignoResultado = signo;
        obligacion.Historial.Add(new ObligacionHistorial
        {
            GestoriaId = obligacion.GestoriaId, ObligacionId = obligacion.Id, FechaUtc = _reloj.AhoraUtc, UsuarioId = _usuario.UsuarioId,
            TipoEvento = TiposEventoHistorial.BorradorRegistrado, Comentario = $"Borrador: {importe:N2} € a {signo.ToString().ToLowerInvariant()}."
        });
        await _db.GuardarCambiosAsync(ct);
    }
}
