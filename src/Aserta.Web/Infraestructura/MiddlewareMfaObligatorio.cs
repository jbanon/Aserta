using System.Security.Claims;
using Aserta.Infraestructura.Identidad;

namespace Aserta.Web.Infraestructura;

/// <summary>
/// Un usuario con MfaObligatorio que aun no ha activado el segundo factor solo
/// puede ir a las paginas de cuenta hasta que lo configure.
/// </summary>
public sealed class MiddlewareMfaObligatorio
{
    private readonly RequestDelegate _siguiente;
    public MiddlewareMfaObligatorio(RequestDelegate siguiente) => _siguiente = siguiente;

    public Task InvokeAsync(HttpContext http)
    {
        var u = http.User;
        if (u.Identity?.IsAuthenticated == true
            && u.FindFirstValue(ClaimsAserta.MfaObligatorio) == "1"
            && u.FindFirstValue(ClaimsAserta.MfaActivo) != "1"
            && !http.Request.Path.StartsWithSegments("/Cuenta")
            && !http.Request.Path.StartsWithSegments("/salud")
            && !Path.HasExtension(http.Request.Path.Value ?? ""))
        {
            http.Response.Redirect("/Cuenta/Mfa/Configurar?obligatorio=1");
            return Task.CompletedTask;
        }
        return _siguiente(http);
    }
}
