using Aserta.Infraestructura.Migraciones;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aserta.Integracion.Tests;

[Collection("aplicacion")]
public class RunnerMigracionesTests
{
    private readonly FabricaAplicacion _app;
    public RunnerMigracionesTests(FabricaAplicacion app) => _app = app;

    private string Cadena() => Aserta.Infraestructura.ExtensionesServicios.CadenaConexion(_app.Services.GetRequiredService<IConfiguration>());

    [Fact]
    public async Task Segunda_ejecucion_no_aplica_nada()
    {
        var runner = new RunnerMigraciones(Cadena(), Path.Combine(FabricaAplicacion.RaizRepo(), "Scripts", "Migrations"), NullLogger<RunnerMigraciones>.Instance);
        var r = await runner.AplicarAsync();
        Assert.Equal(0, r.Aplicados);
        Assert.True(r.Omitidos >= 4);
    }

    [Fact]
    public async Task Script_ya_aplicado_con_contenido_distinto_aborta()
    {
        var temporal = Path.Combine(Path.GetTempPath(), "aserta-runner-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporal);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(temporal, "0001_nucleo.sql"), "-- contenido alterado\nSELECT 1;");
            var runner = new RunnerMigraciones(Cadena(), temporal, NullLogger<RunnerMigraciones>.Instance);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.AplicarAsync());
            Assert.Contains("no se edita", ex.Message);
        }
        finally { Directory.Delete(temporal, true); }
    }

    [Fact]
    public void Los_scripts_del_repositorio_estan_numerados_y_sin_huecos()
    {
        var nombres = Directory.GetFiles(Path.Combine(FabricaAplicacion.RaizRepo(), "Scripts", "Migrations"), "*.sql").Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList();
        for (int i = 0; i < nombres.Count; i++)
            Assert.StartsWith($"{i + 1:0000}_", nombres[i]!);
    }

    [Fact]
    public void El_hash_es_estable_ante_cambios_de_salto_de_linea()
    {
        Assert.Equal(RunnerMigraciones.CalcularHash("SELECT 1;\r\nGO\r\n"), RunnerMigraciones.CalcularHash("SELECT 1;\nGO\n"));
        Assert.NotEqual(RunnerMigraciones.CalcularHash("SELECT 1;"), RunnerMigraciones.CalcularHash("SELECT 2;"));
    }

    [Fact]
    public void Divide_en_lotes_por_GO()
    {
        var lotes = RunnerMigraciones.DividirEnLotes("A\nGO\nB\ngo\n\nC\nGO 2\n").ToList();
        Assert.Equal(["A", "B", "C"], lotes);
    }
}
