using Aserta.Dominio.Catalogo;

namespace Aserta.Dominio.Tests;

public class CalendarioHabilTests
{
    private static readonly CalendarioHabil Calendario = new([new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 6), new DateOnly(2026, 4, 3)]);

    [Fact]
    public void Dia_laborable_no_se_traslada()
    {
        Assert.Equal(new DateOnly(2026, 4, 20), Calendario.SiguienteHabil(new DateOnly(2026, 4, 20))); // lunes
    }

    [Fact]
    public void Sabado_se_traslada_al_lunes()
    {
        // 30/01/2027 es sabado => lunes 01/02/2027 (caso real del 4T del 303 de 2026)
        Assert.Equal(new DateOnly(2027, 2, 1), Calendario.SiguienteHabil(new DateOnly(2027, 1, 30)));
    }

    [Fact]
    public void Festivo_seguido_de_fin_de_semana_encadena_el_traslado()
    {
        // Viernes Santo 03/04/2026 => sabado, domingo => lunes 06/04/2026
        Assert.Equal(new DateOnly(2026, 4, 6), Calendario.SiguienteHabil(new DateOnly(2026, 4, 3)));
    }

    [Fact]
    public void Festivo_entre_semana()
    {
        Assert.Equal(new DateOnly(2027, 1, 7), Calendario.SiguienteHabil(new DateOnly(2027, 1, 6)));
    }
}

public class PeriodoTests
{
    [Theory]
    [InlineData("1T", 1, 1, 3, 31)]
    [InlineData("4T", 10, 1, 12, 31)]
    [InlineData("07", 7, 1, 7, 31)]
    [InlineData("02", 2, 1, 2, 28)]
    [InlineData("AN", 1, 1, 12, 31)]
    [InlineData("2P", 4, 1, 9, 30)]
    public void Rango_de_periodo(string periodo, int m1, int d1, int m2, int d2)
    {
        var (i, f) = Periodo.Rango(2026, periodo);
        Assert.Equal(new DateOnly(2026, m1, d1), i);
        Assert.Equal(new DateOnly(2026, m2, d2), f);
    }

    [Fact]
    public void Periodicidad_genera_los_codigos_correctos()
    {
        Assert.Equal(4, Periodo.DePeriodicidad(Periodicidad.Trimestral).Count);
        Assert.Equal(12, Periodo.DePeriodicidad(Periodicidad.Mensual).Count);
        Assert.Single(Periodo.DePeriodicidad(Periodicidad.Anual));
        Assert.Equal(3, Periodo.DePeriodicidad(Periodicidad.PagoFraccionado).Count);
    }

    [Fact]
    public void Periodo_desconocido_lanza()
    {
        Assert.Throws<ArgumentException>(() => Periodo.Rango(2026, "5T"));
    }
}
