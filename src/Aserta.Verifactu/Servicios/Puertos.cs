using Aserta.Dominio.Facturacion;

namespace Aserta.Verifactu.Servicios;

/// <summary>Datos del emisor (el cliente de la gestoria) que necesita la emision.</summary>
public sealed record EmisorFacturacion(Guid ClienteId, Guid GestoriaId, string Nif, string Nombre, string? Direccion, bool Foral, string FormaJuridica);

/// <summary>Un lote de trabajo pendiente: emisor y numero de filas listas.</summary>
public sealed record EmisorConPendientes(Guid GestoriaId, Guid ClienteEmisorId, string NifEmisor, int Pendientes);

/// <summary>Registro tomado de la cola junto con su fila de cola.</summary>
public sealed record TrabajoEnvio(EnvioPendiente Cola, RegistroFacturacion Registro, EstadoEnvioRegistro Estado);

/// <summary>
/// Unidad de trabajo de una emision: la fila CadenaEmisor llega BLOQUEADA
/// (UPDLOCK, HOLDLOCK) y todo lo que se agrega se confirma en la misma
/// transaccion (patron outbox). Si no se confirma, se deshace al liberar.
/// </summary>
public interface ITransaccionEmision : IAsyncDisposable
{
    CadenaEmisor Cadena { get; }
    SerieFacturacion Serie { get; }
    void Agregar(FacturaEmitida factura);
    void Agregar(RegistroFacturacion registro);
    void Agregar(EstadoEnvioRegistro estado);
    void Agregar(EnvioPendiente pendiente);
    void Agregar(FacturaPdf pdf);
    /// <summary>Guarda y confirma. Devuelve el Id del registro de facturacion insertado.</summary>
    Task<long> ConfirmarAsync(CancellationToken ct = default);
}

/// <summary>Persistencia del modulo (implementada en Infraestructura sobre EF Core). Aserta.Verifactu no conoce EF.</summary>
public interface IRepositorioFacturacion
{
    Task<EmisorFacturacion?> EmisorAsync(Guid clienteEmisorId, CancellationToken ct = default);
    Task<SerieFacturacion> SerieAsync(Guid clienteEmisorId, string codigo, int ejercicio, CancellationToken ct = default);
    Task<ITransaccionEmision> IniciarEmisionAsync(EmisorFacturacion emisor, Guid serieId, CancellationToken ct = default);

    Task<FacturaEmitida?> FacturaAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<RegistroFacturacion>> RegistrosDeFacturaAsync(Guid facturaId, CancellationToken ct = default);
    Task<EstadoEnvioRegistro?> EstadoEnvioAsync(long registroId, CancellationToken ct = default);
    Task<bool> TieneAnulacionAsync(Guid facturaId, CancellationToken ct = default);
    Task<FacturaPdf?> PdfAsync(Guid facturaId, CancellationToken ct = default);
    Task ActualizarPdfAsync(FacturaPdf pdf, CancellationToken ct = default);

    // --- Cola de envio (el worker corre sin usuario) ---
    /// <summary>Emisores con filas PENDIENTE listas (ProximoIntentoUtc vencido). Requiere ambito de mantenimiento.</summary>
    Task<IReadOnlyList<EmisorConPendientes>> EmisoresConPendientesAsync(DateTime ahoraUtc, CancellationToken ct = default);
    /// <summary>Toma hasta N filas de un emisor con READPAST, UPDLOCK, ROWLOCK y las marca EN_CURSO.</summary>
    Task<IReadOnlyList<TrabajoEnvio>> TomarPendientesAsync(string nifEmisor, int maximo, DateTime ahoraUtc, CancellationToken ct = default);
    Task<CadenaEmisor?> CadenaAsync(string nifEmisor, CancellationToken ct = default);
    Task<long> RegistrarLoteAsync(LoteEnvio lote, CancellationToken ct = default);
    /// <summary>Filas EN_CURSO tomadas hace mas de X minutos vuelven a PENDIENTE (E4: reinicio en medio de un lote).</summary>
    Task<int> LiberarTomadasCaducadasAsync(TimeSpan caducidad, DateTime ahoraUtc, CancellationToken ct = default);
    /// <summary>"Procesar ahora": adelanta a ahora el proximo intento de todas las filas PENDIENTE del tenant (y el planificador por emisor).</summary>
    Task<int> AdelantarPendientesAsync(DateTime ahoraUtc, CancellationToken ct = default);
    Task<EnvioPendiente?> EnvioPendienteDeRegistroAsync(long registroId, CancellationToken ct = default);
    Task CrearEnvioPendienteAsync(long registroId, CancellationToken ct = default);
    Task<RegistroFacturacion?> RegistroPorNumeroAsync(string nifEmisor, long numeroEnCadena, CancellationToken ct = default);
    Task GuardarAsync(CancellationToken ct = default);
}

/// <summary>Contenido para el PDF: HTML (Playwright) y datos estructurados (motor basico sin navegador).</summary>
public sealed record ContenidoFacturaPdf(string Html, FacturaEmitida Factura, EmisorFacturacion Emisor, string? NumRectificada, string UrlQr, bool EntornoPruebas);

/// <summary>Genera el PDF de una factura. Implementaciones: Playwright (Chromium, instancia unica y cola de un consumidor) y basico (sin navegador, respaldo).</summary>
public interface IGeneradorPdf
{
    string Motor { get; }
    Task<byte[]> GenerarAsync(ContenidoFacturaPdf contenido, CancellationToken ct = default);
}

/// <summary>Almacen de los PDF generados (mismo almacen cifrado que los documentos).</summary>
public interface IAlmacenPdf
{
    Task<Guid> GuardarAsync(byte[] pdf, CancellationToken ct = default);
    Task<byte[]> LeerAsync(Guid clave, CancellationToken ct = default);
}

/// <summary>Cola de generacion de PDF con un unico consumidor (ColaPdfWorker). Capacidad maxima: al llenarse, Encolar devuelve false.</summary>
public interface IColaPdf
{
    bool Encolar(Guid facturaId);
    int Pendientes { get; }
}

public sealed record CertificadoResuelto(Guid CertificadoId, TipoCertificado Tipo, string HuellaDigital, string NifTitular);

/// <summary>ADR-002 §3.1: el certificado se resuelve por obligado (propio → gestoria con apoderamiento → ninguno). Nunca cae al del productor.</summary>
public interface IProveedorCertificado
{
    Task<CertificadoResuelto?> ObtenerAsync(Guid gestoriaId, string nifObligado, Guid clienteObligadoId, CancellationToken ct = default);
}

/// <summary>Reloj del modulo (Europe/Madrid para la fecha de expedicion y el huso del registro).</summary>
public interface IRelojVerifactu
{
    DateTimeOffset AhoraEnMadrid { get; }
    DateTime AhoraUtc { get; }
    DateOnly Hoy { get; }
}

/// <summary>Excepcion de negocio del modulo (mensaje apto para el usuario).</summary>
public sealed class ExcepcionFacturacion : Exception
{
    public ExcepcionFacturacion(string mensaje) : base(mensaje) { }
}
