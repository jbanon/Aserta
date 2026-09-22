using System.Net;

namespace Aserta.Integracion.Tests;

/// <summary>ADR-003 §5: CSP estricta y antiforgery obligatorio en todos los POST.</summary>
[Collection("aplicacion")]
public class SeguridadWebTests
{
    private readonly FabricaAplicacion _app;
    public SeguridadWebTests(FabricaAplicacion app) => _app = app;

    [Fact]
    public async Task Sin_sesion_redirige_al_login()
    {
        var cliente = _app.CreateClient(new() { AllowAutoRedirect = false });
        var r = await cliente.GetAsync("/Clientes");
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("/Cuenta/Entrar", r.Headers.Location!.ToString());
    }

    [Fact]
    public async Task La_CSP_no_permite_inline_ni_eval()
    {
        var cliente = _app.CreateClient();
        var r = await cliente.GetAsync("/Cuenta/Entrar");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var csp = r.Headers.GetValues("Content-Security-Policy").Single();
        Assert.DoesNotContain("unsafe-inline", csp);
        Assert.DoesNotContain("unsafe-eval", csp);
        Assert.Contains("script-src 'self'", csp);
    }

    [Fact]
    public async Task POST_sin_token_antiforgery_devuelve_400()
    {
        var cliente = _app.CreateClient(new() { AllowAutoRedirect = false });
        var r = await cliente.PostAsync("/Cuenta/Entrar", new FormUrlEncodedContent(new Dictionary<string, string> { ["Datos.Email"] = "x@x.x", ["Datos.Contrasena"] = "y" }));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Salud_responde()
    {
        var r = await _app.CreateClient().GetAsync("/salud");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }
}
