using Aserta.Aplicacion.Puertos;
using Aserta.Verifactu.Servicios;

namespace Aserta.Infraestructura.Facturacion;

/// <summary>Los PDF viven en el mismo almacen cifrado que los documentos.</summary>
public sealed class AlmacenPdfAdaptador : IAlmacenPdf
{
    private readonly IAlmacenDocumental _almacen;
    public AlmacenPdfAdaptador(IAlmacenDocumental almacen) => _almacen = almacen;

    public Task<Guid> GuardarAsync(byte[] pdf, CancellationToken ct = default) => _almacen.GuardarAsync(new MemoryStream(pdf), ct);

    public async Task<byte[]> LeerAsync(Guid clave, CancellationToken ct = default)
    {
        await using var s = await _almacen.AbrirAsync(clave, ct);
        using var ms = new MemoryStream();
        await s.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}

public sealed class RelojVerifactu : IRelojVerifactu
{
    private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
    private readonly IRelojSistema _reloj;
    public RelojVerifactu(IRelojSistema reloj) => _reloj = reloj;
    public DateTime AhoraUtc => _reloj.AhoraUtc;
    public DateOnly Hoy => _reloj.Hoy;
    /// <summary>Instante con el desfase real de Madrid (+01:00 / +02:00), sin segundos fraccionarios: es lo que entra en la huella.</summary>
    public DateTimeOffset AhoraEnMadrid
    {
        get
        {
            var utc = new DateTime(_reloj.AhoraUtc.Ticks - _reloj.AhoraUtc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTime(new DateTimeOffset(utc), Madrid);
        }
    }
}
