using System.Text.Json;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;

namespace Aserta.Infraestructura.Persistencia;

public sealed class RegistroAuditoria : IRegistroAuditoria
{
    private static readonly JsonSerializerOptions Json = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly IAsertaDb _db;
    private readonly IContextoUsuarioActual _usuario;
    private readonly IContextoTenant _tenant;
    private readonly IRelojSistema _reloj;

    public RegistroAuditoria(IAsertaDb db, IContextoUsuarioActual usuario, IContextoTenant tenant, IRelojSistema reloj)
    {
        _db = db;
        _usuario = usuario;
        _tenant = tenant;
        _reloj = reloj;
    }

    public void Registrar(string accion, string entidadTipo, string entidadId, object? detalle = null)
    {
        var gestoriaId = _tenant.GestoriaId ?? _usuario.GestoriaId;
        if (gestoriaId is null) return;
        _db.Auditorias.Add(Crear(accion, entidadTipo, entidadId, detalle, gestoriaId.Value, _usuario.UsuarioId));
    }

    public async Task RegistrarYGuardarAsync(string accion, string entidadTipo, string entidadId, object? detalle = null, Guid? gestoriaId = null, Guid? usuarioId = null, CancellationToken ct = default)
    {
        var g = gestoriaId ?? _tenant.GestoriaId ?? _usuario.GestoriaId;
        if (g is null) return;
        _db.Auditorias.Add(Crear(accion, entidadTipo, entidadId, detalle, g.Value, usuarioId ?? _usuario.UsuarioId));
        await _db.GuardarCambiosAsync(ct);
    }

    private Dominio.Nucleo.Auditoria Crear(string accion, string entidadTipo, string entidadId, object? detalle, Guid gestoriaId, Guid? usuarioId) => new()
    {
        GestoriaId = gestoriaId,
        UsuarioId = usuarioId,
        FechaUtc = _reloj.AhoraUtc,
        Accion = accion,
        EntidadTipo = entidadTipo,
        EntidadId = entidadId,
        Detalle = detalle is null ? null : JsonSerializer.Serialize(detalle, Json),
        DireccionIp = _usuario.DireccionIp,
        AgenteUsuario = _usuario.AgenteUsuario is { Length: > 500 } a ? a[..500] : _usuario.AgenteUsuario,
    };
}
