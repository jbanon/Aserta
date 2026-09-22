using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Aserta.Verifactu.Cliente;
using Aserta.Verifactu.Servicios;
using Aserta.Verifactu.Xml;
using Microsoft.Extensions.Logging;

namespace Aserta.Infraestructura.Facturacion;

/// <summary>
/// Cliente REAL del servicio web (SOAP sobre HttpClient con certificado de cliente,
/// envio-y-cola-reintentos.md §3). NO EJERCITADO en la demo: las URL del servicio
/// van en configuracion y estan vacias a proposito ([VERIFICAR] en la sede de la
/// AEAT); sin URL, cada envio es un ERROR_TECNICO reintentable, nunca un exito falso.
/// La lectura de la respuesta sigue RespuestaSuministro.xsd (EstadoEnvio,
/// RespuestaLinea/EstadoRegistro, CSV, CodigoErrorRegistro, TiempoEsperaEnvio).
/// </summary>
public sealed class ClienteAeatSoap : IClienteAeatVerifactu
{
    private readonly IHttpClientFactory _http;
    private readonly OpcionesVerifactu _opciones;
    private readonly ILogger<ClienteAeatSoap> _log;
    private static readonly XNamespace NsResp = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/RespuestaSuministro.xsd";

    public ClienteAeatSoap(IHttpClientFactory http, OpcionesVerifactu opciones, ILogger<ClienteAeatSoap> log)
    {
        _http = http;
        _opciones = opciones;
        _log = log;
    }

    public string Nombre => "ClienteAeatSoap";

    public async Task<RespuestaAeat> RemitirAsync(LoteRegistros lote, CancellationToken ct = default)
    {
        var url = _opciones.Entorno == Aserta.Verifactu.Qr.EntornoAeat.Produccion ? _opciones.UrlServicioProduccion : _opciones.UrlServicioPruebas;
        if (string.IsNullOrWhiteSpace(url))
            throw new ExcepcionEnvioAeat("URL del servicio web de la AEAT no configurada (Verifactu:UrlServicioPruebas / UrlServicioProduccion) [VERIFICAR en la sede electrónica].");
        if (lote.HuellaCertificado is null)
            throw new ExcepcionEnvioAeat("No hay certificado con el que remitir el lote.");

        // Un HttpClient por certificado (ADR-002 §3.2): el nombre del cliente es la huella del certificado
        var cliente = _http.CreateClient("aeat-" + lote.HuellaCertificado);
        var envio = ConstructorXmlRegistro.EnvioLote(lote.NifEmisor, "", lote.Registros.Select(r => XElement.Parse(r.Xml)));
        var sobre = ConstructorXmlRegistro.SobreSoap(envio);
        using var contenido = new StringContent(sobre.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml");
        contenido.Headers.Add("SOAPAction", "");
        try
        {
            using var respuesta = await cliente.PostAsync(url, contenido, ct);
            var cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
            if (!respuesta.IsSuccessStatusCode) throw new ExcepcionEnvioAeat($"HTTP {(int)respuesta.StatusCode} de la AEAT: {cuerpo[..Math.Min(300, cuerpo.Length)]}");
            return Interpretar(lote, cuerpo);
        }
        catch (HttpRequestException ex) { throw new ExcepcionEnvioAeat("Error de red/TLS al enviar a la AEAT: " + ex.Message, ex); }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested) { throw new ExcepcionEnvioAeat("Tiempo de espera agotado al enviar a la AEAT.", ex); }
    }

    private static RespuestaAeat Interpretar(LoteRegistros lote, string xml)
    {
        var doc = XDocument.Parse(xml);
        var raiz = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "RespuestaRegFactuSistemaFacturacion") ?? throw new ExcepcionEnvioAeat("Respuesta sin RespuestaRegFactuSistemaFacturacion.");
        int espera = int.TryParse(raiz.Elements().FirstOrDefault(e => e.Name.LocalName == "TiempoEsperaEnvio")?.Value, out var t) ? t : 0;
        var lineas = raiz.Elements().Where(e => e.Name.LocalName == "RespuestaLinea").ToList();
        var resultados = new List<RespuestaRegistro>();
        for (int i = 0; i < lote.Registros.Count; i++)
        {
            var linea = i < lineas.Count ? lineas[i] : null;
            string estado = linea?.Elements().FirstOrDefault(e => e.Name.LocalName == "EstadoRegistro")?.Value ?? "Incorrecto";
            var r = estado switch { "Correcto" => ResultadoRegistro.Aceptado, "AceptadoConErrores" => ResultadoRegistro.AceptadoConErrores, _ => ResultadoRegistro.Rechazado };
            resultados.Add(new RespuestaRegistro(lote.Registros[i].RegistroId, r,
                raiz.Elements().FirstOrDefault(e => e.Name.LocalName == "CSV")?.Value,
                linea?.Elements().FirstOrDefault(e => e.Name.LocalName == "CodigoErrorRegistro")?.Value,
                linea?.Elements().FirstOrDefault(e => e.Name.LocalName == "DescripcionErrorRegistro")?.Value));
        }
        return new RespuestaAeat(resultados, espera, xml);
    }
}
