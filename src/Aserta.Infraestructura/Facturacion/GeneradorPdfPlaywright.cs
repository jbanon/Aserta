using System.Diagnostics;
using Aserta.Verifactu.Servicios;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Aserta.Infraestructura.Facturacion;

/// <summary>
/// qr-y-pdf.md §5.2: UNA instancia de Chromium reutilizada, un trabajo a la vez
/// (SemaphoreSlim(1)), contexto de pagina nuevo por trabajo, timeout por trabajo,
/// reinicio del navegador si falla y cada N trabajos para cortar fugas. Singleton.
/// </summary>
public sealed class GeneradorPdfPlaywright : IGeneradorPdf, IAsyncDisposable
{
    private readonly ILogger<GeneradorPdfPlaywright> _log;
    private readonly SemaphoreSlim _uno = new(1, 1);
    private readonly int _reiniciarCada;
    private readonly TimeSpan _timeout;
    private IPlaywright? _playwright;
    private IBrowser? _navegador;
    private int _trabajos;

    public string Motor => "Playwright";
    public bool Disponible { get; private set; }
    public string? UltimoError { get; private set; }

    public GeneradorPdfPlaywright(ILogger<GeneradorPdfPlaywright> log, int reiniciarCada = 200, int timeoutSegundos = 20)
    {
        _log = log;
        _reiniciarCada = reiniciarCada;
        _timeout = TimeSpan.FromSeconds(timeoutSegundos);
    }

    /// <summary>Comprobacion al arrancar: el ejecutable existe y responde (qr-y-pdf.md §5.2). Devuelve false con el motivo si no.</summary>
    public async Task<bool> ComprobarAsync(CancellationToken ct = default)
    {
        try
        {
            await _uno.WaitAsync(ct);
            try { await AsegurarNavegadorAsync(); Disponible = true; return true; }
            finally { _uno.Release(); }
        }
        catch (Exception ex) { UltimoError = ex.Message; Disponible = false; _log.LogWarning("Chromium no disponible para PDF: {Error}", ex.Message); return false; }
    }

    public async Task<byte[]> GenerarAsync(ContenidoFacturaPdf contenido, CancellationToken ct = default)
    {
        await _uno.WaitAsync(ct);
        try
        {
            for (int intento = 1; ; intento++)
            {
                try { return await GenerarInternoAsync(contenido.Html, ct); }
                catch (Exception ex) when (intento == 1 && ex is not OperationCanceledException)
                {
                    _log.LogWarning(ex, "Fallo al generar PDF; se reinicia Chromium y se reintenta una vez");
                    await CerrarAsync();
                }
            }
        }
        finally { _uno.Release(); }
    }

    private async Task<byte[]> GenerarInternoAsync(string html, CancellationToken ct)
    {
        if (_trabajos >= _reiniciarCada) { await CerrarAsync(); _trabajos = 0; }
        var navegador = await AsegurarNavegadorAsync();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeout);
        var sw = Stopwatch.StartNew();
        await using var contexto = await navegador.NewContextAsync();   // contexto nuevo por trabajo: no se comparte estado entre facturas
        var pagina = await contexto.NewPageAsync();
        await pagina.SetContentAsync(html, new() { WaitUntil = WaitUntilState.Load, Timeout = (float)_timeout.TotalMilliseconds });
        var pdf = await pagina.PdfAsync(new() { Format = "A4", PrintBackground = true, PreferCSSPageSize = true });
        _trabajos++;
        _log.LogInformation("PDF generado con Chromium en {Ms} ms ({Bytes} bytes)", sw.ElapsedMilliseconds, pdf.Length);
        return pdf;
    }

    private async Task<IBrowser> AsegurarNavegadorAsync()
    {
        if (_navegador is { IsConnected: true }) return _navegador;
        await CerrarAsync();
        _playwright ??= await Playwright.CreateAsync();
        _navegador = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = ["--no-sandbox", "--disable-dev-shm-usage", "--disable-gpu", "--no-zygote", "--renderer-process-limit=1", "--disable-extensions"],
        });
        _log.LogInformation("Chromium iniciado para PDF ({Version})", _navegador.Version);
        return _navegador;
    }

    private async Task CerrarAsync()
    {
        try { if (_navegador is not null) await _navegador.CloseAsync(); } catch { }
        _navegador = null;
    }

    public async ValueTask DisposeAsync()
    {
        await CerrarAsync();
        _playwright?.Dispose();
    }
}
