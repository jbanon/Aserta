using System.Text;

namespace Aserta.Verifactu.Cliente;

public enum ModoSimulador { Normal, Caido, LatenciaAlta, Rechazo, AceptadoConErrores, EsperaAgresiva }

/// <summary>
/// Estado mutable del simulador, cambiable desde una pantalla de la propia
/// aplicacion durante la demo (envio-y-cola-reintentos.md §6). Singleton.
/// </summary>
public sealed class EstadoSimuladorAeat
{
    private readonly object _cerrojo = new();
    private ModoSimulador _modo = ModoSimulador.Normal;
    public int LatenciaMs { get; private set; } = 300;
    public int TiempoEsperaSegundos { get; private set; } = 0;
    public string CodigoRechazo { get; private set; } = "3002";
    public string DescripcionRechazo { get; private set; } = "Simulado: no existe el registro de facturación (rechazo provocado desde el panel del simulador)";
    public int LotesRecibidos { get; private set; }
    public int RegistrosRecibidos { get; private set; }
    public DateTime? UltimoLoteUtc { get; private set; }

    public ModoSimulador Modo { get { lock (_cerrojo) return _modo; } }

    public void Configurar(ModoSimulador modo, int? latenciaMs = null, int? tiempoEsperaSegundos = null, string? codigoRechazo = null, string? descripcionRechazo = null)
    {
        lock (_cerrojo)
        {
            _modo = modo;
            if (latenciaMs is int l) LatenciaMs = Math.Clamp(l, 0, 60_000);
            if (tiempoEsperaSegundos is int t) TiempoEsperaSegundos = Math.Clamp(t, 0, 3600);
            if (!string.IsNullOrWhiteSpace(codigoRechazo)) CodigoRechazo = codigoRechazo.Trim();
            if (!string.IsNullOrWhiteSpace(descripcionRechazo)) DescripcionRechazo = descripcionRechazo.Trim();
        }
    }

    internal void Contabilizar(int registros)
    {
        lock (_cerrojo) { LotesRecibidos++; RegistrosRecibidos += registros; UltimoLoteUtc = DateTime.UtcNow; }
    }
}

/// <summary>
/// Adaptador de demostracion con el MISMO contrato que el cliente real. Permite
/// provocar caidas, latencia, rechazos, aceptados con errores y esperas agresivas.
/// Nunca se activa en produccion (la aplicacion no arranca si se detecta).
/// </summary>
public sealed class SimuladorAeat : IClienteAeatVerifactu
{
    private readonly EstadoSimuladorAeat _estado;
    private readonly TimeProvider _reloj;

    public SimuladorAeat(EstadoSimuladorAeat estado, TimeProvider? reloj = null)
    {
        _estado = estado;
        _reloj = reloj ?? TimeProvider.System;
    }

    public string Nombre => "SimuladorAeat";

    public async Task<RespuestaAeat> RemitirAsync(LoteRegistros lote, CancellationToken ct = default)
    {
        var modo = _estado.Modo;
        if (modo == ModoSimulador.Caido)
            throw new ExcepcionEnvioAeat("Simulador: la AEAT no responde (caída provocada). Conexión rehusada.");

        int latencia = modo == ModoSimulador.LatenciaAlta ? Math.Max(_estado.LatenciaMs, 4000) : _estado.LatenciaMs;
        if (latencia > 0) await Task.Delay(TimeSpan.FromMilliseconds(latencia), _reloj, ct);
        _estado.Contabilizar(lote.Registros.Count);

        var ahora = _reloj.GetUtcNow();
        var registros = lote.Registros.Select((r, i) =>
        {
            string csv = $"SIM{ahora:yyyyMMddHHmmss}{r.RegistroId:D8}";
            return modo switch
            {
                ModoSimulador.Rechazo => new RespuestaRegistro(r.RegistroId, ResultadoRegistro.Rechazado, null, _estado.CodigoRechazo, _estado.DescripcionRechazo),
                ModoSimulador.AceptadoConErrores => new RespuestaRegistro(r.RegistroId, ResultadoRegistro.AceptadoConErrores, csv, "2001", "Simulado: la huella recibida no coincide con la calculada por la AEAT"),
                _ => new RespuestaRegistro(r.RegistroId, ResultadoRegistro.Aceptado, csv, null, null),
            };
        }).ToList();

        int espera = modo == ModoSimulador.EsperaAgresiva ? Math.Max(_estado.TiempoEsperaSegundos, 60) : _estado.TiempoEsperaSegundos;
        return new RespuestaAeat(registros, espera, XmlRespuesta(lote, registros, espera, ahora));
    }

    private static string XmlRespuesta(LoteRegistros lote, List<RespuestaRegistro> registros, int espera, DateTimeOffset ahora)
    {
        // Respuesta "de aspecto" AEAT para la bandeja de envios; los nombres reales del RespuestaSuministro.xsd son [VERIFICAR] (envio-y-cola-reintentos.md §3).
        var sb = new StringBuilder();
        sb.Append("<RespuestaSimulada origen=\"SimuladorAeat\" fecha=\"").Append(ahora.ToString("O")).Append("\" nifEmisor=\"").Append(lote.NifEmisor).Append("\" tiempoEsperaEnvio=\"").Append(espera).Append("\">");
        foreach (var r in registros)
            sb.Append("<Registro id=\"").Append(r.RegistroId).Append("\" estado=\"").Append(r.Resultado).Append("\" csv=\"").Append(r.Csv).Append("\" codigoError=\"").Append(r.CodigoError).Append("\"/>");
        sb.Append("</RespuestaSimulada>");
        return sb.ToString();
    }
}
