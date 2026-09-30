using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Sga.Web.Infraestructura;

/// <summary>
/// Sesion de demostracion: entrada directa por perfil, sin contrasenas (G7 del encargo).
/// Un usuario es "cliente:{id}" o "gestor:{id}". No hay Identity ni MFA: es una demo.
/// </summary>
public static class Sesion
{
    public const string Esquema = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string ClaimPerfil = "sga:perfil";
    public const string ClaimId = "sga:id";
    public const string ClaimNombre = "sga:nombre";
    public const string ClaimTipo = "sga:tipo";

    public static async Task EntrarAsync(HttpContext http, string perfil, int id, string nombre, string? tipoCliente)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, nombre), new(ClaimPerfil, perfil), new(ClaimId, id.ToString()), new(ClaimNombre, nombre),
        };
        if (tipoCliente is not null) claims.Add(new Claim(ClaimTipo, tipoCliente));
        var identidad = new ClaimsIdentity(claims, Esquema);
        await http.SignInAsync(Esquema, new ClaimsPrincipal(identidad), new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });
    }

    public static Task SalirAsync(HttpContext http) => http.SignOutAsync(Esquema);

    public static string? Perfil(this ClaimsPrincipal u) => u.FindFirstValue(ClaimPerfil);
    public static int IdActual(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue(ClaimId), out var id) ? id : 0;
    public static string NombreActual(this ClaimsPrincipal u) => u.FindFirstValue(ClaimNombre) ?? "";
    public static bool EsGestor(this ClaimsPrincipal u) => u.Perfil() == "gestor";
    public static bool EsCliente(this ClaimsPrincipal u) => u.Perfil() == "cliente";
}
