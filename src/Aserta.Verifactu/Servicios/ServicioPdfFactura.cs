using Aserta.Dominio.Facturacion;

namespace Aserta.Verifactu.Servicios;

/// <summary>Regla de oro (qr-y-pdf.md §5.3): el PDF de una factura se genera UNA vez y se guarda; nunca se regenera.</summary>
public sealed class ServicioPdfFactura
{
    private readonly IRepositorioFacturacion _repo;
    private readonly IGeneradorPdf _generador;
    private readonly IAlmacenPdf _almacen;
    private readonly IRelojVerifactu _reloj;
    private readonly OpcionesVerifactu _opciones;

    public ServicioPdfFactura(IRepositorioFacturacion repo, IGeneradorPdf generador, IAlmacenPdf almacen, IRelojVerifactu reloj, OpcionesVerifactu opciones)
    {
        _repo = repo;
        _generador = generador;
        _almacen = almacen;
        _reloj = reloj;
        _opciones = opciones;
    }

    public string Html(FacturaEmitida f, EmisorFacturacion emisor, string? nombreRectificada) =>
        PlantillaFacturaHtml.Generar(f, emisor, nombreRectificada, _opciones.Entorno == Qr.EntornoAeat.Pruebas);

    /// <summary>Genera y guarda el PDF si no existe. Devuelve true si se genero ahora.</summary>
    public async Task<bool> GenerarSiFaltaAsync(Guid facturaId, CancellationToken ct = default)
    {
        var pdf = await _repo.PdfAsync(facturaId, ct);
        if (pdf is null || pdf.Generado) return false;
        var factura = await _repo.FacturaAsync(facturaId, ct) ?? throw new ExcepcionFacturacion("Factura desconocida.");
        var emisor = await _repo.EmisorAsync(factura.ClienteEmisorId, ct) ?? throw new ExcepcionFacturacion("Emisor desconocido.");
        string? rectificada = factura.FacturaRectificadaId is Guid r ? (await _repo.FacturaAsync(r, ct))?.NumSerieFactura : null;
        try
        {
            var bytes = await _generador.GenerarAsync(new ContenidoFacturaPdf(Html(factura, emisor, rectificada), factura, emisor, rectificada, factura.UrlQr, _opciones.Entorno == Qr.EntornoAeat.Pruebas), ct);
            pdf.ClaveAlmacen = await _almacen.GuardarAsync(bytes, ct);
            pdf.VersionPlantilla = PlantillaFacturaHtml.Version; pdf.Motor = _generador.Motor; pdf.FechaGeneracionUtc = _reloj.AhoraUtc; pdf.Error = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            pdf.Intentos++; pdf.Error = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            await _repo.ActualizarPdfAsync(pdf, ct);
            throw;
        }
        pdf.Intentos++;
        await _repo.ActualizarPdfAsync(pdf, ct);
        return true;
    }

    public async Task<byte[]?> LeerAsync(Guid facturaId, CancellationToken ct = default)
    {
        var pdf = await _repo.PdfAsync(facturaId, ct);
        return pdf?.ClaveAlmacen is Guid clave ? await _almacen.LeerAsync(clave, ct) : null;
    }
}
