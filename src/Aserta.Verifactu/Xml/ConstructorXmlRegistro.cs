using System.Xml.Linq;
using Aserta.Verifactu.Comun;

namespace Aserta.Verifactu.Xml;

/// <summary>Bloque SistemaInformatico (SuministroInformacion.xsd: SistemaInformaticoType). Coherente con la declaracion responsable.</summary>
public sealed record SistemaInformatico(string NombreRazon, string Nif, string NombreSistema, string IdSistemaInformatico, string Version, string NumeroInstalacion)
{
    public void Validar()
    {
        if (NombreSistema.Length > 30) throw new ArgumentException("NombreSistemaInformatico admite 30 caracteres como máximo (XSD TextMax30Type).");
        if (IdSistemaInformatico.Length is 0 or > 2) throw new ArgumentException("IdSistemaInformatico debe tener 1 o 2 caracteres (XSD TextMax2Type).");
        if (Version.Length > 50) throw new ArgumentException("Version admite 50 caracteres como máximo.");
        if (NumeroInstalacion.Length is 0 or > 100) throw new ArgumentException("NumeroInstalacion debe tener entre 1 y 100 caracteres.");
    }
}

/// <summary>Una linea de DetalleDesglose (DetalleType). Si Exenta, se usa OperacionExenta en lugar de CalificacionOperacion.</summary>
public sealed record LineaDesglose(string ClaveRegimen, string CalificacionOperacion, bool Exenta, string? CodigoExencion, decimal TipoImpositivo, decimal BaseImponible, decimal Cuota, decimal? TipoRecargo, decimal? CuotaRecargo);

public sealed record IdFactura(string IdEmisor, string NumSerie, DateOnly Fecha);

public sealed record DatosRegistroAlta(
    IdFactura Factura, string NombreRazonEmisor, string TipoFactura, string? TipoRectificativa, IdFactura? FacturaRectificada,
    string DescripcionOperacion, string? DestinatarioNif, string? DestinatarioNombre,
    IReadOnlyList<LineaDesglose> Desglose, decimal CuotaTotal, decimal ImporteTotal,
    bool PrimerRegistro, IdFactura? RegistroAnterior, string? HuellaAnterior,
    string FechaHoraHusoGenRegistro, string Huella, SistemaInformatico Sistema);

public sealed record DatosRegistroAnulacion(
    IdFactura FacturaAnulada, bool PrimerRegistro, IdFactura? RegistroAnterior, string? HuellaAnterior,
    string FechaHoraHusoGenRegistro, string Huella, SistemaInformatico Sistema);

/// <summary>
/// Construye el XML de los registros con los nombres, el orden y los valores de
/// SuministroLR.xsd y SuministroInformacion.xsd (versionados en Esquemas/,
/// descargados del portal de la AEAT el 2026-09-22). El documento generado se
/// valida contra esos esquemas (ValidadorXsd) antes de enviarse y se guarda tal
/// cual en vf.RegistroFacturacion.XmlRegistro.
/// </summary>
public static class ConstructorXmlRegistro
{
    // targetNamespace literal de los XSD oficiales (nota: la ruta del namespace es tike/cont/ws, sin "V1.0")
    public static readonly XNamespace NsLR = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroLR.xsd";
    public static readonly XNamespace NsSF = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd";
    public const string IdVersion = "1.0";        // VersionType
    public const string TipoHuellaSha256 = "01";  // TipoHuellaType: unico valor admitido
    public const string ImpuestoIva = "01";       // ImpuestoType: 01 = IVA
    public const string ClaveRegimenGeneral = "01";     // IdOperacionesTrascendenciaTributariaType [VERIFICAR semantica exacta de cada clave]
    public const string CalificacionSujetaNoExenta = "S1"; // CalificacionOperacionType [VERIFICAR]
    public const string ExencionArt20 = "E1";     // OperacionExentaType [VERIFICAR]

    public static XElement RegistroAlta(DatosRegistroAlta d)
    {
        d.Sistema.Validar();
        var sf = NsSF;
        var e = new XElement(sf + "RegistroAlta",
            new XElement(sf + "IDVersion", IdVersion),
            IdFacturaElemento(sf, "IDFactura", d.Factura),
            new XElement(sf + "NombreRazonEmisor", Recortar(d.NombreRazonEmisor, 120)),
            new XElement(sf + "TipoFactura", d.TipoFactura));

        if (d.TipoRectificativa is not null) e.Add(new XElement(sf + "TipoRectificativa", d.TipoRectificativa));   // S | I
        if (d.FacturaRectificada is { } fr)
            e.Add(new XElement(sf + "FacturasRectificadas", IdFacturaElemento(sf, "IDFacturaRectificada", fr)));

        e.Add(new XElement(sf + "DescripcionOperacion", Recortar(d.DescripcionOperacion, 500)));
        if (d.DestinatarioNif is not null)
            e.Add(new XElement(sf + "Destinatarios", new XElement(sf + "IDDestinatario",
                new XElement(sf + "NombreRazon", Recortar(d.DestinatarioNombre ?? "", 120)), new XElement(sf + "NIF", d.DestinatarioNif))));

        var desglose = new XElement(sf + "Desglose");
        foreach (var l in d.Desglose.Take(12))
        {
            var det = new XElement(sf + "DetalleDesglose",
                new XElement(sf + "Impuesto", ImpuestoIva),
                new XElement(sf + "ClaveRegimen", l.ClaveRegimen));
            if (l.Exenta) det.Add(new XElement(sf + "OperacionExenta", l.CodigoExencion ?? ExencionArt20));
            else
            {
                det.Add(new XElement(sf + "CalificacionOperacion", l.CalificacionOperacion));
                det.Add(new XElement(sf + "TipoImpositivo", FormatosVerifactu.Importe(l.TipoImpositivo)));
            }
            det.Add(new XElement(sf + "BaseImponibleOimporteNoSujeto", FormatosVerifactu.Importe(l.BaseImponible)));
            if (!l.Exenta) det.Add(new XElement(sf + "CuotaRepercutida", FormatosVerifactu.Importe(l.Cuota)));
            if (!l.Exenta && l.TipoRecargo is decimal tr)
            {
                det.Add(new XElement(sf + "TipoRecargoEquivalencia", FormatosVerifactu.Importe(tr)));
                det.Add(new XElement(sf + "CuotaRecargoEquivalencia", FormatosVerifactu.Importe(l.CuotaRecargo ?? 0m)));
            }
            desglose.Add(det);
        }
        e.Add(desglose);
        e.Add(new XElement(sf + "CuotaTotal", FormatosVerifactu.Importe(d.CuotaTotal)));
        e.Add(new XElement(sf + "ImporteTotal", FormatosVerifactu.Importe(d.ImporteTotal)));
        e.Add(Encadenamiento(sf, d.PrimerRegistro, d.RegistroAnterior, d.HuellaAnterior));
        e.Add(Sistema(sf, d.Sistema));
        e.Add(new XElement(sf + "FechaHoraHusoGenRegistro", d.FechaHoraHusoGenRegistro));
        e.Add(new XElement(sf + "TipoHuella", TipoHuellaSha256));
        e.Add(new XElement(sf + "Huella", d.Huella));
        return e;
    }

    public static XElement RegistroAnulacion(DatosRegistroAnulacion d)
    {
        d.Sistema.Validar();
        var sf = NsSF;
        var e = new XElement(sf + "RegistroAnulacion",
            new XElement(sf + "IDVersion", IdVersion),
            new XElement(sf + "IDFactura",
                new XElement(sf + "IDEmisorFacturaAnulada", d.FacturaAnulada.IdEmisor),
                new XElement(sf + "NumSerieFacturaAnulada", d.FacturaAnulada.NumSerie),
                new XElement(sf + "FechaExpedicionFacturaAnulada", FormatosVerifactu.Fecha(d.FacturaAnulada.Fecha))));
        e.Add(Encadenamiento(sf, d.PrimerRegistro, d.RegistroAnterior, d.HuellaAnterior));
        e.Add(Sistema(sf, d.Sistema));
        e.Add(new XElement(sf + "FechaHoraHusoGenRegistro", d.FechaHoraHusoGenRegistro));
        e.Add(new XElement(sf + "TipoHuella", TipoHuellaSha256));
        e.Add(new XElement(sf + "Huella", d.Huella));
        return e;
    }

    /// <summary>Envoltorio RegFactuSistemaFacturacion (SuministroLR.xsd): cabecera con el obligado y hasta 1000 RegistroFactura.</summary>
    public static XDocument EnvioLote(string nifObligado, string nombreObligado, IEnumerable<XElement> registros)
    {
        var raiz = new XElement(NsLR + "RegFactuSistemaFacturacion",
            new XAttribute(XNamespace.Xmlns + "sfLR", NsLR), new XAttribute(XNamespace.Xmlns + "sf", NsSF),
            new XElement(NsLR + "Cabecera", new XElement(NsSF + "ObligadoEmision", new XElement(NsSF + "NombreRazon", Recortar(nombreObligado, 120)), new XElement(NsSF + "NIF", nifObligado))));
        foreach (var r in registros) raiz.Add(new XElement(NsLR + "RegistroFactura", r));
        return new XDocument(new XDeclaration("1.0", "UTF-8", null), raiz);
    }

    /// <summary>Sobre SOAP 1.1 con el lote como cuerpo (envio-y-cola-reintentos.md §3: sobre construido explicitamente).</summary>
    public static XDocument SobreSoap(XDocument lote)
    {
        XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        return new XDocument(new XDeclaration("1.0", "UTF-8", null),
            new XElement(soap + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                new XElement(soap + "Header"), new XElement(soap + "Body", lote.Root)));
    }

    private static XElement IdFacturaElemento(XNamespace sf, string nombre, IdFactura f) => new(sf + nombre,
        new XElement(sf + "IDEmisorFactura", f.IdEmisor), new XElement(sf + "NumSerieFactura", f.NumSerie), new XElement(sf + "FechaExpedicionFactura", FormatosVerifactu.Fecha(f.Fecha)));

    private static XElement Encadenamiento(XNamespace sf, bool primero, IdFactura? anterior, string? huellaAnterior)
    {
        var e = new XElement(sf + "Encadenamiento");
        if (primero || anterior is null) e.Add(new XElement(sf + "PrimerRegistro", "S"));
        else e.Add(new XElement(sf + "RegistroAnterior",
            new XElement(sf + "IDEmisorFactura", anterior.IdEmisor), new XElement(sf + "NumSerieFactura", anterior.NumSerie),
            new XElement(sf + "FechaExpedicionFactura", FormatosVerifactu.Fecha(anterior.Fecha)), new XElement(sf + "Huella", huellaAnterior ?? "")));
        return e;
    }

    private static XElement Sistema(XNamespace sf, SistemaInformatico s) => new(sf + "SistemaInformatico",
        new XElement(sf + "NombreRazon", Recortar(s.NombreRazon, 120)), new XElement(sf + "NIF", s.Nif), new XElement(sf + "NombreSistemaInformatico", s.NombreSistema),
        new XElement(sf + "IdSistemaInformatico", s.IdSistemaInformatico), new XElement(sf + "Version", s.Version), new XElement(sf + "NumeroInstalacion", s.NumeroInstalacion),
        new XElement(sf + "TipoUsoPosibleSoloVerifactu", "S"),   // DA-11: solo modo VERI*FACTU
        new XElement(sf + "TipoUsoPosibleMultiOT", "S"),         // multi-obligado: una plataforma para muchos emisores
        new XElement(sf + "IndicadorMultiplesOT", "S"));

    private static string Recortar(string s, int max) => s.Length <= max ? s : s[..max];
}
