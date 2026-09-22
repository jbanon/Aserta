using Aserta.Verifactu.Qr;
using Aserta.Verifactu.Xml;

namespace Aserta.Verifactu.Servicios;

/// <summary>Seccion "Verifactu" de la configuracion. Nada normativo en codigo: URLs, entorno, identificacion del sistema y declaracion responsable.</summary>
public sealed class OpcionesVerifactu
{
    public const string Seccion = "Verifactu";

    /// <summary>Una sola bandera de entorno para QR y servicio web (Q4).</summary>
    public EntornoAeat Entorno { get; set; } = EntornoAeat.Pruebas;
    /// <summary>true = SimuladorAeat (DA-05). Vetado en Production: la aplicacion no arranca.</summary>
    public bool UsarSimulador { get; set; } = true;
    public string UrlQrPruebas { get; set; } = "https://prewww2.aeat.es/wlpl/TIKE-CONT/ValidarQR";
    public string UrlQrProduccion { get; set; } = "https://www2.agenciatributaria.gob.es/wlpl/TIKE-CONT/ValidarQR";
    /// <summary>URLs del servicio web SOAP: [VERIFICAR] en la sede de la AEAT. Vacias a proposito: nunca inventadas.</summary>
    public string? UrlServicioPruebas { get; set; }
    public string? UrlServicioProduccion { get; set; }
    public int TamanoLote { get; set; } = 100;           // max. 1000 segun SuministroLR.xsd
    public int EsperaPorDefectoSegundos { get; set; } = 0;
    public int SegundosEntreCiclosWorker { get; set; } = 5;
    /// <summary>false desactiva el worker de envio (tests de integracion: los ciclos se lanzan a mano).</summary>
    public bool WorkerActivo { get; set; } = true;
    public bool ValidarXsd { get; set; } = true;
    public string VersionPlantillaPdf { get; set; } = "1";

    public SistemaInformaticoOpciones Sistema { get; set; } = new();
    public DeclaracionResponsableOpciones DeclaracionResponsable { get; set; } = new();

    public OpcionesQr Qr => new() { Entorno = Entorno, UrlPruebas = UrlQrPruebas, UrlProduccion = UrlQrProduccion };
    public SistemaInformatico SistemaInformatico => new(Sistema.NombreRazonProductor, Sistema.NifProductor, Sistema.NombreSistema, Sistema.IdSistemaInformatico, Sistema.Version, Sistema.NumeroInstalacion);
}

/// <summary>Bloque SistemaInformatico que viaja en cada registro. Debe coincidir con la declaracion responsable.</summary>
public sealed class SistemaInformaticoOpciones
{
    public string NombreRazonProductor { get; set; } = "";
    public string NifProductor { get; set; } = "";
    public string NombreSistema { get; set; } = "Aserta";
    public string IdSistemaInformatico { get; set; } = "AS";
    public string Version { get; set; } = "";
    public string NumeroInstalacion { get; set; } = "1";
}

public sealed class DeclaracionResponsableOpciones
{
    public bool DatosDeDemostracion { get; set; } = true;
    public string RazonSocial { get; set; } = "";
    public string Nif { get; set; } = "";
    public string Domicilio { get; set; } = "";
    public string NombreSistema { get; set; } = "Aserta";
    public string IdSistemaInformatico { get; set; } = "AS";
    public string Version { get; set; } = "";
    public string Componentes { get; set; } = "";
    public string FechaSuscripcion { get; set; } = "";
    public string LugarSuscripcion { get; set; } = "";
    public string Firmante { get; set; } = "";
}
