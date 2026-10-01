using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Sga.Web.Infraestructura;

/// <summary>
/// Configuracion SEO. El dominio canonico definitivo es sga.es, pero mientras el sitio responda en otro
/// host (la demo en sga.winsoft.es, el puerto local) se sirve "noindex" y un robots.txt que lo deniega todo.
/// El dia que sga.es apunte aqui no hay que tocar codigo: la comprobacion es por el Host de cada peticion.
/// </summary>
public sealed class OpcionesSeo
{
    /// <summary>Con esquema y sin barra final, p. ej. https://sga.es</summary>
    public string DominioCanonico { get; set; } = "https://sga.es";
    /// <summary>Codigo de la etiqueta google-site-verification (Search Console); vacio = no se emite.</summary>
    public string VerificacionGoogle { get; set; } = "";
}

public sealed class Seo(IOptions<OpcionesSeo> opciones)
{
    /// <summary>Rutas publicas que entran en el mapa del sitio (las privadas y de acceso quedan fuera).</summary>
    public static readonly string[] RutasPublicas =
    [
        "/", "/servicios", "/como-trabajamos", "/equipo", "/contacto",
        "/alcorcon/alquiler-de-local-impuestos", "/alcorcon/autonomo-plazos-del-trimestre", "/alcorcon/sociedad-obligaciones-fiscales",
    ];

    public string Dominio => opciones.Value.DominioCanonico.TrimEnd('/');
    public string VerificacionGoogle => opciones.Value.VerificacionGoogle;

    /// <summary>true si la peticion llega por el host del dominio canonico (sga.es); false en la demo o en local.</summary>
    public bool EsIndexable(HttpRequest peticion) => EsHostCanonico(peticion.Host.Host, Dominio);

    /// <summary>URL canonica absoluta de una ruta, siempre sobre el dominio definitivo.</summary>
    public string Canonica(PathString ruta) => Canonica(Dominio, ruta.HasValue ? ruta.Value! : "/");

    public static string Canonica(string dominio, string ruta)
    {
        dominio = dominio.TrimEnd('/');
        if (!ruta.StartsWith('/')) ruta = "/" + ruta;
        if (ruta.Length > 1) ruta = ruta.TrimEnd('/');
        return dominio + ruta;
    }

    public static bool EsHostCanonico(string? host, string dominioCanonico)
    {
        if (string.IsNullOrWhiteSpace(host) || !Uri.TryCreate(dominioCanonico, UriKind.Absolute, out var canonico)) return false;
        var h = host.Trim().ToLowerInvariant();
        var dosPuntos = h.IndexOf(':');
        if (dosPuntos > 0 && !h.StartsWith('[')) h = h[..dosPuntos];
        return h == canonico.Host.ToLowerInvariant();
    }

    public static string Robots(bool indexable, string dominio) => indexable
        ? "User-agent: *\nDisallow: /cliente/\nDisallow: /gestor/\nDisallow: /acceso\nDisallow: /salir\nAllow: /\n\nSitemap: " + Canonica(dominio, "/sitemap.xml") + "\n"
        : "# Este host no es el dominio definitivo: no indexar.\nUser-agent: *\nDisallow: /\n";

    public static string Sitemap(string dominio, IEnumerable<string> rutas)
    {
        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");
        foreach (var r in rutas) sb.Append("  <url><loc>").Append(System.Security.SecurityElement.Escape(Canonica(dominio, r))).Append("</loc></url>\n");
        return sb.Append("</urlset>\n").ToString();
    }

    private static readonly JsonSerializerOptions OpcionesJson = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = false };

    /// <summary>Datos estructurados schema.org del negocio local (AccountingService es el tipo especifico de gestoria/asesoria).</summary>
    public string DatosLocalesJson(OpcionesSga sga)
    {
        var horarios = sga.Horarios.Select(h => new Dictionary<string, object>
        {
            ["@type"] = "OpeningHoursSpecification",
            ["dayOfWeek"] = h.Dias.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            ["opens"] = h.Abre, ["closes"] = h.Cierra,
        }).ToArray();
        var datos = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "AccountingService",
            ["@id"] = Dominio + "/#negocio",
            ["name"] = sga.Nombre,
            ["url"] = Dominio + "/",
            ["image"] = Dominio + "/img/sga-tarjeta.png",
            ["logo"] = Dominio + "/img/sga-logo.svg",
            ["telephone"] = "+34 " + sga.Telefono,
            ["email"] = sga.Email,
            ["address"] = new Dictionary<string, object>
            {
                ["@type"] = "PostalAddress", ["streetAddress"] = sga.Direccion, ["postalCode"] = sga.CodigoPostal,
                ["addressLocality"] = sga.Localidad, ["addressRegion"] = "Madrid", ["addressCountry"] = "ES",
            },
            ["areaServed"] = new[] { new Dictionary<string, object> { ["@type"] = "City", ["name"] = sga.Localidad } },
            ["openingHoursSpecification"] = horarios.Length > 0 ? horarios : null,
            ["knowsLanguage"] = "es",
        };
        // El serializador escapa < > & por defecto: el bloque no puede cerrar la etiqueta script aunque un dato lleve HTML.
        return JsonSerializer.Serialize(datos, OpcionesJson);
    }
}
