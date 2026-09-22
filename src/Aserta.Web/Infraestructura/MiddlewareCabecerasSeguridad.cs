namespace Aserta.Web.Infraestructura;

/// <summary>CSP sin unsafe-inline ni unsafe-eval (ADR-003 §2.3) y cabeceras basicas de endurecimiento.</summary>
public sealed class MiddlewareCabecerasSeguridad
{
    private const string Csp =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; " +
        "connect-src 'self'; frame-ancestors 'none'; form-action 'self'; base-uri 'self'; object-src 'none'";

    private readonly RequestDelegate _siguiente;

    public MiddlewareCabecerasSeguridad(RequestDelegate siguiente) => _siguiente = siguiente;

    public Task InvokeAsync(HttpContext http)
    {
        var h = http.Response.Headers;
        h["Content-Security-Policy"] = Csp;
        h["X-Content-Type-Options"] = "nosniff";
        h["X-Frame-Options"] = "DENY";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        h["Permissions-Policy"] = "camera=(self), microphone=(), geolocation=()";
        return _siguiente(http);
    }
}
