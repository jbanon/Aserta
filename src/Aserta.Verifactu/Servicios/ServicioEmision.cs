using Aserta.Dominio.Facturacion;
using Aserta.Verifactu.Comun;
using Aserta.Verifactu.Huella;
using Aserta.Verifactu.Qr;
using Aserta.Verifactu.Xml;

namespace Aserta.Verifactu.Servicios;

public sealed class LineaNueva
{
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal TipoIva { get; set; } = 21m;
    public decimal? TipoRecargo { get; set; }
    public bool Exenta { get; set; }
}

public sealed class FacturaNueva
{
    public Guid ClienteEmisorId { get; set; }
    public string CodigoSerie { get; set; } = "A";
    public bool Simplificada { get; set; }
    public string? DestinatarioNif { get; set; }
    public string? DestinatarioNombre { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal PorcentajeRetencion { get; set; }
    public List<LineaNueva> Lineas { get; set; } = [];
    /// <summary>Solo rectificativas: factura original y motivo en lenguaje llano.</summary>
    public Guid? FacturaRectificadaId { get; set; }
    public MotivoRectificacion? Motivo { get; set; }
    public string? TextoMotivo { get; set; }
}

public sealed record ResultadoEmision(FacturaEmitida Factura, long RegistroId, string Huella, long NumeroEnCadena, string UrlQr, bool PdfEncolado, EstadoEnvio EstadoInicial);

/// <summary>
/// Emision, rectificacion y anulacion (especificacion.md §3-5, envio-y-cola-reintentos.md §1-2):
/// la factura se emite, se registra, se calcula su huella y se encola su envio y su
/// PDF INMEDIATAMENTE, con independencia de la AEAT. Todo en UNA transaccion con la
/// fila CadenaEmisor bloqueada (RD-05).
/// </summary>
public sealed class ServicioEmision
{
    private readonly IRepositorioFacturacion _repo;
    private readonly IProveedorCertificado _certificados;
    private readonly IColaPdf _colaPdf;
    private readonly IRelojVerifactu _reloj;
    private readonly OpcionesVerifactu _opciones;
    private readonly ValidadorXsd _xsd;

    public ServicioEmision(IRepositorioFacturacion repo, IProveedorCertificado certificados, IColaPdf colaPdf, IRelojVerifactu reloj, OpcionesVerifactu opciones, ValidadorXsd xsd)
    {
        _repo = repo;
        _certificados = certificados;
        _colaPdf = colaPdf;
        _reloj = reloj;
        _opciones = opciones;
        _xsd = xsd;
    }

    public async Task<ResultadoEmision> EmitirAsync(FacturaNueva datos, Guid usuarioId, CancellationToken ct = default)
    {
        var emisor = await _repo.EmisorAsync(datos.ClienteEmisorId, ct) ?? throw new ExcepcionFacturacion("Emisor desconocido.");
        if (emisor.Foral) throw new ExcepcionFacturacion("El territorio foral (País Vasco y Navarra) queda fuera de Veri*Factu (TicketBAI): no se puede facturar desde esta plataforma.");
        Validar(datos);

        // Rectificativa: codigo derivado del motivo (nunca se pregunta el codigo al usuario)
        FacturaEmitida? original = null;
        TipoFactura tipo = datos.Simplificada ? TipoFactura.F2 : TipoFactura.F1;
        if (datos.FacturaRectificadaId is Guid idOriginal)
        {
            original = await _repo.FacturaAsync(idOriginal, ct) ?? throw new ExcepcionFacturacion("La factura a rectificar no existe.");
            if (original.ClienteEmisorId != emisor.ClienteId) throw new ExcepcionFacturacion("La factura a rectificar no es de este emisor.");
            if (datos.Motivo is null) throw new ExcepcionFacturacion("Indique por qué rectifica la factura.");
            if (datos.Motivo is MotivoRectificacion.ConcursoAcreedores or MotivoRectificacion.CreditoIncobrable)
                throw new ExcepcionFacturacion("Las rectificativas por concurso (R2) o crédito incobrable (R3) están modeladas pero no se emiten en la demo.");
            tipo = datos.Motivo.Value.CodigoRectificativa(original.TipoFactura);
            datos.CodigoSerie = "R";
        }

        var totales = CalculadoraImportes.Calcular(datos.Lineas.Select(l => new LineaImporte(l.Descripcion, l.Cantidad, l.PrecioUnitario, l.TipoIva, l.TipoRecargo, l.Exenta)), datos.PorcentajeRetencion);
        var ahoraMadrid = _reloj.AhoraEnMadrid;
        var fechaExpedicion = DateOnly.FromDateTime(ahoraMadrid.DateTime);
        int ejercicio = fechaExpedicion.Year;

        var serie = await _repo.SerieAsync(emisor.ClienteId, datos.CodigoSerie, ejercicio, ct);
        await using var tx = await _repo.IniciarEmisionAsync(emisor, serie.Id, ct);

        var numSerie = tx.Serie.SiguienteNumSerieFactura();
        ConstructorUrlQr.Validar(emisor.Nif, numSerie, totales.ImporteTotal);
        var urlQr = ConstructorUrlQr.ConstruirUrlQr(_opciones.Qr, emisor.Nif, numSerie, fechaExpedicion, totales.ImporteTotal);

        var factura = new FacturaEmitida
        {
            Id = Guid.NewGuid(), GestoriaId = emisor.GestoriaId, ClienteEmisorId = emisor.ClienteId, NifEmisor = emisor.Nif, SerieId = tx.Serie.Id,
            NumSerieFactura = numSerie, FechaExpedicion = fechaExpedicion, TipoFactura = tipo,
            TipoRectificativa = original is null ? null : TipoRectificativa.I, FacturaRectificadaId = original?.Id, MotivoRectificacion = original is null ? null : $"{datos.Motivo}: {datos.TextoMotivo}".Trim(),
            DestinatarioNif = datos.Simplificada ? null : datos.DestinatarioNif?.Trim().ToUpperInvariant(), DestinatarioNombre = datos.Simplificada ? null : datos.DestinatarioNombre?.Trim(),
            BaseTotal = totales.BaseTotal, CuotaTotal = totales.CuotaTotal, CuotaRecargoTotal = totales.CuotaRecargoTotal, PorcentajeRetencion = datos.PorcentajeRetencion, RetencionTotal = totales.RetencionTotal, ImporteTotal = totales.ImporteTotal,
            Descripcion = datos.Descripcion.Trim(), FechaHoraCreacionUtc = _reloj.AhoraUtc, UsuarioId = usuarioId, UrlQr = urlQr,
        };
        int orden = 0;
        foreach (var l in datos.Lineas)
            factura.Lineas.Add(new LineaFactura { GestoriaId = factura.GestoriaId, FacturaEmitidaId = factura.Id, Orden = ++orden, Descripcion = l.Descripcion.Trim(), Cantidad = l.Cantidad, PrecioUnitario = l.PrecioUnitario, TipoIva = l.Exenta ? 0m : l.TipoIva, TipoRecargo = l.Exenta ? null : l.TipoRecargo, Exenta = l.Exenta, BaseLinea = Math.Round(l.Cantidad * l.PrecioUnitario, 2) });

        // Huella encadenada (RD-05): la fila CadenaEmisor esta bloqueada por la transaccion
        var cadena = tx.Cadena;
        bool primero = cadena.UltimoNumero == 0;
        var fechaHoraTexto = FormatosVerifactu.FechaHoraHuso(ahoraMadrid);
        var datosHuella = new DatosHuellaAlta(emisor.Nif, numSerie, FormatosVerifactu.Fecha(fechaExpedicion), tipo.ToString(),
            FormatosVerifactu.Importe(totales.CuotaTotal + totales.CuotaRecargoTotal), FormatosVerifactu.Importe(totales.ImporteTotal), primero ? null : cadena.UltimaHuella, fechaHoraTexto);
        var cadenaHuella = CalculadoraHuella.ConstruirCadenaAlta(datosHuella);
        var huella = CalculadoraHuella.CalcularHuella(cadenaHuella);

        var anterior = primero ? null : await UltimoRegistroAsync(cadena, ct);
        var xml = ConstructorXmlRegistro.RegistroAlta(new DatosRegistroAlta(
            new IdFactura(emisor.Nif, numSerie, fechaExpedicion), emisor.Nombre, tipo.ToString(), factura.TipoRectificativa?.ToString(),
            original is null ? null : new IdFactura(original.NifEmisor, original.NumSerieFactura, original.FechaExpedicion),
            factura.Descripcion, factura.DestinatarioNif, factura.DestinatarioNombre,
            totales.Desglose.Select(d => new LineaDesglose(ConstructorXmlRegistro.ClaveRegimenGeneral, ConstructorXmlRegistro.CalificacionSujetaNoExenta, d.Exenta, d.Exenta ? ConstructorXmlRegistro.ExencionArt20 : null, d.TipoIva, d.Base, d.Cuota, d.TipoRecargo, d.CuotaRecargo)).ToList(),
            totales.CuotaTotal + totales.CuotaRecargoTotal, totales.ImporteTotal, primero, anterior, primero ? null : cadena.UltimaHuella, fechaHoraTexto, huella, _opciones.SistemaInformatico));

        var registro = new RegistroFacturacion
        {
            GestoriaId = factura.GestoriaId, NifEmisor = emisor.Nif, Tipo = TipoRegistro.ALTA, FacturaEmitidaId = factura.Id, NumeroEnCadena = cadena.UltimoNumero + 1,
            PrimerRegistro = primero, HuellaAnterior = primero ? null : cadena.UltimaHuella, Huella = huella, FechaHoraHusoGenRegistro = ahoraMadrid, FechaHoraHusoGenRegistroTexto = fechaHoraTexto,
            CadenaHuella = cadenaHuella, XmlRegistro = xml.ToString(System.Xml.Linq.SaveOptions.DisableFormatting),
            IdSistemaInformatico = _opciones.Sistema.IdSistemaInformatico, VersionSistemaInformatico = _opciones.Sistema.Version, NumeroInstalacion = _opciones.Sistema.NumeroInstalacion,
        };

        var (estadoInicial, error) = await EstadoInicialAsync(emisor, xml, ct);
        tx.Serie.UltimoNumero++;
        cadena.UltimaHuella = huella; cadena.UltimoNumero++; cadena.FechaUltimoRegistroUtc = _reloj.AhoraUtc;

        tx.Agregar(factura);
        tx.Agregar(registro);
        tx.Agregar(new EstadoEnvioRegistro { GestoriaId = factura.GestoriaId, Estado = estadoInicial, DescripcionErrorAeat = error });
        if (estadoInicial == EstadoEnvio.EN_COLA)
            tx.Agregar(new EnvioPendiente { GestoriaId = factura.GestoriaId, NifEmisor = emisor.Nif, FechaAltaUtc = _reloj.AhoraUtc, ProximoIntentoUtc = _reloj.AhoraUtc, Estado = EstadoEnvioPendiente.PENDIENTE });
        tx.Agregar(new FacturaPdf { FacturaEmitidaId = factura.Id, GestoriaId = factura.GestoriaId });
        var registroId = await tx.ConfirmarAsync(ct);

        bool pdfEncolado = _colaPdf.Encolar(factura.Id);
        return new ResultadoEmision(factura, registroId, huella, registro.NumeroEnCadena, urlQr, pdfEncolado, estadoInicial);
    }

    /// <summary>Registro de anulacion: fila nueva en la cadena; la factura y su registro de alta permanecen intactos (especificacion.md §5.1).</summary>
    public async Task<ResultadoEmision> AnularAsync(Guid facturaId, Guid usuarioId, CancellationToken ct = default)
    {
        var factura = await _repo.FacturaAsync(facturaId, ct) ?? throw new ExcepcionFacturacion("La factura no existe.");
        if (await _repo.TieneAnulacionAsync(facturaId, ct)) throw new ExcepcionFacturacion("Esta factura ya está anulada.");
        var emisor = await _repo.EmisorAsync(factura.ClienteEmisorId, ct) ?? throw new ExcepcionFacturacion("Emisor desconocido.");

        await using var tx = await _repo.IniciarEmisionAsync(emisor, factura.SerieId, ct);
        var cadena = tx.Cadena;
        var ahoraMadrid = _reloj.AhoraEnMadrid;
        var fechaHoraTexto = FormatosVerifactu.FechaHoraHuso(ahoraMadrid);
        bool primero = cadena.UltimoNumero == 0;
        var datos = new DatosHuellaAnulacion(emisor.Nif, factura.NumSerieFactura, FormatosVerifactu.Fecha(factura.FechaExpedicion), primero ? null : cadena.UltimaHuella, fechaHoraTexto);
        var cadenaHuella = CalculadoraHuella.ConstruirCadenaAnulacion(datos);
        var huella = CalculadoraHuella.CalcularHuella(cadenaHuella);
        var anterior = primero ? null : await UltimoRegistroAsync(cadena, ct);
        var xml = ConstructorXmlRegistro.RegistroAnulacion(new DatosRegistroAnulacion(new IdFactura(emisor.Nif, factura.NumSerieFactura, factura.FechaExpedicion), primero, anterior, primero ? null : cadena.UltimaHuella, fechaHoraTexto, huella, _opciones.SistemaInformatico));

        var registro = new RegistroFacturacion
        {
            GestoriaId = factura.GestoriaId, NifEmisor = emisor.Nif, Tipo = TipoRegistro.ANULACION, FacturaEmitidaId = factura.Id, NumeroEnCadena = cadena.UltimoNumero + 1,
            PrimerRegistro = primero, HuellaAnterior = primero ? null : cadena.UltimaHuella, Huella = huella, FechaHoraHusoGenRegistro = ahoraMadrid, FechaHoraHusoGenRegistroTexto = fechaHoraTexto,
            CadenaHuella = cadenaHuella, XmlRegistro = xml.ToString(System.Xml.Linq.SaveOptions.DisableFormatting),
            IdSistemaInformatico = _opciones.Sistema.IdSistemaInformatico, VersionSistemaInformatico = _opciones.Sistema.Version, NumeroInstalacion = _opciones.Sistema.NumeroInstalacion,
        };
        var (estadoInicial, error) = await EstadoInicialAsync(emisor, xml, ct);
        cadena.UltimaHuella = huella; cadena.UltimoNumero++; cadena.FechaUltimoRegistroUtc = _reloj.AhoraUtc;
        tx.Agregar(registro);
        tx.Agregar(new EstadoEnvioRegistro { GestoriaId = factura.GestoriaId, Estado = estadoInicial, DescripcionErrorAeat = error });
        if (estadoInicial == EstadoEnvio.EN_COLA)
            tx.Agregar(new EnvioPendiente { GestoriaId = factura.GestoriaId, NifEmisor = emisor.Nif, FechaAltaUtc = _reloj.AhoraUtc, ProximoIntentoUtc = _reloj.AhoraUtc, Estado = EstadoEnvioPendiente.PENDIENTE });
        var registroId = await tx.ConfirmarAsync(ct);
        return new ResultadoEmision(factura, registroId, huella, registro.NumeroEnCadena, factura.UrlQr, false, estadoInicial);
    }

    /// <summary>EN_COLA salvo que falte certificado/apoderamiento (BLOQUEADO_SIN_CERTIFICADO) o el XML no valide (ERROR_VALIDACION).</summary>
    private async Task<(EstadoEnvio Estado, string? Error)> EstadoInicialAsync(EmisorFacturacion emisor, System.Xml.Linq.XElement registro, CancellationToken ct)
    {
        if (_opciones.ValidarXsd && _xsd.Disponible)
        {
            var errores = _xsd.Validar(ConstructorXmlRegistro.EnvioLote(emisor.Nif, emisor.Nombre, [registro]));
            if (errores.Count > 0) return (EstadoEnvio.ERROR_VALIDACION, string.Join(" | ", errores.Take(3)));
        }
        var cert = await _certificados.ObtenerAsync(emisor.GestoriaId, emisor.Nif, emisor.ClienteId, ct);
        if (cert is null) return (EstadoEnvio.BLOQUEADO_SIN_CERTIFICADO, "No hay certificado propio del obligado ni certificado de la gestoría con apoderamiento vigente (ADR-002 §3.1).");
        return (EstadoEnvio.EN_COLA, null);
    }

    private async Task<IdFactura?> UltimoRegistroAsync(CadenaEmisor cadena, CancellationToken ct)
    {
        // El registro anterior se identifica por su factura (IDEmisor, NumSerie, Fecha): lo busca el repositorio por NumeroEnCadena
        var ultimo = await _repo.RegistroPorNumeroAsync(cadena.NifEmisor, cadena.UltimoNumero, ct);
        if (ultimo is null) return null;
        var f = await _repo.FacturaAsync(ultimo.FacturaEmitidaId, ct);
        return f is null ? null : new IdFactura(f.NifEmisor, f.NumSerieFactura, f.FechaExpedicion);
    }

    private static void Validar(FacturaNueva d)
    {
        if (d.Lineas.Count == 0) throw new ExcepcionFacturacion("La factura necesita al menos una línea.");
        if (d.Lineas.Any(l => string.IsNullOrWhiteSpace(l.Descripcion))) throw new ExcepcionFacturacion("Todas las líneas necesitan descripción.");
        if (d.Lineas.Any(l => l.Cantidad <= 0)) throw new ExcepcionFacturacion("La cantidad de cada línea debe ser mayor que cero.");
        if (d.Lineas.Any(l => !l.Exenta && !CalculadoraImportes.TiposIvaAdmitidos.Contains(l.TipoIva))) throw new ExcepcionFacturacion("Tipo de IVA no admitido (21, 10, 4 o 0).");
        if (string.IsNullOrWhiteSpace(d.Descripcion)) throw new ExcepcionFacturacion("Indique la descripción de la operación.");
        if (d.PorcentajeRetencion is < 0 or > 50) throw new ExcepcionFacturacion("Retención entre 0 y 50 %.");
        if (!d.Simplificada)
        {
            if (string.IsNullOrWhiteSpace(d.DestinatarioNombre)) throw new ExcepcionFacturacion("Una factura completa identifica al destinatario (nombre).");
            var nif = Dominio.Comun.ValidadorNif.Validar(d.DestinatarioNif);
            if (!nif.EsValido) throw new ExcepcionFacturacion("NIF del destinatario: " + nif.Error);
        }
        if (d.Simplificada && d.Lineas.Sum(l => l.Cantidad * l.PrecioUnitario) > 3000m)
            throw new ExcepcionFacturacion("Una factura simplificada no puede superar 3.000 € [VERIFICAR umbral art. 4 RD 1619/2012].");
    }
}
