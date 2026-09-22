using Aserta.Infraestructura.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aserta.Integracion.Tests;

/// <summary>RD-08: auditoria e historial son de solo anexado, incluso para db_owner.</summary>
[Collection("aplicacion")]
public class SoloAnexadoTests
{
    private readonly FabricaAplicacion _app;
    public SoloAnexadoTests(FabricaAplicacion app) => _app = app;

    [Fact]
    public async Task No_se_puede_modificar_ni_borrar_la_auditoria()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var a = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var db = a.ServiceProvider.GetRequiredService<AsertaDbContext>();

        var fila = new Dominio.Nucleo.Auditoria { GestoriaId = FabricaAplicacion.TenantA, FechaUtc = DateTime.UtcNow, Accion = "Prueba", EntidadTipo = "Test", EntidadId = Guid.NewGuid().ToString() };
        db.Auditorias.Add(fila);
        await db.SaveChangesAsync();

        var exU = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync("UPDATE dbo.Auditoria SET Accion = 'Alterada' WHERE Id = {0}", fila.Id));
        Assert.Contains("solo anexado", ((exU as SqlException) ?? exU.InnerException as SqlException)?.Message ?? exU.Message);

        var exD = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM dbo.Auditoria WHERE Id = {0}", fila.Id));
        Assert.Contains("solo anexado", ((exD as SqlException) ?? exD.InnerException as SqlException)?.Message ?? exD.Message);

        var sigue = await db.Auditorias.AsNoTracking().FirstAsync(x => x.Id == fila.Id);
        Assert.Equal("Prueba", sigue.Accion);
    }
}
