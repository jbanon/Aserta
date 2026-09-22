using System.Net;
using System.Text.RegularExpressions;
using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Comun;
using Aserta.Aplicacion.CuadroMando;
using Aserta.Aplicacion.Documental;
using Aserta.Aplicacion.Exportacion;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Nucleo;
using Aserta.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aserta.Integracion.Tests;

/// <summary>
/// Criterios de exito 6 y 7 (00-vision-y-alcance.md §7): exportar un periodo,
/// volver a exportarlo sin duplicar (RD-11), repetirlo solo a peticion explicita,
/// aislamiento del historico entre tenants y cuadro de mando del socio.
/// </summary>
[Collection("aplicacion")]
public class FlujoExportacionTests
{
    private readonly FabricaAplicacion _app;
    public FlujoExportacionTests(FabricaAplicacion app) => _app = app;

    private static readonly byte[] Pdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\ntrailer << /Root 1 0 R >>\n%%EOF");

    private async Task<Guid> PrepararClienteAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AsertaDbContext>();
        var cliente = await db.Clientes.Include(c => c.Perfiles).FirstAsync(c => c.GestoriaId == FabricaAplicacion.TenantA);
        sp.GetRequiredService<ContextoEjecucion>().EstablecerUsuario(cliente.AsesorResponsableId, FabricaAplicacion.TenantA, null, "test", [Roles.SocioDirector], "127.0.0.1", "tests");
        if (cliente.Perfiles.Count == 0)
            await sp.GetRequiredService<ServicioClientes>().NuevaVersionPerfilAsync(cliente.Id, new DatosPerfilFiscal { VigenteDesde = cliente.FechaAlta, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral, TieneEmpleados = true });
        return cliente.Id;
    }

    [Fact]
    public async Task Exportar_dos_veces_no_duplica_y_solo_repite_a_peticion_explicita()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var sp = scope.ServiceProvider;
        var clienteId = await PrepararClienteAsync(sp);
        var documentos = sp.GetRequiredService<ServicioDocumentos>();
        var exportacion = sp.GetRequiredService<ServicioExportacion>();

        // Un ejercicio ficticio y lejano para no chocar con otras ejecuciones: dos facturas recibidas validadas
        var db = sp.GetRequiredService<AsertaDbContext>();
        int ejercicio;
        do { ejercicio = Random.Shared.Next(2030, 2100); }
        while (await db.Documentos.AnyAsync(d => d.ClienteId == clienteId && d.Ejercicio == ejercicio));
        foreach (var periodo in new[] { "10", "11" })
        {
            var d = await documentos.SubirAsync(new SubidaDocumento { ClienteId = clienteId, Tipo = TipoDocumento.FacturaRecibida, Ejercicio = ejercicio, Periodo = periodo, NombreOriginal = $"f-{Guid.NewGuid():N}.pdf", TipoMime = "application/pdf", TamanoBytes = 10, Contenido = new MemoryStream(Pdf.Concat(Guid.NewGuid().ToByteArray()).ToArray()) });
            await documentos.ValidarAsync(d.Id);
        }
        // Un documento sin validar no entra
        await documentos.SubirAsync(new SubidaDocumento { ClienteId = clienteId, Tipo = TipoDocumento.FacturaRecibida, Ejercicio = ejercicio, Periodo = "12", NombreOriginal = $"f-{Guid.NewGuid():N}.pdf", TipoMime = "application/pdf", TamanoBytes = 10, Contenido = new MemoryStream(Pdf.Concat(Guid.NewGuid().ToByteArray()).ToArray()) });

        var previa = await exportacion.VistaPreviaAsync(clienteId, ejercicio, "4T", false);
        Assert.Equal(2, previa.Nuevos.Count);
        Assert.Equal(0, previa.YaExportados);

        var primera = await exportacion.ExportarAsync(clienteId, ejercicio, "4T", "CsvGenerico", false);
        Assert.Equal(2, primera.NumRegistros);
        Assert.Equal(64, primera.HashSha256.Length);
        var (_, contenido, tipo) = await exportacion.DescargarAsync(primera.Id);
        using var ms = new MemoryStream(); await contenido.CopyToAsync(ms);
        Assert.StartsWith("text/csv", tipo);
        Assert.Equal(3, System.Text.Encoding.UTF8.GetString(ms.ToArray()).Split('\n', StringSplitOptions.RemoveEmptyEntries).Length); // cabecera + 2

        // RD-11: la segunda exportacion no encuentra nada nuevo
        var segunda = await exportacion.VistaPreviaAsync(clienteId, ejercicio, "4T", false);
        Assert.Empty(segunda.Nuevos);
        Assert.Equal(2, segunda.YaExportados);
        var ex = await Assert.ThrowsAsync<ExcepcionValidacion>(() => exportacion.ExportarAsync(clienteId, ejercicio, "4T", "CsvGenerico", false));
        Assert.Contains("RD-11", ex.Message);

        // Peticion explicita: se repite y queda marcada como repeticion
        var repetida = await exportacion.ExportarAsync(clienteId, ejercicio, "4T", "CsvGenerico", true);
        Assert.True(repetida.IncluyoExportados);
        Assert.Equal(2, repetida.NumRegistros);

        // A3 modelado pero no disponible
        var a3 = await Assert.ThrowsAsync<ExcepcionValidacion>(() => exportacion.ExportarAsync(clienteId, ejercicio, "4T", "A3", true));
        Assert.Contains("VERIFICAR", a3.Message);

        // El historico no se ve desde otro tenant
        using var otro = _app.AmbitoComo(FabricaAplicacion.TenantB);
        Assert.False(await otro.ServiceProvider.GetRequiredService<AsertaDbContext>().Exportaciones.AnyAsync(e => e.Id == primera.Id));
    }

    [Fact]
    public async Task Cuadro_de_mando_del_socio_responde_y_alerta_de_certificados()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        await PrepararClienteAsync(scope.ServiceProvider);
        var cuadro = await scope.ServiceProvider.GetRequiredService<ServicioCuadroMando>().ObtenerAsync();
        Assert.NotNull(cuadro.PorEstado);
        Assert.True(cuadro.ClientesActivos >= 1);
        // La tarea diaria de certificados no falla sin certificados vigentes
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<ServicioAlertaCertificados>().GenerarAsync());

        var http = _app.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });
        var html = await (await http.GetAsync("/Cuenta/Entrar")).Content.ReadAsStringAsync();
        var token = Regex.Match(html, "__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        await http.PostAsync("/Cuenta/Entrar", new FormUrlEncodedContent(new Dictionary<string, string> { ["Datos.Email"] = FabricaAplicacion.EmailTecnico(FabricaAplicacion.TenantA), ["Datos.Contrasena"] = FabricaAplicacion.ContrasenaTecnico, ["__RequestVerificationToken"] = token }));
        var r = await http.GetAsync("/CuadroMando");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("Obligaciones por estado", await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/Exportacion")).StatusCode);
    }
}
