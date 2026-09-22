using System.Security.Claims;
using Aserta.Infraestructura.Identidad;
using Aserta.Infraestructura.Persistencia;

namespace Aserta.Web.Infraestructura;

/// <summary>Rellena el contexto de ejecucion (tenant + usuario) desde los claims de la cookie, una vez por peticion.</summary>
public sealed class MiddlewareContextoEjecucion
{
    private readonly RequestDelegate _siguiente;

    public MiddlewareContextoEjecucion(RequestDelegate siguiente) => _siguiente = siguiente;

    public async Task InvokeAsync(HttpContext http, ContextoEjecucion contexto)
    {
        var u = http.User;
        Guid? usuarioId = null, gestoriaId = null, clienteId = null;
        string? nombre = null;
        IReadOnlyCollection<string> roles = [];

        if (u.Identity?.IsAuthenticated == true)
        {
            usuarioId = Leer(u.FindFirstValue(ClaimTypes.NameIdentifier));
            gestoriaId = Leer(u.FindFirstValue(ClaimsAserta.GestoriaId));
            clienteId = Leer(u.FindFirstValue(ClaimsAserta.ClienteId));
            nombre = u.FindFirstValue(ClaimsAserta.NombreCompleto) ?? u.Identity.Name;
            roles = u.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        }

        contexto.EstablecerUsuario(usuarioId, gestoriaId, clienteId, nombre, roles,
            http.Connection.RemoteIpAddress?.ToString(), http.Request.Headers.UserAgent.ToString());

        await _siguiente(http);
    }

    private static Guid? Leer(string? valor) => Guid.TryParse(valor, out var g) ? g : null;
}
