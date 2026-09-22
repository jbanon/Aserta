using Aserta.Dominio.Facturacion;
using Aserta.Verifactu.Cliente;
using Aserta.Verifactu.Xml;

namespace Aserta.Verifactu.Servicios;

/// <summary>
/// Consumidor de la cola por emisor (envio-y-cola-reintentos.md §2-5): toma un lote,
/// construye el XML de envio, lo valida y lo remite; interpreta la respuesta y aplica
/// la politica de reintentos con retroceso exponencial y jitter. Respeta el tiempo
/// de espera devuelto por la AEAT por emisor, sin bloquear a los demas.
/// </summary>
public sealed class ServicioEnvio
{
    private readonly IRepositorioFacturacion _repo;
    private readonly IClienteAeatVerifactu _cliente;
    private readonly IProveedorCertificado _certificados;
    private readonly IRelojVerifactu _reloj;
    private readonly OpcionesVerifactu _opciones;
    private readonly ValidadorXsd _xsd;

    public ServicioEnvio(IRepositorioFacturacion repo, IClienteAeatVerifactu cliente, IProveedorCertificado certificados, IRelojVerifactu reloj, OpcionesVerifactu opciones, ValidadorXsd xsd)
    {
        _repo = repo;
        _cliente = cliente;
        _certificados = certificados;
        _reloj = reloj;
        _opciones = opciones;
        _xsd = xsd;
    }

    public sealed record ResultadoLote(string NifEmisor, int Enviados, int Aceptados, int AceptadosConErrores, int Rechazados, int ErroresTecnicos, string? Error);

    /// <summary>Procesa un emisor: hasta TamanoLote registros pendientes. Devuelve null si no habia nada o el emisor esta en espera.</summary>
    public async Task<ResultadoLote?> ProcesarEmisorAsync(EmisorConPendientes emisor, CancellationToken ct = default)
    {
        var ahora = _reloj.AhoraUtc;
        var cadena = await _repo.CadenaAsync(emisor.NifEmisor, ct);
        if (cadena?.ProximoEnvioPermitidoUtc is DateTime espera && espera > ahora) return null;   // control de flujo por emisor

        var trabajos = await _repo.TomarPendientesAsync(emisor.NifEmisor, Math.Clamp(_opciones.TamanoLote, 1, 1000), ahora, ct);
        if (trabajos.Count == 0) return null;

        var cert = await _certificados.ObtenerAsync(emisor.GestoriaId, emisor.NifEmisor, emisor.ClienteEmisorId, ct);
        if (cert is null)
        {
            foreach (var t in trabajos)
            {
                t.Estado.Estado = EstadoEnvio.BLOQUEADO_SIN_CERTIFICADO;
                t.Estado.DescripcionErrorAeat = "Sin certificado ni apoderamiento vigente en el momento del envío.";
                t.Cola.Estado = EstadoEnvioPendiente.PENDIENTE; t.Cola.TomadoUtc = null;
                t.Cola.ProximoIntentoUtc = ahora.AddMinutes(30);
            }
            await _repo.GuardarAsync(ct);
            return new ResultadoLote(emisor.NifEmisor, 0, 0, 0, 0, 0, "Sin certificado");
        }

        // Validacion XSD del lote literal antes de enviar
        var emisorInfo = await _repo.EmisorAsync(emisor.ClienteEmisorId, ct);
        var lote = ConstructorXmlRegistro.EnvioLote(emisor.NifEmisor, emisorInfo?.Nombre ?? "", trabajos.Select(t => System.Xml.Linq.XElement.Parse(t.Registro.XmlRegistro)));
        if (_opciones.ValidarXsd && _xsd.Disponible)
        {
            var errores = _xsd.Validar(lote);
            if (errores.Count > 0)
            {
                foreach (var t in trabajos)
                {
                    t.Estado.Estado = EstadoEnvio.ERROR_VALIDACION; t.Estado.DescripcionErrorAeat = string.Join(" | ", errores.Take(3));
                    t.Cola.Estado = EstadoEnvioPendiente.DEAD_LETTER; t.Cola.UltimoError = t.Estado.DescripcionErrorAeat; t.Cola.TomadoUtc = null;
                }
                await _repo.GuardarAsync(ct);
                return new ResultadoLote(emisor.NifEmisor, 0, 0, 0, 0, trabajos.Count, "XML no válido: " + errores[0]);
            }
        }

        var registros = trabajos.Select(t => new RegistroParaEnvio(t.Registro.Id, t.Registro.NifEmisor, t.Registro.Tipo.ToString(), "", t.Registro.Huella, t.Registro.XmlRegistro)).ToList();
        var loteEnvio = new LoteEnvio { GestoriaId = emisor.GestoriaId, NifEmisor = emisor.NifEmisor, FechaEnvioUtc = ahora, NumRegistros = registros.Count, Cliente = _cliente.Nombre };
        foreach (var t in trabajos) { t.Estado.Estado = EstadoEnvio.ENVIADO; t.Estado.FechaEnvioUtc = ahora; t.Estado.Intentos++; t.Cola.Intentos++; }

        try
        {
            var respuesta = await _cliente.RemitirAsync(new LoteRegistros(emisor.NifEmisor, registros, cert.HuellaDigital), ct);
            loteEnvio.Resultado = "OK"; loteEnvio.RespuestaXml = respuesta.XmlRespuesta; loteEnvio.TiempoEsperaSegundos = respuesta.TiempoEsperaSegundos;
            var loteId = await _repo.RegistrarLoteAsync(loteEnvio, ct);

            int ok = 0, conErrores = 0, rechazados = 0;
            var porId = respuesta.Registros.ToDictionary(r => r.RegistroId);
            foreach (var t in trabajos)
            {
                t.Estado.LoteEnvioId = loteId; t.Cola.LoteEnvioId = loteId; t.Estado.FechaRespuestaUtc = _reloj.AhoraUtc;
                if (!porId.TryGetValue(t.Registro.Id, out var r)) { Reintentar(t, "La respuesta no incluye este registro."); continue; }
                t.Estado.CsvAeat = r.Csv; t.Estado.CodigoErrorAeat = r.CodigoError; t.Estado.DescripcionErrorAeat = r.DescripcionError;
                switch (r.Resultado)
                {
                    case ResultadoRegistro.Aceptado: t.Estado.Estado = EstadoEnvio.ACEPTADO; ok++; break;
                    case ResultadoRegistro.AceptadoConErrores: t.Estado.Estado = EstadoEnvio.ACEPTADO_CON_ERRORES; conErrores++; break;   // alerta roja (H4/E6)
                    default: t.Estado.Estado = EstadoEnvio.RECHAZADO; rechazados++; break;   // no se reintenta: subsanacion con registro nuevo
                }
                t.Cola.Estado = EstadoEnvioPendiente.COMPLETADO; t.Cola.TomadoUtc = null;
            }
            // Control de flujo: el valor devuelto por la AEAT siempre manda sobre el configurado
            int esperaSeg = respuesta.TiempoEsperaSegundos > 0 ? respuesta.TiempoEsperaSegundos : _opciones.EsperaPorDefectoSegundos;
            if (cadena is not null && esperaSeg > 0) cadena.ProximoEnvioPermitidoUtc = _reloj.AhoraUtc.AddSeconds(esperaSeg);
            await _repo.GuardarAsync(ct);
            return new ResultadoLote(emisor.NifEmisor, registros.Count, ok, conErrores, rechazados, 0, null);
        }
        catch (ExcepcionEnvioAeat ex)
        {
            loteEnvio.Resultado = "ERROR_TECNICO"; loteEnvio.Error = ex.Message;
            await _repo.RegistrarLoteAsync(loteEnvio, ct);
            foreach (var t in trabajos) Reintentar(t, ex.Message);
            await _repo.GuardarAsync(ct);
            return new ResultadoLote(emisor.NifEmisor, registros.Count, 0, 0, 0, registros.Count, ex.Message);
        }
    }

    private void Reintentar(TrabajoEnvio t, string error)
    {
        t.Cola.UltimoError = error.Length > 1000 ? error[..1000] : error;
        t.Cola.TomadoUtc = null;
        t.Estado.DescripcionErrorAeat = t.Cola.UltimoError;
        if (t.Cola.Intentos >= PoliticaReintentos.MaximoIntentos)
        {
            t.Estado.Estado = EstadoEnvio.DEAD_LETTER;
            t.Cola.Estado = EstadoEnvioPendiente.DEAD_LETTER;   // nunca se descarta: reenvio manual
        }
        else
        {
            t.Estado.Estado = EstadoEnvio.ERROR_TECNICO;
            t.Cola.Estado = EstadoEnvioPendiente.PENDIENTE;
            t.Cola.ProximoIntentoUtc = _reloj.AhoraUtc.Add(PoliticaReintentos.Espera(t.Cola.Intentos));
        }
    }

    /// <summary>Reenvio manual desde la bandeja (DEAD_LETTER, BLOQUEADO o ERROR_TECNICO): vuelve a PENDIENTE ahora mismo con los intentos a cero.</summary>
    public async Task ReencolarAsync(long registroId, CancellationToken ct = default)
    {
        var estado = await _repo.EstadoEnvioAsync(registroId, ct) ?? throw new ExcepcionFacturacion("Registro desconocido.");
        if (estado.Estado.EsFinal()) throw new ExcepcionFacturacion("Este registro ya tiene respuesta definitiva de la AEAT.");
        var cola = await _repo.EnvioPendienteDeRegistroAsync(registroId, ct);
        if (cola is null) { await _repo.CrearEnvioPendienteAsync(registroId, ct); estado.Estado = EstadoEnvio.EN_COLA; estado.DescripcionErrorAeat = null; await _repo.GuardarAsync(ct); return; }
        cola.Estado = EstadoEnvioPendiente.PENDIENTE; cola.Intentos = 0; cola.ProximoIntentoUtc = _reloj.AhoraUtc; cola.TomadoUtc = null; cola.UltimoError = null;
        estado.Estado = EstadoEnvio.EN_COLA; estado.DescripcionErrorAeat = null; estado.CodigoErrorAeat = null;
        await _repo.GuardarAsync(ct);
    }

    /// <summary>Bloqueados sin certificado que no tienen fila de cola (se emitieron sin certificado): al cargarlo, se crea la fila.</summary>
    public async Task<int> DesbloquearAsync(IEnumerable<long> registroIds, CancellationToken ct = default)
    {
        int n = 0;
        foreach (var id in registroIds)
        {
            var estado = await _repo.EstadoEnvioAsync(id, ct);
            if (estado is null || estado.Estado != EstadoEnvio.BLOQUEADO_SIN_CERTIFICADO) continue;
            var cola = await _repo.EnvioPendienteDeRegistroAsync(id, ct);
            if (cola is null) { await _repo.CrearEnvioPendienteAsync(id, ct); }
            else { cola.Estado = EstadoEnvioPendiente.PENDIENTE; cola.ProximoIntentoUtc = _reloj.AhoraUtc; cola.TomadoUtc = null; }
            estado.Estado = EstadoEnvio.EN_COLA; estado.DescripcionErrorAeat = null;
            n++;
        }
        if (n > 0) await _repo.GuardarAsync(ct);
        return n;
    }
}
