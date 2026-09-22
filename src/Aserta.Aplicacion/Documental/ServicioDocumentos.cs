using System.Security.Cryptography;
using Aserta.Aplicacion.Comun;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Aplicacion.Documental;

public sealed class SubidaDocumento
{
    public Guid ClienteId { get; set; }
    public TipoDocumento Tipo { get; set; } = TipoDocumento.FacturaRecibida;
    public int Ejercicio { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public string NombreOriginal { get; set; } = string.Empty;
    public string TipoMime { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public Stream Contenido { get; set; } = Stream.Null;
    public string? Notas { get; set; }
}

/// <summary>Casos de uso de la gestion documental (M3) y de la subida desde el portal (M2).</summary>
public sealed class ServicioDocumentos
{
    public const long TamanoMaximoBytes = 20 * 1024 * 1024;
    public static readonly IReadOnlySet<string> MimesPermitidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp", "image/heic", "application/pdf" };

    private readonly IAsertaDb _db;
    private readonly IAlmacenDocumental _almacen;
    private readonly IExtractorDocumental _extractor;
    private readonly IPublicadorEventos _eventos;
    private readonly IContextoUsuarioActual _usuario;
    private readonly IRelojSistema _reloj;

    public ServicioDocumentos(IAsertaDb db, IAlmacenDocumental almacen, IExtractorDocumental extractor, IPublicadorEventos eventos, IContextoUsuarioActual usuario, IRelojSistema reloj)
    {
        _db = db;
        _almacen = almacen;
        _extractor = extractor;
        _eventos = eventos;
        _usuario = usuario;
        _reloj = reloj;
    }

    public async Task<Documento> SubirAsync(SubidaDocumento s, CancellationToken ct = default)
    {
        var gestoriaId = _usuario.GestoriaId ?? throw new ExcepcionNoAutorizado("Sin gestoría en el contexto.");
        var usuarioId = _usuario.UsuarioId ?? throw new ExcepcionNoAutorizado("Sin usuario en el contexto.");
        if (_usuario.ClienteId is Guid propio && propio != s.ClienteId) throw new ExcepcionNoAutorizado("Solo puede subir documentos de su propia empresa.");

        var errores = new Dictionary<string, string[]>();
        if (!MimesPermitidos.Contains(s.TipoMime)) errores["Fichero"] = ["Solo se admiten imágenes (JPG, PNG, WEBP, HEIC) o PDF."];
        if (s.TamanoBytes <= 0) errores["Fichero"] = ["El fichero está vacío."];
        if (s.TamanoBytes > TamanoMaximoBytes) errores["Fichero"] = ["El fichero supera los 20 MB."];
        if (!Periodo.EsValido(s.Periodo) || s.Periodo is "1P" or "2P" or "3P") errores["Periodo"] = ["Periodo no válido."];
        if (s.Ejercicio is < 2020 or > 2100) errores["Ejercicio"] = ["Ejercicio no válido."];
        if (!await _db.Clientes.AnyAsync(c => c.Id == s.ClienteId, ct)) errores["ClienteId"] = ["Cliente desconocido."];
        if (errores.Count > 0) throw new ExcepcionValidacion(errores);

        // Hash primero (detecta el ticket subido dos veces desde el movil), luego almacen
        using var memoria = new MemoryStream();
        await s.Contenido.CopyToAsync(memoria, ct);
        memoria.Position = 0;
        var hash = Convert.ToHexString(SHA256.HashData(memoria.ToArray()));
        var duplicado = await _db.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.ClienteId == s.ClienteId && d.HashSha256 == hash, ct);
        if (duplicado is not null)
            throw new ExcepcionValidacion("Fichero", $"Este fichero ya se subió el {duplicado.FechaSubidaUtc.ToLocalTime():dd/MM/yyyy} como «{duplicado.NombreOriginal}» ({duplicado.Tipo.Etiqueta()} de {Periodo.Etiqueta(duplicado.Ejercicio, duplicado.Periodo)}).");

        memoria.Position = 0;
        var clave = await _almacen.GuardarAsync(memoria, ct);
        var extraccion = await _extractor.ExtraerAsync(s.NombreOriginal, s.TipoMime, hash, s.Tipo.ToString(), ct);

        var doc = new Documento
        {
            Id = Guid.NewGuid(), GestoriaId = gestoriaId, ClienteId = s.ClienteId, Tipo = s.Tipo, Ejercicio = (short)s.Ejercicio, Periodo = s.Periodo,
            NombreOriginal = Path.GetFileName(s.NombreOriginal).Trim() is { Length: > 0 } n ? (n.Length > 260 ? n[..260] : n) : "documento",
            ClaveAlmacen = clave, HashSha256 = hash, TamanoBytes = s.TamanoBytes, TipoMime = s.TipoMime.ToLowerInvariant(),
            Estado = EstadoDocumento.Recibido, SubidoPorId = usuarioId, FechaSubidaUtc = _reloj.AhoraUtc,
            DatosExtraidos = extraccion?.Json, OrigenExtraccion = extraccion?.OrigenExtraccion, Notas = string.IsNullOrWhiteSpace(s.Notas) ? null : s.Notas.Trim(),
        };
        _db.Documentos.Add(doc);
        await VincularConObligacionesAsync(doc, ct);
        await _db.GuardarCambiosAsync(ct);
        await _eventos.PublicarAsync(new DocumentoRecibido(doc.Id, doc.ClienteId, usuarioId, _usuario.ClienteId is not null), ct);
        await _db.GuardarCambiosAsync(ct);
        return doc;
    }

    /// <summary>Un documento de julio se vincula a las obligaciones del 3T y a las mensuales de julio del mismo ejercicio (y deja rastro en su historial).</summary>
    private async Task VincularConObligacionesAsync(Documento doc, CancellationToken ct)
    {
        var (inicio, fin) = Periodo.Rango(doc.Ejercicio, doc.Periodo);
        var candidatas = await _db.Obligaciones.Where(o => o.ClienteId == doc.ClienteId && o.Ejercicio == doc.Ejercicio && o.Estado != EstadoObligacion.NoAplica && o.Estado != EstadoObligacion.Cerrado).ToListAsync(ct);
        foreach (var o in candidatas)
        {
            var (oi, of) = Periodo.Rango(o.Ejercicio, o.Periodo);
            bool contiene = oi <= inicio && of >= fin && o.Periodo != "AN"; // el anual no se vincula con cada ticket
            if (!contiene) continue;
            _db.DocumentosObligacion.Add(new DocumentoObligacion { DocumentoId = doc.Id, ObligacionId = o.Id, GestoriaId = doc.GestoriaId });
            _db.ObligacionHistoriales.Add(new ObligacionHistorial
            {
                GestoriaId = o.GestoriaId, ObligacionId = o.Id, FechaUtc = _reloj.AhoraUtc, UsuarioId = _usuario.UsuarioId,
                TipoEvento = TiposEventoHistorial.DocumentoVinculado, Comentario = $"{doc.Tipo.Etiqueta()} «{doc.NombreOriginal}» recibido.", ReferenciaId = doc.Id.ToString()
            });
        }
    }

    public async Task<Documento> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var d = await _db.Documentos.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new ExcepcionNoEncontrado("Documento", id);
        if (_usuario.ClienteId is Guid propio && propio != d.ClienteId) throw new ExcepcionNoAutorizado("Este documento no es de su empresa.");
        return d;
    }

    public async Task<(Stream Contenido, Documento Documento)> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        var d = await ObtenerAsync(id, ct);
        return (await _almacen.AbrirAsync(d.ClaveAlmacen, ct), d);
    }

    private void SoloGestoria()
    {
        if (_usuario.ClienteId is not null) throw new ExcepcionNoAutorizado("Solo la gestoría revisa documentos.");
    }

    public async Task<Documento> MarcarEnRevisionAsync(Guid id, CancellationToken ct = default)
    {
        SoloGestoria();
        var d = await ObtenerAsync(id, ct);
        d.MarcarEnRevision(_usuario.UsuarioId!.Value, _reloj.AhoraUtc);
        await _db.GuardarCambiosAsync(ct);
        return d;
    }

    public async Task<Documento> ValidarAsync(Guid id, CancellationToken ct = default)
    {
        SoloGestoria();
        var d = await ObtenerAsync(id, ct);
        d.Validar(_usuario.UsuarioId!.Value, _reloj.AhoraUtc);
        await _db.GuardarCambiosAsync(ct);
        await _eventos.PublicarAsync(new DocumentoValidado(d.Id, d.ClienteId, d.Ejercicio, d.Periodo), ct);
        await _db.GuardarCambiosAsync(ct);
        return d;
    }

    public async Task<Documento> RechazarAsync(Guid id, string motivo, CancellationToken ct = default)
    {
        SoloGestoria();
        var d = await ObtenerAsync(id, ct);
        d.Rechazar(_usuario.UsuarioId!.Value, _reloj.AhoraUtc, motivo);
        await _db.GuardarCambiosAsync(ct);
        return d;
    }

    public async Task VincularAsync(Guid documentoId, Guid obligacionId, CancellationToken ct = default)
    {
        SoloGestoria();
        var d = await ObtenerAsync(documentoId, ct);
        var o = await _db.Obligaciones.FirstOrDefaultAsync(x => x.Id == obligacionId && x.ClienteId == d.ClienteId, ct) ?? throw new ExcepcionNoEncontrado("Obligación", obligacionId);
        if (await _db.DocumentosObligacion.AnyAsync(x => x.DocumentoId == documentoId && x.ObligacionId == obligacionId, ct)) return;
        _db.DocumentosObligacion.Add(new DocumentoObligacion { DocumentoId = d.Id, ObligacionId = o.Id, GestoriaId = d.GestoriaId });
        _db.ObligacionHistoriales.Add(new ObligacionHistorial { GestoriaId = o.GestoriaId, ObligacionId = o.Id, FechaUtc = _reloj.AhoraUtc, UsuarioId = _usuario.UsuarioId, TipoEvento = TiposEventoHistorial.DocumentoVinculado, Comentario = $"{d.Tipo.Etiqueta()} «{d.NombreOriginal}» vinculado a mano.", ReferenciaId = d.Id.ToString() });
        await _db.GuardarCambiosAsync(ct);
    }

    /// <summary>Archiva el justificante de presentacion (paso 7 del ciclo) y cierra la obligacion.</summary>
    public async Task ArchivarJustificanteAsync(Guid obligacionId, SubidaDocumento subida, bool gestoriaExigeAprobacion, CancellationToken ct = default)
    {
        SoloGestoria();
        subida.Tipo = TipoDocumento.Justificante;
        var o = await _db.Obligaciones.Include(x => x.Historial).FirstOrDefaultAsync(x => x.Id == obligacionId, ct) ?? throw new ExcepcionNoEncontrado("Obligación", obligacionId);
        subida.ClienteId = o.ClienteId; subida.Ejercicio = o.Ejercicio; subida.Periodo = o.Periodo is "1P" or "2P" or "3P" ? "AN" : o.Periodo;
        var doc = await SubirAsync(subida, ct);
        doc.Validar(_usuario.UsuarioId!.Value, _reloj.AhoraUtc);
        o.JustificanteDocumentoId = doc.Id;
        o.Historial.Add(new ObligacionHistorial { GestoriaId = o.GestoriaId, ObligacionId = o.Id, FechaUtc = _reloj.AhoraUtc, UsuarioId = _usuario.UsuarioId, TipoEvento = TiposEventoHistorial.JustificanteArchivado, Comentario = $"Justificante «{doc.NombreOriginal}» archivado.", ReferenciaId = doc.Id.ToString() });
        if (o.Estado == EstadoObligacion.Presentado) o.CambiarEstado(EstadoObligacion.Cerrado, gestoriaExigeAprobacion, _usuario.UsuarioId, _reloj.AhoraUtc, "Cerrada al archivar el justificante.");
        await _db.GuardarCambiosAsync(ct);
    }
}
