using System.Security.Cryptography;
using System.Text.Json;
using Aserta.Aplicacion.Comun;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Exportacion;
using Aserta.Dominio.Facturacion;
using Aserta.Dominio.Nucleo;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Aplicacion.Exportacion;

public sealed record VistaPreviaExportacion(IReadOnlyList<ApunteExportable> Nuevos, int YaExportados, IReadOnlyList<Guid> DocumentosSinDatos);

/// <summary>
/// M5: recopila los apuntes de un cliente y periodo (facturas emitidas Veri*Factu y
/// facturas recibidas/tickets validados con datos extraidos), excluye lo ya
/// exportado (RD-11) salvo peticion explicita, genera el fichero con el exportador
/// elegido, lo conserva en el almacen y registra que se exporto.
/// </summary>
public sealed class ServicioExportacion
{
    private readonly IAsertaDb _db;
    private readonly IReadOnlyList<IExportadorContable> _exportadores;
    private readonly IAlmacenDocumental _almacen;
    private readonly IContextoUsuarioActual _usuario;
    private readonly IRelojSistema _reloj;
    private readonly IRegistroAuditoria _auditoria;

    public ServicioExportacion(IAsertaDb db, IEnumerable<IExportadorContable> exportadores, IAlmacenDocumental almacen, IContextoUsuarioActual usuario, IRelojSistema reloj, IRegistroAuditoria auditoria)
    {
        _db = db;
        _exportadores = exportadores.ToList();
        _almacen = almacen;
        _usuario = usuario;
        _reloj = reloj;
        _auditoria = auditoria;
    }

    public IReadOnlyList<IExportadorContable> Exportadores => _exportadores;

    public async Task<VistaPreviaExportacion> VistaPreviaAsync(Guid clienteId, int ejercicio, string periodo, bool incluirExportados, CancellationToken ct = default)
    {
        var (inicio, fin) = Periodo.Rango(ejercicio, periodo);
        var apuntes = new List<ApunteExportable>();
        var sinDatos = new List<Guid>();
        int yaExportados = 0;

        // Facturas emitidas (Veri*Factu): datos reales
        var facturas = await _db.FacturasEmitidas.AsNoTracking().Include(f => f.Lineas)
            .Where(f => f.ClienteEmisorId == clienteId && f.FechaExpedicion >= inicio && f.FechaExpedicion <= fin).ToListAsync(ct);
        var anuladas = await _db.RegistrosFacturacion.AsNoTracking().Where(r => r.Tipo == TipoRegistro.ANULACION && facturas.Select(f => f.Id).Contains(r.FacturaEmitidaId)).Select(r => r.FacturaEmitidaId).ToListAsync(ct);
        var exportadasF = await _db.ExportacionesFactura.AsNoTracking().Where(e => facturas.Select(f => f.Id).Contains(e.FacturaEmitidaId)).Select(e => e.FacturaEmitidaId).ToListAsync(ct);
        foreach (var f in facturas.Where(f => !anuladas.Contains(f.Id)))
        {
            if (exportadasF.Contains(f.Id) && !incluirExportados) { yaExportados++; continue; }
            var tipoIva = f.Lineas.Count == 0 ? 0m : f.Lineas.GroupBy(l => l.TipoIva).OrderByDescending(g => g.Sum(l => l.BaseLinea)).First().Key;
            apuntes.Add(new ApunteExportable(TipoApunte.FacturaEmitida, f.Id, f.FechaExpedicion, f.NumSerieFactura, f.DestinatarioNif, f.DestinatarioNombre ?? "(simplificada)",
                f.BaseTotal, tipoIva, f.CuotaTotal, f.CuotaRecargoTotal, f.RetencionTotal, f.ImporteTotal, f.Descripcion, "Verifactu", true));
        }

        // Facturas recibidas y tickets validados: datos extraidos (OCR simulado), sugeridos
        var docs = await _db.Documentos.AsNoTracking()
            .Where(d => d.ClienteId == clienteId && d.Estado == EstadoDocumento.Validado && (d.Tipo == TipoDocumento.FacturaRecibida || d.Tipo == TipoDocumento.Ticket))
            .ToListAsync(ct);
        docs = docs.Where(d => { var (di, df) = Periodo.Rango(d.Ejercicio, d.Periodo); return di >= inicio && df <= fin; }).ToList();
        var exportadosD = await _db.ExportacionesDocumento.AsNoTracking().Where(e => docs.Select(d => d.Id).Contains(e.DocumentoId)).Select(e => e.DocumentoId).ToListAsync(ct);
        foreach (var d in docs)
        {
            if (exportadosD.Contains(d.Id) && !incluirExportados) { yaExportados++; continue; }
            var apunte = DesdeDatosExtraidos(d);
            if (apunte is null) { sinDatos.Add(d.Id); continue; }
            apuntes.Add(apunte);
        }
        return new VistaPreviaExportacion(apuntes, yaExportados, sinDatos);
    }

    public async Task<ExportacionContable> ExportarAsync(Guid clienteId, int ejercicio, string periodo, string formato, bool incluirExportados, CancellationToken ct = default)
    {
        if (_usuario.ClienteId is not null) throw new ExcepcionNoAutorizado("Solo la gestoría exporta.");
        var exportador = _exportadores.FirstOrDefault(e => e.Formato == formato) ?? throw new ExcepcionValidacion("Formato", "Formato desconocido.");
        if (!exportador.Disponible) throw new ExcepcionValidacion("Formato", exportador.Descripcion);
        if (!Periodo.EsValido(periodo)) throw new ExcepcionValidacion("Periodo", "Periodo no válido.");
        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clienteId, ct) ?? throw new ExcepcionNoEncontrado("Cliente", clienteId);

        var previa = await VistaPreviaAsync(clienteId, ejercicio, periodo, incluirExportados, ct);
        if (previa.Nuevos.Count == 0) throw new ExcepcionValidacion("Periodo", previa.YaExportados > 0
            ? $"No hay registros nuevos: los {previa.YaExportados} del periodo ya se exportaron (RD-11). Marque «incluir ya exportados» si quiere repetirlos."
            : "No hay registros que exportar en ese periodo.");

        var fichero = exportador.Exportar(previa.Nuevos, cliente.Nif, ejercicio, periodo);
        var clave = await _almacen.GuardarAsync(new MemoryStream(fichero.Contenido), ct);
        var exportacion = new ExportacionContable
        {
            Id = Guid.NewGuid(), GestoriaId = cliente.GestoriaId, ClienteId = clienteId, Formato = formato, Ejercicio = (short)ejercicio, Periodo = periodo,
            FechaGeneracionUtc = _reloj.AhoraUtc, UsuarioId = _usuario.UsuarioId!.Value, ClaveAlmacen = clave, NombreFichero = fichero.NombreFichero,
            NumRegistros = previa.Nuevos.Count, HashSha256 = Convert.ToHexString(SHA256.HashData(fichero.Contenido)), IncluyoExportados = incluirExportados,
        };
        _db.Exportaciones.Add(exportacion);
        foreach (var a in previa.Nuevos)
        {
            if (a.Tipo == TipoApunte.FacturaEmitida)
            {
                if (!await _db.ExportacionesFactura.AnyAsync(x => x.FacturaEmitidaId == a.ReferenciaId && x.ExportacionId == exportacion.Id, ct))
                    _db.ExportacionesFactura.Add(new ExportacionFactura { ExportacionId = exportacion.Id, FacturaEmitidaId = a.ReferenciaId, GestoriaId = cliente.GestoriaId });
            }
            else _db.ExportacionesDocumento.Add(new ExportacionDocumento { ExportacionId = exportacion.Id, DocumentoId = a.ReferenciaId, GestoriaId = cliente.GestoriaId });
        }
        _auditoria.Registrar(AccionesAuditoria.Alta, nameof(ExportacionContable), exportacion.Id.ToString(), new { formato, ejercicio, periodo, registros = previa.Nuevos.Count, hash = exportacion.HashSha256 });
        await _db.GuardarCambiosAsync(ct);
        return exportacion;
    }

    public async Task<(ExportacionContable Exportacion, Stream Contenido, string TipoMime)> DescargarAsync(Guid exportacionId, CancellationToken ct = default)
    {
        var e = await _db.Exportaciones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == exportacionId, ct) ?? throw new ExcepcionNoEncontrado("Exportación", exportacionId);
        var tipo = e.Formato == "CsvGenerico" ? "text/csv; charset=utf-8" : "application/octet-stream";
        return (e, await _almacen.AbrirAsync(e.ClaveAlmacen, ct), tipo);
    }

    public Task<List<ExportacionContable>> HistoricoAsync(Guid? clienteId, CancellationToken ct = default)
    {
        var q = _db.Exportaciones.AsNoTracking().AsQueryable();
        if (clienteId is Guid c) q = q.Where(e => e.ClienteId == c);
        return q.OrderByDescending(e => e.FechaGeneracionUtc).Take(100).ToListAsync(ct);
    }

    /// <summary>Apunte a partir del JSON del extractor (simulado): proveedor, nifProveedor, numeroFactura, fecha, baseImponible, tipoIva, cuotaIva, total.</summary>
    private static ApunteExportable? DesdeDatosExtraidos(Documento d)
    {
        if (string.IsNullOrEmpty(d.DatosExtraidos)) return null;
        try
        {
            using var json = JsonDocument.Parse(d.DatosExtraidos);
            var r = json.RootElement;
            decimal Dec(string n) => r.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : 0m;
            string Txt(string n) => r.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
            var fecha = DateOnly.TryParse(Txt("fecha"), out var f) ? f : Periodo.Rango(d.Ejercicio, d.Periodo).Fin;
            return new ApunteExportable(TipoApunte.FacturaRecibida, d.Id, fecha, Txt("numeroFactura") is { Length: > 0 } num ? num : d.NombreOriginal, Txt("nifProveedor"), Txt("proveedor"),
                Dec("baseImponible"), Dec("tipoIva"), Dec("cuotaIva"), 0m, 0m, Dec("total"), d.Tipo.Etiqueta() + ": " + d.NombreOriginal, d.OrigenExtraccion ?? "OCR", false);
        }
        catch { return null; }
    }
}
