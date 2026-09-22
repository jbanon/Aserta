namespace Aserta.Verifactu.Cliente;

/// <summary>Un registro dentro de un lote de envio.</summary>
public sealed record RegistroParaEnvio(long RegistroId, string NifEmisor, string Tipo, string NumSerieFactura, string Huella, string Xml);

/// <summary>Lote de un unico emisor (los envios nunca mezclan emisores).</summary>
public sealed record LoteRegistros(string NifEmisor, IReadOnlyList<RegistroParaEnvio> Registros, string? HuellaCertificado);

public enum ResultadoRegistro { Aceptado, AceptadoConErrores, Rechazado }

public sealed record RespuestaRegistro(long RegistroId, ResultadoRegistro Resultado, string? Csv, string? CodigoError, string? DescripcionError);

/// <summary>Respuesta de la AEAT (o del simulador) a un lote. TiempoEsperaSegundos manda sobre la configuracion.</summary>
public sealed record RespuestaAeat(IReadOnlyList<RespuestaRegistro> Registros, int TiempoEsperaSegundos, string XmlRespuesta);

/// <summary>Fallo tecnico (red, TLS, 5xx, timeout): reintentable con retroceso exponencial.</summary>
public sealed class ExcepcionEnvioAeat : Exception
{
    public ExcepcionEnvioAeat(string mensaje, Exception? interna = null) : base(mensaje, interna) { }
}

/// <summary>Puerto hacia el servicio web de la AEAT. Implementaciones: SimuladorAeat (demo) y ClienteAeatSoap (real, SOAP + mTLS).</summary>
public interface IClienteAeatVerifactu
{
    string Nombre { get; }
    Task<RespuestaAeat> RemitirAsync(LoteRegistros lote, CancellationToken ct = default);
}
