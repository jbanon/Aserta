using Aserta.Infraestructura.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aserta.Integracion.Tests;

/// <summary>ADR-001 §5.5: la doble barrera. Es el test que demuestra que RLS funciona aunque EF se salte sus filtros.</summary>
[Collection("aplicacion")]
public class AislamientoTenantTests
{
    private readonly FabricaAplicacion _app;
    public AislamientoTenantTests(FabricaAplicacion app) => _app = app;

    [Fact]
    public async Task Cada_tenant_ve_solo_sus_clientes_con_filtros_EF()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var a = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var clientesA = await a.ServiceProvider.GetRequiredService<AsertaDbContext>().Clientes.ToListAsync();
        Assert.NotEmpty(clientesA);
        Assert.All(clientesA, c => Assert.Equal(FabricaAplicacion.TenantA, c.GestoriaId));
    }

    [Fact]
    public async Task RLS_bloquea_aunque_se_ignoren_los_filtros_de_EF()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var a = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var db = a.ServiceProvider.GetRequiredService<AsertaDbContext>();
        var deB = await db.Clientes.IgnoreQueryFilters().Where(c => c.GestoriaId == FabricaAplicacion.TenantB).ToListAsync();
        Assert.Empty(deB); // la segunda barrera
        var total = await db.Clientes.IgnoreQueryFilters().CountAsync();
        Assert.All(await db.Clientes.IgnoreQueryFilters().ToListAsync(), c => Assert.Equal(FabricaAplicacion.TenantA, c.GestoriaId));
        Assert.True(total >= 1);
    }

    [Fact]
    public async Task Sin_tenant_en_el_contexto_no_se_ve_nada()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var sin = _app.AmbitoComo(null);
        var db = sin.ServiceProvider.GetRequiredService<AsertaDbContext>();
        Assert.Equal(0, await db.Clientes.IgnoreQueryFilters().CountAsync());
        Assert.Equal(0, await db.Gestorias.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Alternar_tenants_sobre_el_mismo_pool_no_filtra_el_contexto_anterior()
    {
        // Riesgo M1 de 04-modelo-datos.md: una conexion que vuelve al pool con el SESSION_CONTEXT de otro tenant.
        await _app.AsegurarTenantsDePruebaAsync();
        for (int i = 0; i < 5; i++)
        {
            using (var a = _app.AmbitoComo(FabricaAplicacion.TenantA))
                Assert.All(await a.ServiceProvider.GetRequiredService<AsertaDbContext>().Clientes.IgnoreQueryFilters().ToListAsync(), c => Assert.Equal(FabricaAplicacion.TenantA, c.GestoriaId));
            using (var b = _app.AmbitoComo(FabricaAplicacion.TenantB))
                Assert.All(await b.ServiceProvider.GetRequiredService<AsertaDbContext>().Clientes.IgnoreQueryFilters().ToListAsync(), c => Assert.Equal(FabricaAplicacion.TenantB, c.GestoriaId));
        }
    }

    [Fact]
    public async Task BLOCK_PREDICATE_impide_insertar_en_nombre_de_otro_tenant()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var a = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var db = a.ServiceProvider.GetRequiredService<AsertaDbContext>();
        var asesorB = await db.Database.SqlQueryRaw<Guid>("SELECT Id AS Value FROM dbo.Usuario WHERE GestoriaId = {0}", FabricaAplicacion.TenantB).ToListAsync();
        // Desde A ni siquiera vemos a los usuarios de B (RLS), asi que tomamos cualquier Guid: la insercion debe fallar por el predicado de bloqueo.
        db.Clientes.Add(new Dominio.Clientes.Cliente
        {
            Id = Guid.NewGuid(), GestoriaId = FabricaAplicacion.TenantB, Nif = "B22222222", RazonSocial = "Intruso", FormaJuridica = Dominio.Clientes.FormaJuridica.SL,
            AsesorResponsableId = asesorB.FirstOrDefault(), FechaAlta = new DateOnly(2026, 1, 1)
        });
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
        Assert.Contains("BLOCK PREDICATE", (ex.InnerException ?? ex).Message + (ex.InnerException as SqlException)?.Message, StringComparison.OrdinalIgnoreCase);
    }
}
