using Aserta.Aplicacion.Comun;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Aplicacion.Nucleo;

public sealed class DatosUsuarioNuevo
{
    public string Email { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = Roles.Asesor;
    public Guid? ClienteId { get; set; }
    public bool MfaObligatorio { get; set; }
}

/// <summary>Gestion de usuarios del tenant (solo SocioDirector; ClienteAdmin para los usuarios de su empresa).</summary>
public sealed class ServicioUsuarios
{
    private readonly IAsertaDb _db;
    private readonly IGestorIdentidad _identidad;
    private readonly IContextoUsuarioActual _actual;

    public ServicioUsuarios(IAsertaDb db, IGestorIdentidad identidad, IContextoUsuarioActual actual)
    {
        _db = db;
        _identidad = identidad;
        _actual = actual;
    }

    public async Task<Guid> CrearAsync(DatosUsuarioNuevo datos, CancellationToken ct = default)
    {
        var gestoriaId = _actual.GestoriaId ?? throw new ExcepcionNoAutorizado("No hay gestoría en el contexto.");
        var errores = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(datos.Email) || !datos.Email.Contains('@')) errores["Email"] = ["Correo electrónico no válido."];
        if (string.IsNullOrWhiteSpace(datos.NombreCompleto)) errores["NombreCompleto"] = ["El nombre es obligatorio."];
        if (!Roles.Todos.Contains(datos.Rol)) errores["Rol"] = ["Rol desconocido."];
        if (Roles.EsDeCliente(datos.Rol) && datos.ClienteId is null) errores["ClienteId"] = ["Un usuario del lado cliente tiene que pertenecer a un cliente."];
        if (Roles.EsDeGestoria(datos.Rol) && datos.ClienteId is not null) errores["ClienteId"] = ["Un usuario de la gestoría no pertenece a ningún cliente."];
        if (_actual.TieneRol(Roles.ClienteAdmin) && !_actual.TieneRol(Roles.SocioDirector))
        {
            if (!Roles.EsDeCliente(datos.Rol) || datos.ClienteId != _actual.ClienteId)
                errores["Rol"] = ["Un administrador de cliente solo puede crear usuarios de su propia empresa."];
        }
        if (errores.Count == 0 && await _identidad.ExisteEmailAsync(datos.Email, ct)) errores["Email"] = ["Ya existe un usuario con este correo."];
        if (errores.Count > 0) throw new ExcepcionValidacion(errores);

        var resultado = await _identidad.CrearUsuarioAsync(
            new UsuarioNuevo(datos.Email.Trim(), datos.Contrasena, datos.NombreCompleto.Trim(), gestoriaId, datos.ClienteId, [datos.Rol], datos.MfaObligatorio), ct);
        if (!resultado.Exito) throw new ExcepcionValidacion("Contrasena", string.Join(" ", resultado.Errores));
        return resultado.UsuarioId!.Value;
    }

    public async Task CambiarEstadoAsync(Guid usuarioId, EstadoUsuario estado, CancellationToken ct = default)
    {
        var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId, ct) ?? throw new ExcepcionNoEncontrado("Usuario", usuarioId);
        if (u.Id == _actual.UsuarioId && estado != EstadoUsuario.Activo) throw new ExcepcionValidacion("Estado", "No puede bloquearse a sí mismo.");
        u.Estado = estado;
        await _db.GuardarCambiosAsync(ct);
    }

    public Task<List<Usuario>> ListarDeGestoriaAsync(CancellationToken ct = default) =>
        _db.Usuarios.AsNoTracking().Where(u => u.ClienteId == null).OrderBy(u => u.NombreCompleto).ToListAsync(ct);

    public Task<List<Usuario>> AsesoresActivosAsync(CancellationToken ct = default) =>
        _db.Usuarios.AsNoTracking().Where(u => u.ClienteId == null && u.Estado == EstadoUsuario.Activo).OrderBy(u => u.NombreCompleto).ToListAsync(ct);
}
