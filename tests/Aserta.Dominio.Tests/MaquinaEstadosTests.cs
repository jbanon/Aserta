using Aserta.Dominio.Comun;
using Aserta.Dominio.Obligaciones;

namespace Aserta.Dominio.Tests;

public class MaquinaEstadosTests
{
    private static Obligacion Nueva(EstadoObligacion estado) => new()
    {
        Id = Guid.NewGuid(), GestoriaId = Guid.NewGuid(), ClienteId = Guid.NewGuid(), ModeloCodigo = "303", Ejercicio = 2026, Periodo = "1T",
        Estado = estado, FechaLimitePresentacion = new DateOnly(2026, 4, 20), FechaLimiteDomiciliacion = new DateOnly(2026, 4, 15)
    };

    [Theory]
    [InlineData(EstadoObligacion.PendienteDocumentacion, EstadoObligacion.DocumentacionCompleta)]
    [InlineData(EstadoObligacion.DocumentacionCompleta, EstadoObligacion.EnPreparacion)]
    [InlineData(EstadoObligacion.DocumentacionCompleta, EstadoObligacion.PendienteDocumentacion)]
    [InlineData(EstadoObligacion.EnPreparacion, EstadoObligacion.RevisionInterna)]
    [InlineData(EstadoObligacion.RevisionInterna, EstadoObligacion.EnPreparacion)]
    [InlineData(EstadoObligacion.RevisionInterna, EstadoObligacion.PendienteAprobacionCliente)]
    [InlineData(EstadoObligacion.PendienteAprobacionCliente, EstadoObligacion.EnPreparacion)]
    [InlineData(EstadoObligacion.Presentado, EstadoObligacion.Cerrado)]
    [InlineData(EstadoObligacion.PendienteDocumentacion, EstadoObligacion.NoAplica)]
    public void Transiciones_validas(EstadoObligacion origen, EstadoObligacion destino)
    {
        Assert.True(MaquinaEstadosObligacion.EsTransicionValida(origen, destino));
    }

    [Theory]
    [InlineData(EstadoObligacion.PendienteDocumentacion, EstadoObligacion.Presentado)]
    [InlineData(EstadoObligacion.EnPreparacion, EstadoObligacion.Cerrado)]
    [InlineData(EstadoObligacion.Cerrado, EstadoObligacion.Presentado)]
    [InlineData(EstadoObligacion.NoAplica, EstadoObligacion.PendienteDocumentacion)]
    [InlineData(EstadoObligacion.EnPreparacion, EstadoObligacion.NoAplica)]
    public void Transiciones_invalidas(EstadoObligacion origen, EstadoObligacion destino)
    {
        Assert.False(MaquinaEstadosObligacion.EsTransicionValida(origen, destino));
        var o = Nueva(origen);
        Assert.Throws<ExcepcionDominio>(() => o.CambiarEstado(destino, true, null, DateTime.UtcNow));
    }

    [Fact]
    public void RD09_no_se_presenta_sin_aprobacion_si_la_gestoria_la_exige()
    {
        var o = Nueva(EstadoObligacion.PendienteAprobacionCliente);
        var ex = Assert.Throws<ExcepcionDominio>(() => o.CambiarEstado(EstadoObligacion.Presentado, gestoriaExigeAprobacionCliente: true, null, DateTime.UtcNow));
        Assert.Contains("RD-09", ex.Message);
        Assert.Equal(EstadoObligacion.PendienteAprobacionCliente, o.Estado);
    }

    [Fact]
    public void RD09_con_aprobacion_registrada_si_se_presenta()
    {
        var o = Nueva(EstadoObligacion.PendienteAprobacionCliente);
        o.RegistrarAprobacionCliente(Guid.NewGuid(), DateTime.UtcNow);
        o.CambiarEstado(EstadoObligacion.Presentado, true, Guid.NewGuid(), DateTime.UtcNow);
        Assert.Equal(EstadoObligacion.Presentado, o.Estado);
        Assert.Equal(2, o.Historial.Count);
        Assert.Equal(TiposEventoHistorial.CambioEstado, o.Historial[^1].TipoEvento);
    }

    [Fact]
    public void RD09_desactivado_por_el_tenant_permite_presentar_sin_aprobacion()
    {
        var o = Nueva(EstadoObligacion.PendienteAprobacionCliente);
        o.CambiarEstado(EstadoObligacion.Presentado, gestoriaExigeAprobacionCliente: false, null, DateTime.UtcNow);
        Assert.Equal(EstadoObligacion.Presentado, o.Estado);
    }

    [Fact]
    public void Semaforo_usa_la_fecha_de_domiciliacion()
    {
        var o = Nueva(EstadoObligacion.EnPreparacion);
        Assert.Equal(ColorSemaforo.Verde, Semaforo.Calcular(o, new DateOnly(2026, 4, 1)));
        Assert.Equal(ColorSemaforo.Ambar, Semaforo.Calcular(o, new DateOnly(2026, 4, 9)));
        Assert.Equal(ColorSemaforo.Rojo, Semaforo.Calcular(o, new DateOnly(2026, 4, 13)));
        Assert.Equal(ColorSemaforo.Rojo, Semaforo.Calcular(o, new DateOnly(2026, 4, 15)));
        Assert.Equal(ColorSemaforo.Vencido, Semaforo.Calcular(o, new DateOnly(2026, 4, 16))); // aunque la presentacion aun no venza
        o.Estado = EstadoObligacion.Presentado;
        Assert.Equal(ColorSemaforo.Gris, Semaforo.Calcular(o, new DateOnly(2026, 4, 16)));
    }

    [Fact]
    public void Semaforo_cae_a_presentacion_si_no_hay_domiciliacion()
    {
        var o = Nueva(EstadoObligacion.EnPreparacion);
        o.FechaLimiteDomiciliacion = null;
        Assert.Equal(new DateOnly(2026, 4, 20), Semaforo.FechaDeReferencia(o));
    }
}
