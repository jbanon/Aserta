using System.Net;
using System.Text.RegularExpressions;
using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Obligaciones;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Obligaciones;
using Aserta.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aserta.Integracion.Tests;

/// <summary>
/// Criterio de exito 2 (00-vision-y-alcance.md §7) de extremo a extremo por HTTP:
/// login, tarjeta en el tablero, transicion invalida rechazada con 400, y flujo
/// completo hasta Cerrado (con RD-09) dejando historial.
/// </summary>
[Collection("aplicacion")]
public class FlujoKanbanTests
{
    private readonly FabricaAplicacion _app;
    public FlujoKanbanTests(FabricaAplicacion app) => _app = app;

    private static string Token(string html) => Regex.Match(html, "__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private async Task<HttpClient> ClienteConSesionAsync()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        var http = _app.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });
        var login = await http.GetAsync("/Cuenta/Entrar");
        var html = await login.Content.ReadAsStringAsync();
        var r = await http.PostAsync("/Cuenta/Entrar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Datos.Email"] = FabricaAplicacion.EmailTecnico(FabricaAplicacion.TenantA),
            ["Datos.Contrasena"] = FabricaAplicacion.ContrasenaTecnico,
            ["__RequestVerificationToken"] = Token(html),
        }));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        return http;
    }

    /// <summary>Asegura que el cliente de prueba del tenant A tiene perfil y obligaciones del ejercicio actual.</summary>
    private async Task<Guid> ObligacionAbiertaAsync()
    {
        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var db = scope.ServiceProvider.GetRequiredService<AsertaDbContext>();
        var reloj = scope.ServiceProvider.GetRequiredService<Aserta.Aplicacion.Puertos.IRelojSistema>();
        var cliente = await db.Clientes.Include(c => c.Perfiles).FirstAsync(c => c.GestoriaId == FabricaAplicacion.TenantA);
        if (cliente.Perfiles.Count == 0)
        {
            scope.ServiceProvider.GetRequiredService<ContextoEjecucion>().EstablecerUsuario(cliente.AsesorResponsableId, FabricaAplicacion.TenantA, null, "test", ["SocioDirector"], "127.0.0.1", "tests");
            await scope.ServiceProvider.GetRequiredService<ServicioClientes>().NuevaVersionPerfilAsync(cliente.Id,
                new DatosPerfilFiscal { VigenteDesde = cliente.FechaAlta, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral, TieneEmpleados = true });
            await scope.ServiceProvider.GetRequiredService<ServicioGeneracionObligaciones>().GenerarParaClienteAsync(cliente.Id, [reloj.Hoy.Year]);
        }
        var pendiente = await db.Obligaciones.Where(o => o.ClienteId == cliente.Id && o.Estado == EstadoObligacion.PendienteDocumentacion).OrderBy(o => o.Periodo).FirstOrDefaultAsync();
        if (pendiente is null)
        {
            // Todas usadas por ejecuciones anteriores: se reabre una cerrada creando un cliente nuevo no es posible sin NIF nuevo; reactivamos por SQL una NoAplica o creamos obligacion directa.
            pendiente = new Obligacion { Id = Guid.NewGuid(), GestoriaId = FabricaAplicacion.TenantA, ClienteId = cliente.Id, ModeloCodigo = "303", Ejercicio = (short)(reloj.Hoy.Year - 5 - Random.Shared.Next(0, 1000)), Periodo = "1T", FechaLimitePresentacion = reloj.Hoy.AddDays(30), FechaLimiteDomiciliacion = reloj.Hoy.AddDays(25), FechaGeneracionUtc = DateTime.UtcNow };
            db.Obligaciones.Add(pendiente);
            await db.SaveChangesAsync();
        }
        return pendiente.Id;
    }

    [Fact]
    public async Task Transicion_invalida_devuelve_400_con_motivo_y_valida_repinta_la_tarjeta()
    {
        var http = await ClienteConSesionAsync();
        var id = await ObligacionAbiertaAsync();
        var tablero = await http.GetAsync("/Obligaciones/Tablero");
        Assert.Equal(HttpStatusCode.OK, tablero.StatusCode);
        var token = Token(await tablero.Content.ReadAsStringAsync());

        var invalida = await http.PostAsync("/Obligaciones/Tablero?handler=Mover", Form(token, ("id", id.ToString()), ("destino", "Presentado"), ("orden", "0")));
        Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);
        Assert.Contains("No se puede pasar", await invalida.Content.ReadAsStringAsync());

        var valida = await http.PostAsync("/Obligaciones/Tablero?handler=Mover", Form(token, ("id", id.ToString()), ("destino", "DocumentacionCompleta"), ("orden", "0")));
        Assert.Equal(HttpStatusCode.OK, valida.StatusCode);
        var tarjeta = await valida.Content.ReadAsStringAsync();
        Assert.Contains("data-estado=\"DocumentacionCompleta\"", tarjeta);
        Assert.True(valida.Headers.Contains("X-Aserta-Anuncio"));
    }

    [Fact]
    public async Task Flujo_completo_hasta_Cerrado_con_RD09_y_historial()
    {
        var http = await ClienteConSesionAsync();
        var id = await ObligacionAbiertaAsync();
        var token = Token(await (await http.GetAsync("/Obligaciones/Tablero")).Content.ReadAsStringAsync());

        foreach (var destino in new[] { "DocumentacionCompleta", "EnPreparacion", "RevisionInterna", "PendienteAprobacionCliente" })
        {
            var r = await http.PostAsync("/Obligaciones/Tablero?handler=Mover", Form(token, ("id", id.ToString()), ("destino", destino)));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        // RD-09: sin aprobacion del cliente no se presenta
        var sinAprobacion = await http.PostAsync("/Obligaciones/Tablero?handler=Mover", Form(token, ("id", id.ToString()), ("destino", "Presentado")));
        Assert.Equal(HttpStatusCode.BadRequest, sinAprobacion.StatusCode);
        Assert.Contains("RD-09", await sinAprobacion.Content.ReadAsStringAsync());

        var detalle = await http.GetAsync($"/Obligaciones/Detalle/{id}");
        var tokenDetalle = Token(await detalle.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Redirect, (await http.PostAsync($"/Obligaciones/Detalle/{id}?handler=RegistrarAprobacion", Form(tokenDetalle))).StatusCode);

        foreach (var destino in new[] { "Presentado", "Cerrado" })
            Assert.Equal(HttpStatusCode.OK, (await http.PostAsync("/Obligaciones/Tablero?handler=Mover", Form(token, ("id", id.ToString()), ("destino", destino)))).StatusCode);

        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var db = scope.ServiceProvider.GetRequiredService<AsertaDbContext>();
        var o = await db.Obligaciones.AsNoTracking().Include(x => x.Historial).FirstAsync(x => x.Id == id);
        Assert.Equal(EstadoObligacion.Cerrado, o.Estado);
        Assert.NotNull(o.FechaAprobacionClienteUtc);
        Assert.True(o.Historial.Count(h => h.TipoEvento == "CambioEstado") >= 6);
        Assert.Contains(o.Historial, h => h.TipoEvento == "AprobacionCliente");
    }

    private static FormUrlEncodedContent Form(string token, params (string, string)[] campos)
    {
        var d = campos.ToDictionary(c => c.Item1, c => c.Item2);
        d["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(d);
    }
}
