using Aserta.Verifactu.Cliente;
using Aserta.Verifactu.Xml;

namespace Aserta.Verifactu.Tests;

public class SimuladorTests
{
    private static LoteRegistros Lote(int n) => new("89890001K", Enumerable.Range(1, n).Select(i => new RegistroParaEnvio(i, "89890001K", "ALTA", $"A/{i}", new string('A', 64), "<x/>")).ToList(), null);

    [Fact]
    public async Task Caido_lanza_error_tecnico_reintentable()
    {
        var estado = new EstadoSimuladorAeat();
        estado.Configurar(ModoSimulador.Caido, latenciaMs: 0);
        await Assert.ThrowsAsync<ExcepcionEnvioAeat>(() => new SimuladorAeat(estado).RemitirAsync(Lote(3)));
        Assert.Equal(0, estado.LotesRecibidos);
    }

    [Fact]
    public async Task Normal_acepta_todos_con_csv()
    {
        var estado = new EstadoSimuladorAeat();
        estado.Configurar(ModoSimulador.Normal, latenciaMs: 0);
        var r = await new SimuladorAeat(estado).RemitirAsync(Lote(3));
        Assert.All(r.Registros, x => { Assert.Equal(ResultadoRegistro.Aceptado, x.Resultado); Assert.NotNull(x.Csv); });
        Assert.Equal(1, estado.LotesRecibidos);
        Assert.Equal(3, estado.RegistrosRecibidos);
    }

    [Fact]
    public async Task Rechazo_y_aceptado_con_errores_devuelven_codigo()
    {
        var estado = new EstadoSimuladorAeat();
        estado.Configurar(ModoSimulador.Rechazo, latenciaMs: 0, codigoRechazo: "1234", descripcionRechazo: "prueba");
        var r = await new SimuladorAeat(estado).RemitirAsync(Lote(1));
        Assert.Equal(ResultadoRegistro.Rechazado, r.Registros[0].Resultado);
        Assert.Equal("1234", r.Registros[0].CodigoError);
        estado.Configurar(ModoSimulador.AceptadoConErrores);
        r = await new SimuladorAeat(estado).RemitirAsync(Lote(1));
        Assert.Equal(ResultadoRegistro.AceptadoConErrores, r.Registros[0].Resultado);
        Assert.NotNull(r.Registros[0].Csv);
    }

    [Fact]
    public async Task Espera_agresiva_devuelve_tiempo_de_espera()
    {
        var estado = new EstadoSimuladorAeat();
        estado.Configurar(ModoSimulador.EsperaAgresiva, latenciaMs: 0, tiempoEsperaSegundos: 90);
        var r = await new SimuladorAeat(estado).RemitirAsync(Lote(1));
        Assert.Equal(90, r.TiempoEsperaSegundos);
    }
}
