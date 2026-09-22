using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Aserta.Verifactu.Xml;

/// <summary>
/// Valida el XML literal contra los XSD oficiales versionados en Esquemas/. El
/// import de xmldsig se resuelve contra la copia local para no depender de
/// internet en el momento del envio. Si faltan los esquemas, Disponible = false
/// y el envio al simulador continua; contra la AEAT real la validacion es
/// obligatoria (envio-y-cola-reintentos.md §3).
/// </summary>
public sealed class ValidadorXsd
{
    private readonly XmlSchemaSet? _esquemas;
    public bool Disponible => _esquemas is not null;
    public string CarpetaEsquemas { get; }
    public string? ErrorCarga { get; }

    public static string CarpetaPorDefecto => Path.Combine(AppContext.BaseDirectory, "Esquemas");

    public ValidadorXsd(string? carpetaEsquemas = null)
    {
        CarpetaEsquemas = carpetaEsquemas ?? CarpetaPorDefecto;
        var lr = Path.Combine(CarpetaEsquemas, "SuministroLR.xsd");
        if (!File.Exists(lr)) { ErrorCarga = "No existe " + lr; return; }
        try
        {
            var set = new XmlSchemaSet { XmlResolver = new ResolverLocal(CarpetaEsquemas) };
            // El esquema xmldsig del W3C lleva DOCTYPE con subconjunto interno: hay que permitir el DTD al leerlo.
            var xmldsig = Path.Combine(CarpetaEsquemas, "xmldsig-core-schema.xsd");
            if (File.Exists(xmldsig))
            {
                using var lector = XmlReader.Create(xmldsig, new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = null });
                set.Add("http://www.w3.org/2000/09/xmldsig#", lector);
            }
            set.Add(null, Path.Combine(CarpetaEsquemas, "SuministroInformacion.xsd"));
            set.Add(null, lr);
            set.Compile();
            _esquemas = set;
        }
        catch (Exception ex) { ErrorCarga = ex.Message; _esquemas = null; }
    }

    public IReadOnlyList<string> Validar(XDocument documento)
    {
        if (_esquemas is null) return ["Validación XSD no disponible: " + ErrorCarga];
        var errores = new List<string>();
        documento.Validate(_esquemas, (_, e) => errores.Add(e.Message));
        return errores;
    }

    /// <summary>Resuelve el import de xmldsig (URL del W3C) y los schemaLocation relativos contra la carpeta local.</summary>
    private sealed class ResolverLocal(string carpeta) : XmlUrlResolver
    {
        public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
        {
            if (relativeUri is not null && relativeUri.Contains("xmldsig-core-schema.xsd", StringComparison.OrdinalIgnoreCase))
                return new Uri(Path.Combine(carpeta, "xmldsig-core-schema.xsd"));
            if (relativeUri is not null && !relativeUri.Contains("://"))
                return new Uri(Path.Combine(carpeta, Path.GetFileName(relativeUri)));
            return base.ResolveUri(baseUri, relativeUri);
        }
    }
}
