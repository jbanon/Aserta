using Aserta.Aplicacion.Clientes;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Facturacion;
using Aserta.Dominio.Nucleo;
using Aserta.Infraestructura.Facturacion;
using Aserta.Infraestructura.Persistencia;
using Aserta.Verifactu.Cliente;
using Aserta.Verifactu.Huella;
using Aserta.Verifactu.Servicios;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aserta.Integracion.Tests;

/// <summary>
/// Definicion de hecho de huella-y-vectores-prueba.md §7 y envio-y-cola-reintentos.md §8
/// sobre la base real: cadena verificable, inalterabilidad (RD-06), simulador caido
/// => todo se emite y queda en cola => al restaurar todo llega a ACEPTADO,
/// rechazo sin reintento, concurrencia sin huecos.
/// </summary>
[Collection("aplicacion")]
public class FlujoVerifactuTests
{
    private readonly FabricaAplicacion _app;
    public FlujoVerifactuTests(FabricaAplicacion app) => _app = app;

    private async Task<(Guid ClienteId, Guid UsuarioId)> EmisorDePruebaAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AsertaDbContext>();
        var cliente = await db.Clientes.Include(c => c.Perfiles).FirstAsync(c => c.GestoriaId == FabricaAplicacion.TenantA);
        sp.GetRequiredService<ContextoEjecucion>().EstablecerUsuario(cliente.AsesorResponsableId, FabricaAplicacion.TenantA, null, "test", [Roles.SocioDirector], "127.0.0.1", "tests");
        if (cliente.Perfiles.Count == 0)
            await sp.GetRequiredService<ServicioClientes>().NuevaVersionPerfilAsync(cliente.Id, new DatosPerfilFiscal { VigenteDesde = cliente.FechaAlta, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral });
        var gestoria = await db.Gestorias.FirstAsync(g => g.Id == FabricaAplicacion.TenantA);
        if (!await db.Certificados.AnyAsync(c => c.Tipo == TipoCertificado.Gestoria))
            db.Certificados.Add(new Certificado { Id = Guid.NewGuid(), GestoriaId = FabricaAplicacion.TenantA, NifTitular = gestoria.Nif, Tipo = TipoCertificado.Gestoria, Alias = "test", HuellaDigital = new string('F', 64), ValidoDesde = new DateOnly(2026, 1, 1), ValidoHasta = new DateOnly(2030, 1, 1) });
        if (!await db.Apoderamientos.AnyAsync(a => a.ClienteId == cliente.Id))
            db.Apoderamientos.Add(new Apoderamiento { Id = Guid.NewGuid(), GestoriaId = FabricaAplicacion.TenantA, ClienteId = cliente.Id, FechaAlta = new DateOnly(2026, 1, 1) });
        await db.SaveChangesAsync();
        return (cliente.Id, cliente.AsesorResponsableId);
    }

    private static FacturaNueva Factura(Guid emisor, decimal precio = 100m) => new()
    {
        ClienteEmisorId = emisor, DestinatarioNif = "12345678Z", DestinatarioNombre = "Cliente de prueba", Descripcion = "Prueba",
        Lineas = [new LineaNueva { Descripcion = "Servicio", Cantidad = 1, PrecioUnitario = precio, TipoIva = 21m }]
    };

    [Fact]
    public async Task Emision_encadena_huellas_verificables_y_encola_el_envio()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var sp = scope.ServiceProvider;
        var (emisor, usuario) = await EmisorDePruebaAsync(sp);
        var emision = sp.GetRequiredService<ServicioEmision>();
        var r1 = await emision.EmitirAsync(Factura(emisor), usuario);
        var r2 = await emision.EmitirAsync(Factura(emisor, 50m), usuario);

        Assert.Equal(r1.NumeroEnCadena + 1, r2.NumeroEnCadena);
        Assert.Equal(EstadoEnvio.EN_COLA, r1.EstadoInicial);
        Assert.Equal(121m, r1.Factura.ImporteTotal);
        Assert.Contains("numserie=", r1.UrlQr);

        var db = sp.GetRequiredService<AsertaDbContext>();
        var reg2 = await db.RegistrosFacturacion.AsNoTracking().FirstAsync(r => r.Id == r2.RegistroId);
        Assert.Equal(r1.Huella, reg2.HuellaAnterior);
        Assert.Equal(reg2.Huella, CalculadoraHuella.CalcularHuella(reg2.CadenaHuella));   // reverificable desde la cadena guardada
        Assert.Contains($"&Huella={r1.Huella}&", reg2.CadenaHuella);
        Assert.True(await db.EnviosPendientes.AnyAsync(p => p.RegistroFacturacionId == r1.RegistroId));   // outbox en la misma transaccion
    }

    [Fact]
    public async Task Las_facturas_emitidas_son_inalterables_incluso_para_db_owner()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var sp = scope.ServiceProvider;
        var (emisor, usuario) = await EmisorDePruebaAsync(sp);
        var r = await sp.GetRequiredService<ServicioEmision>().EmitirAsync(Factura(emisor), usuario);
        var db = sp.GetRequiredService<AsertaDbContext>();
        var exU = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync("UPDATE vf.FacturaEmitida SET ImporteTotal = 1 WHERE Id = {0}", r.Factura.Id));
        Assert.Contains("inalterables", (exU as SqlException ?? exU.InnerException as SqlException)?.Message ?? exU.Message);
        var exD = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM vf.RegistroFacturacion WHERE Id = {0}", r.RegistroId));
        Assert.Contains("inalterables", (exD as SqlException ?? exD.InnerException as SqlException)?.Message ?? exD.Message);
    }

    [Fact]
    public async Task Con_la_AEAT_caida_todo_se_emite_y_queda_en_cola_y_al_restaurar_todo_se_acepta()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var sp = scope.ServiceProvider;
        var (emisor, usuario) = await EmisorDePruebaAsync(sp);
        var simulador = sp.GetRequiredService<EstadoSimuladorAeat>();
        var worker = sp.GetRequiredService<EnvioVerifactuWorker>();
        var db = sp.GetRequiredService<AsertaDbContext>();
        var emision = sp.GetRequiredService<ServicioEmision>();

        simulador.Configurar(ModoSimulador.Caido, latenciaMs: 0);
        var ids = new List<long>();
        for (int i = 0; i < 3; i++) ids.Add((await emision.EmitirAsync(Factura(emisor, 10m + i), usuario)).RegistroId);

        await worker.CicloAsync(CancellationToken.None);
        var estados = await db.EstadosEnvio.AsNoTracking().Where(e => ids.Contains(e.RegistroFacturacionId)).ToListAsync();
        Assert.All(estados, e => Assert.Equal(EstadoEnvio.ERROR_TECNICO, e.Estado));
        Assert.All(estados, e => Assert.Equal(1, e.Intentos));
        var colas = await db.EnviosPendientes.AsNoTracking().Where(p => ids.Contains(p.RegistroFacturacionId)).ToListAsync();
        Assert.All(colas, c => { Assert.Equal(EstadoEnvioPendiente.PENDIENTE, c.Estado); Assert.True(c.ProximoIntentoUtc > DateTime.UtcNow.AddSeconds(10)); }); // retroceso

        // Restaurar: adelantamos el proximo intento y procesamos
        simulador.Configurar(ModoSimulador.Normal, latenciaMs: 0);
        await db.EnviosPendientes.Where(p => ids.Contains(p.RegistroFacturacionId)).ExecuteUpdateAsync(s => s.SetProperty(p => p.ProximoIntentoUtc, DateTime.UtcNow.AddMinutes(-1)));
        await worker.CicloAsync(CancellationToken.None);
        estados = await db.EstadosEnvio.AsNoTracking().Where(e => ids.Contains(e.RegistroFacturacionId)).ToListAsync();
        Assert.All(estados, e => { Assert.Equal(EstadoEnvio.ACEPTADO, e.Estado); Assert.NotNull(e.CsvAeat); });
        Assert.All(await db.EnviosPendientes.AsNoTracking().Where(p => ids.Contains(p.RegistroFacturacionId)).ToListAsync(), c => Assert.Equal(EstadoEnvioPendiente.COMPLETADO, c.Estado));
    }

    [Fact]
    public async Task Rechazo_no_se_reintenta_y_anulacion_ocupa_su_posicion_en_la_cadena()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var sp = scope.ServiceProvider;
        var (emisor, usuario) = await EmisorDePruebaAsync(sp);
        var simulador = sp.GetRequiredService<EstadoSimuladorAeat>();
        var emision = sp.GetRequiredService<ServicioEmision>();
        var db = sp.GetRequiredService<AsertaDbContext>();
        simulador.Configurar(ModoSimulador.Rechazo, latenciaMs: 0, codigoRechazo: "9999");
        var r = await emision.EmitirAsync(Factura(emisor), usuario);
        await sp.GetRequiredService<EnvioVerifactuWorker>().CicloAsync(CancellationToken.None);
        var e = await db.EstadosEnvio.AsNoTracking().FirstAsync(x => x.RegistroFacturacionId == r.RegistroId);
        Assert.Equal(EstadoEnvio.RECHAZADO, e.Estado);
        Assert.Equal("9999", e.CodigoErrorAeat);
        Assert.Equal(EstadoEnvioPendiente.COMPLETADO, (await db.EnviosPendientes.AsNoTracking().FirstAsync(p => p.RegistroFacturacionId == r.RegistroId)).Estado);

        simulador.Configurar(ModoSimulador.Normal, latenciaMs: 0);
        var anulacion = await emision.AnularAsync(r.Factura.Id, usuario);
        Assert.Equal(r.NumeroEnCadena + 1, anulacion.NumeroEnCadena);
        Assert.Equal(r.Huella, (await db.RegistrosFacturacion.AsNoTracking().FirstAsync(x => x.Id == anulacion.RegistroId)).HuellaAnterior);
        await Assert.ThrowsAsync<ExcepcionFacturacion>(() => emision.AnularAsync(r.Factura.Id, usuario));
        var f = await db.FacturasEmitidas.AsNoTracking().FirstAsync(x => x.Id == r.Factura.Id);
        Assert.Equal(121m, f.ImporteTotal);   // la factura original sigue intacta
    }

    [Fact]
    public async Task Veinte_emisiones_simultaneas_del_mismo_emisor_no_dejan_huecos_ni_duplicados()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        Guid emisor; Guid usuario;
        using (var scope = _app.AmbitoComo(FabricaAplicacion.TenantA)) (emisor, usuario) = await EmisorDePruebaAsync(scope.ServiceProvider);

        long antes;
        using (var scope = _app.AmbitoComo(FabricaAplicacion.TenantA))
            antes = (await scope.ServiceProvider.GetRequiredService<AsertaDbContext>().CadenasEmisor.AsNoTracking().FirstAsync(c => c.ClienteEmisorId == emisor)).UltimoNumero;

        var tareas = Enumerable.Range(0, 20).Select(async i =>
        {
            using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
            scope.ServiceProvider.GetRequiredService<ContextoEjecucion>().EstablecerUsuario(usuario, FabricaAplicacion.TenantA, null, "test", [Roles.SocioDirector], "127.0.0.1", "tests");
            return (await scope.ServiceProvider.GetRequiredService<ServicioEmision>().EmitirAsync(Factura(emisor, 1m + i), usuario)).NumeroEnCadena;
        });
        var numeros = (await Task.WhenAll(tareas)).Order().ToList();
        Assert.Equal(Enumerable.Range(1, 20).Select(i => antes + i).ToList(), numeros);

        using var verificacion = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var db = verificacion.ServiceProvider.GetRequiredService<AsertaDbContext>();
        var nif = (await db.Clientes.AsNoTracking().FirstAsync(c => c.Id == emisor)).Nif;
        var registros = await db.RegistrosFacturacion.AsNoTracking().Where(r => r.NifEmisor == nif && r.NumeroEnCadena > antes).OrderBy(r => r.NumeroEnCadena).ToListAsync();
        string? previa = registros.First().HuellaAnterior;
        foreach (var r in registros)
        {
            Assert.Equal(previa, r.HuellaAnterior);
            Assert.Equal(r.Huella, CalculadoraHuella.CalcularHuella(r.CadenaHuella));
            previa = r.Huella;
        }
    }

    [Fact]
    public async Task Sin_apoderamiento_la_factura_se_emite_pero_queda_bloqueada()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var sp = scope.ServiceProvider;
        var (emisor, usuario) = await EmisorDePruebaAsync(sp);
        var db = sp.GetRequiredService<AsertaDbContext>();
        foreach (var a in await db.Apoderamientos.Where(a => a.ClienteId == emisor).ToListAsync()) a.FechaFin = new DateOnly(2026, 1, 2);
        await db.SaveChangesAsync();
        try
        {
            var r = await sp.GetRequiredService<ServicioEmision>().EmitirAsync(Factura(emisor), usuario);
            Assert.Equal(EstadoEnvio.BLOQUEADO_SIN_CERTIFICADO, r.EstadoInicial);
            Assert.NotNull(await db.FacturasEmitidas.AsNoTracking().FirstOrDefaultAsync(f => f.Id == r.Factura.Id));   // se emite igualmente
        }
        finally
        {
            foreach (var a in await db.Apoderamientos.Where(a => a.ClienteId == emisor).ToListAsync()) a.FechaFin = null;
            await db.SaveChangesAsync();
        }
    }
}
