using Aserta.Aplicacion.Puertos;

namespace Aserta.Infraestructura.Persistencia;

/// <summary>
/// Implementacion unica (con ambito de peticion) de IContextoTenant e
/// IContextoUsuarioActual. El middleware web la rellena desde los claims; los
/// trabajos de fondo y el runner abren ambitos explicitos.
/// </summary>
public sealed class ContextoEjecucion : IContextoTenant, IContextoUsuarioActual
{
    private readonly Stack<(Guid? GestoriaId, bool EsMantenimiento)> _pila = new();

    public Guid? GestoriaId { get; private set; }
    public bool EsMantenimiento { get; private set; }

    public bool EstaAutenticado => UsuarioId.HasValue;
    public Guid? UsuarioId { get; private set; }
    public Guid? ClienteId { get; private set; }
    public string? NombreCompleto { get; private set; }
    public IReadOnlyCollection<string> Roles { get; private set; } = [];
    public string? DireccionIp { get; private set; }
    public string? AgenteUsuario { get; private set; }

    public bool TieneRol(string rol) => Roles.Contains(rol);

    /// <summary>Lo llama el middleware web una vez por peticion.</summary>
    public void EstablecerUsuario(Guid? usuarioId, Guid? gestoriaId, Guid? clienteId, string? nombre, IReadOnlyCollection<string> roles, string? ip, string? agente)
    {
        UsuarioId = usuarioId;
        GestoriaId = gestoriaId;
        ClienteId = clienteId;
        NombreCompleto = nombre;
        Roles = roles;
        DireccionIp = ip;
        AgenteUsuario = agente;
    }

    public IDisposable AbrirAmbitoMantenimiento()
    {
        _pila.Push((GestoriaId, EsMantenimiento));
        EsMantenimiento = true;
        return new Ambito(this);
    }

    public IDisposable AbrirAmbitoTenant(Guid gestoriaId)
    {
        _pila.Push((GestoriaId, EsMantenimiento));
        GestoriaId = gestoriaId;
        EsMantenimiento = false;
        return new Ambito(this);
    }

    private void Restaurar()
    {
        if (_pila.Count == 0) return;
        var (g, m) = _pila.Pop();
        GestoriaId = g;
        EsMantenimiento = m;
    }

    private sealed class Ambito(ContextoEjecucion ctx) : IDisposable
    {
        private bool _cerrado;
        public void Dispose()
        {
            if (_cerrado) return;
            _cerrado = true;
            ctx.Restaurar();
        }
    }
}
