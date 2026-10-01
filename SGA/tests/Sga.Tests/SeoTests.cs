using System.Text.Json;
using Microsoft.Extensions.Options;
using Sga.Web.Infraestructura;

namespace Sga.Tests;

/// <summary>
/// Mecanismo de dominio canonico (encargo 03): mientras el sitio responda fuera de sga.es no se indexa nada;
/// en sga.es se indexa con normalidad sin tocar codigo.
/// </summary>
public class SeoTests
{
    private const string Dominio = "https://sga.es";

    [Theory]
    [InlineData("sga.es", true)]
    [InlineData("SGA.ES", true)]
    [InlineData("sga.es:443", true)]
    [InlineData("www.sga.es", false)]
    [InlineData("sga.winsoft.es", false)]
    [InlineData("127.0.0.1:5120", false)]
    [InlineData("localhost", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Solo_el_host_del_dominio_canonico_es_indexable(string? host, bool esperado) =>
        Assert.Equal(esperado, Seo.EsHostCanonico(host, Dominio));

    [Fact]
    public void Fuera_del_dominio_canonico_robots_lo_deniega_todo_y_no_anuncia_sitemap()
    {
        var robots = Seo.Robots(indexable: false, Dominio);
        Assert.Contains("Disallow: /\n", robots);
        Assert.DoesNotContain("Sitemap:", robots);
    }

    [Fact]
    public void En_el_dominio_canonico_robots_permite_la_web_publica_y_protege_las_areas_privadas()
    {
        var robots = Seo.Robots(indexable: true, Dominio);
        Assert.Contains("Disallow: /cliente/", robots);
        Assert.Contains("Disallow: /gestor/", robots);
        Assert.Contains("Sitemap: https://sga.es/sitemap.xml", robots);
        Assert.DoesNotContain("Disallow: /\n", robots);
    }

    [Fact]
    public void El_sitemap_lleva_todas_las_rutas_publicas_sobre_el_dominio_canonico()
    {
        var xml = Seo.Sitemap(Dominio + "/", Seo.RutasPublicas);
        Assert.Contains("<loc>https://sga.es/</loc>", xml);
        Assert.Contains("<loc>https://sga.es/contacto</loc>", xml);
        Assert.Contains("<loc>https://sga.es/alcorcon/autonomo-plazos-del-trimestre</loc>", xml);
        Assert.Equal(Seo.RutasPublicas.Length, xml.Split("<url>").Length - 1);
        Assert.DoesNotContain("winsoft", xml);
    }

    [Theory]
    [InlineData("/", "https://sga.es/")]
    [InlineData("/servicios/", "https://sga.es/servicios")]
    [InlineData("contacto", "https://sga.es/contacto")]
    public void La_url_canonica_siempre_apunta_al_dominio_definitivo(string ruta, string esperado) =>
        Assert.Equal(esperado, Seo.Canonica(Dominio + "/", ruta));

    [Fact]
    public void Los_datos_estructurados_describen_una_gestoria_en_Alcorcon_y_no_pueden_cerrar_la_etiqueta_script()
    {
        var seo = new Seo(Options.Create(new OpcionesSeo { DominioCanonico = Dominio }));
        var sga = new OpcionesSga
        {
            Nombre = "SGA Contabilizado", Direccion = "Calle Mayor, 12 </script><b>", CodigoPostal = "28921", Localidad = "Alcorcón",
            Telefono = "916 000 000", Email = "hola@sga.es",
            Horarios = [new() { Dias = "Monday, Tuesday", Abre = "09:00", Cierra = "18:00" }],
        };
        var json = seo.DatosLocalesJson(sga);
        Assert.DoesNotContain("</script", json, StringComparison.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(json);
        var raiz = doc.RootElement;
        Assert.Equal("AccountingService", raiz.GetProperty("@type").GetString());
        Assert.Equal("https://sga.es/", raiz.GetProperty("url").GetString());
        Assert.Equal("Alcorcón", raiz.GetProperty("address").GetProperty("addressLocality").GetString());
        Assert.Equal("ES", raiz.GetProperty("address").GetProperty("addressCountry").GetString());
        var tramo = raiz.GetProperty("openingHoursSpecification")[0];
        Assert.Equal(2, tramo.GetProperty("dayOfWeek").GetArrayLength());
        Assert.Equal("09:00", tramo.GetProperty("opens").GetString());
    }
}
