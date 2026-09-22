using Aserta.Dominio.Clientes;
using Aserta.Dominio.Facturacion;
using Aserta.Infraestructura.Persistencia;
using Aserta.Verifactu.Servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Aserta.Infraestructura.Facturacion;

/// <summary>Persistencia del modulo Veri*Factu sobre EF Core. Las tablas inmutables solo reciben Add (RD-06).</summary>
public sealed class RepositorioFacturacion : IRepositorioFacturacion
{
    private readonly AsertaDbContext _db;
    private readonly ContextoEjecucion _contexto;

    public RepositorioFacturacion(AsertaDbContext db, ContextoEjecucion contexto)
    {
        _db = db;
        _contexto = contexto;
    }

    public async Task<EmisorFacturacion?> EmisorAsync(Guid clienteEmisorId, CancellationToken ct = default)
    {
        var c = await _db.Clientes.AsNoTracking().Include(x => x.Perfiles).FirstOrDefaultAsync(x => x.Id == clienteEmisorId, ct);
        if (c is null) return null;
        var direccion = string.Join(", ", new[] { c.DireccionCalle, c.DireccionCodigoPostal, c.DireccionMunicipio, c.DireccionProvincia }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return new EmisorFacturacion(c.Id, c.GestoriaId, c.Nif, c.RazonSocial, direccion, c.PerfilActual?.Territorio == Territorio.Foral, c.FormaJuridica.ToString());
    }

    public async Task<SerieFacturacion> SerieAsync(Guid clienteEmisorId, string codigo, int ejercicio, CancellationToken ct = default)
    {
        var serie = await _db.SeriesFacturacion.FirstOrDefaultAsync(s => s.ClienteEmisorId == clienteEmisorId && s.Codigo == codigo && s.Ejercicio == ejercicio, ct);
        if (serie is not null) return serie;
        var cliente = await _db.Clientes.AsNoTracking().FirstAsync(c => c.Id == clienteEmisorId, ct);
        serie = new SerieFacturacion { Id = Guid.NewGuid(), GestoriaId = cliente.GestoriaId, ClienteEmisorId = clienteEmisorId, Codigo = codigo, Ejercicio = (short)ejercicio, Descripcion = codigo switch { "R" => "Rectificativas", "T" => "Simplificadas", _ => "General" } };
        _db.SeriesFacturacion.Add(serie);
        // La fila de CadenaEmisor se crea al dar de alta la primera serie, nunca en la emision (04-modelo-datos.md §6.4)
        if (!await _db.CadenasEmisor.AnyAsync(x => x.NifEmisor == cliente.Nif, ct))
            _db.CadenasEmisor.Add(new CadenaEmisor { NifEmisor = cliente.Nif, GestoriaId = cliente.GestoriaId, ClienteEmisorId = clienteEmisorId });
        await _db.SaveChangesAsync(ct);
        return serie;
    }

    public async Task<ITransaccionEmision> IniciarEmisionAsync(EmisorFacturacion emisor, Guid serieId, CancellationToken ct = default)
    {
        var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        // Bloqueo de la fila del emisor: dos emisiones simultaneas del mismo NIF se serializan (RD-05)
        var cadena = await _db.CadenasEmisor.FromSqlRaw("SELECT * FROM vf.CadenaEmisor WITH (UPDLOCK, HOLDLOCK) WHERE NifEmisor = {0}", emisor.Nif).FirstOrDefaultAsync(ct);
        if (cadena is null)
        {
            await tx.RollbackAsync(ct);
            throw new ExcepcionFacturacion("El emisor no tiene cadena de facturación: dé de alta una serie primero.");
        }
        var serie = await _db.SeriesFacturacion.FromSqlRaw("SELECT * FROM vf.SerieFacturacion WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", serieId).FirstAsync(ct);
        // Si las entidades ya estaban rastreadas antes del bloqueo, EF devuelve la instancia antigua: se recargan con los valores actuales (ya bajo el bloqueo)
        await _db.Entry(cadena).ReloadAsync(ct);
        await _db.Entry(serie).ReloadAsync(ct);
        return new TransaccionEmision(_db, tx, cadena, serie);
    }

    private sealed class TransaccionEmision(AsertaDbContext db, IDbContextTransaction tx, CadenaEmisor cadena, SerieFacturacion serie) : ITransaccionEmision
    {
        private bool _confirmada;
        private RegistroFacturacion? _registro;
        private EstadoEnvioRegistro? _estado;
        private EnvioPendiente? _pendiente;
        public CadenaEmisor Cadena => cadena;
        public SerieFacturacion Serie => serie;
        public void Agregar(FacturaEmitida factura) => db.FacturasEmitidas.Add(factura);
        public void Agregar(RegistroFacturacion registro) => _registro = registro;
        public void Agregar(EstadoEnvioRegistro estado) => _estado = estado;
        public void Agregar(EnvioPendiente pendiente) => _pendiente = pendiente;
        public void Agregar(FacturaPdf pdf) => db.FacturasPdf.Add(pdf);

        public async Task<long> ConfirmarAsync(CancellationToken ct = default)
        {
            if (_registro is null) throw new InvalidOperationException("Falta el registro de facturacion.");
            db.RegistrosFacturacion.Add(_registro);
            await db.SaveChangesAsync(ct);   // obtiene el Id IDENTITY del registro
            if (_estado is not null) { _estado.RegistroFacturacionId = _registro.Id; db.EstadosEnvio.Add(_estado); }
            if (_pendiente is not null) { _pendiente.RegistroFacturacionId = _registro.Id; db.EnviosPendientes.Add(_pendiente); }
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            _confirmada = true;
            return _registro.Id;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_confirmada) { try { await tx.RollbackAsync(); } catch { } }
            await tx.DisposeAsync();
        }
    }

    public Task<FacturaEmitida?> FacturaAsync(Guid id, CancellationToken ct = default) =>
        _db.FacturasEmitidas.AsNoTracking().Include(f => f.Lineas).FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<RegistroFacturacion>> RegistrosDeFacturaAsync(Guid facturaId, CancellationToken ct = default) =>
        await _db.RegistrosFacturacion.AsNoTracking().Where(r => r.FacturaEmitidaId == facturaId).OrderBy(r => r.NumeroEnCadena).ToListAsync(ct);

    public Task<EstadoEnvioRegistro?> EstadoEnvioAsync(long registroId, CancellationToken ct = default) =>
        _db.EstadosEnvio.FirstOrDefaultAsync(e => e.RegistroFacturacionId == registroId, ct);

    public Task<bool> TieneAnulacionAsync(Guid facturaId, CancellationToken ct = default) =>
        _db.RegistrosFacturacion.AnyAsync(r => r.FacturaEmitidaId == facturaId && r.Tipo == TipoRegistro.ANULACION, ct);

    public Task<FacturaPdf?> PdfAsync(Guid facturaId, CancellationToken ct = default) => _db.FacturasPdf.FirstOrDefaultAsync(p => p.FacturaEmitidaId == facturaId, ct);

    public Task ActualizarPdfAsync(FacturaPdf pdf, CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<EmisorConPendientes>> EmisoresConPendientesAsync(DateTime ahoraUtc, CancellationToken ct = default) =>
        await _db.EnviosPendientes.AsNoTracking()
            .Where(p => p.Estado == EstadoEnvioPendiente.PENDIENTE && p.ProximoIntentoUtc <= ahoraUtc)
            .GroupBy(p => new { p.GestoriaId, p.NifEmisor })
            .Select(g => new { g.Key.GestoriaId, g.Key.NifEmisor, N = g.Count() })
            .Join(_db.CadenasEmisor.AsNoTracking(), p => p.NifEmisor, c => c.NifEmisor, (p, c) => new EmisorConPendientes(p.GestoriaId, c.ClienteEmisorId, p.NifEmisor, p.N))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TrabajoEnvio>> TomarPendientesAsync(string nifEmisor, int maximo, DateTime ahoraUtc, CancellationToken ct = default)
    {
        // READPAST salta lo que otro proceso este trabajando; UPDLOCK reserva lo que tomamos (ADR-001 §2.5)
        var filas = await _db.EnviosPendientes
            .FromSqlRaw("SELECT TOP ({0}) * FROM vf.EnvioPendiente WITH (READPAST, UPDLOCK, ROWLOCK) WHERE Estado = 'PENDIENTE' AND ProximoIntentoUtc <= {1} AND NifEmisor = {2} ORDER BY Id", maximo, ahoraUtc, nifEmisor)
            .ToListAsync(ct);
        if (filas.Count == 0) return [];
        foreach (var f in filas) { f.Estado = EstadoEnvioPendiente.EN_CURSO; f.TomadoUtc = ahoraUtc; }
        await _db.SaveChangesAsync(ct);

        var ids = filas.Select(f => f.RegistroFacturacionId).ToList();
        var registros = await _db.RegistrosFacturacion.AsNoTracking().Where(r => ids.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct);
        var estados = await _db.EstadosEnvio.Where(e => ids.Contains(e.RegistroFacturacionId)).ToDictionaryAsync(e => e.RegistroFacturacionId, ct);
        return filas.Where(f => registros.ContainsKey(f.RegistroFacturacionId) && estados.ContainsKey(f.RegistroFacturacionId))
            .Select(f => new TrabajoEnvio(f, registros[f.RegistroFacturacionId], estados[f.RegistroFacturacionId])).ToList();
    }

    public Task<CadenaEmisor?> CadenaAsync(string nifEmisor, CancellationToken ct = default) => _db.CadenasEmisor.FirstOrDefaultAsync(c => c.NifEmisor == nifEmisor, ct);

    public async Task<long> RegistrarLoteAsync(LoteEnvio lote, CancellationToken ct = default)
    {
        _db.LotesEnvio.Add(lote);
        await _db.SaveChangesAsync(ct);
        return lote.Id;
    }

    public async Task<int> LiberarTomadasCaducadasAsync(TimeSpan caducidad, DateTime ahoraUtc, CancellationToken ct = default)
    {
        var limite = ahoraUtc - caducidad;
        return await _db.EnviosPendientes.Where(p => p.Estado == EstadoEnvioPendiente.EN_CURSO && p.TomadoUtc != null && p.TomadoUtc < limite)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, EstadoEnvioPendiente.PENDIENTE).SetProperty(p => p.TomadoUtc, (DateTime?)null), ct);
    }

    public async Task<int> AdelantarPendientesAsync(DateTime ahoraUtc, CancellationToken ct = default)
    {
        await _db.CadenasEmisor.Where(c => c.ProximoEnvioPermitidoUtc != null && c.ProximoEnvioPermitidoUtc > ahoraUtc).ExecuteUpdateAsync(s => s.SetProperty(c => c.ProximoEnvioPermitidoUtc, (DateTime?)null), ct);
        return await _db.EnviosPendientes.Where(p => p.Estado == EstadoEnvioPendiente.PENDIENTE && p.ProximoIntentoUtc > ahoraUtc).ExecuteUpdateAsync(s => s.SetProperty(p => p.ProximoIntentoUtc, ahoraUtc), ct);
    }

    public Task<EnvioPendiente?> EnvioPendienteDeRegistroAsync(long registroId, CancellationToken ct = default) =>
        _db.EnviosPendientes.OrderByDescending(p => p.Id).FirstOrDefaultAsync(p => p.RegistroFacturacionId == registroId, ct);

    public async Task CrearEnvioPendienteAsync(long registroId, CancellationToken ct = default)
    {
        var r = await _db.RegistrosFacturacion.AsNoTracking().FirstAsync(x => x.Id == registroId, ct);
        _db.EnviosPendientes.Add(new EnvioPendiente { RegistroFacturacionId = r.Id, GestoriaId = r.GestoriaId, NifEmisor = r.NifEmisor, FechaAltaUtc = DateTime.UtcNow, ProximoIntentoUtc = DateTime.UtcNow, Estado = EstadoEnvioPendiente.PENDIENTE });
    }

    public Task<RegistroFacturacion?> RegistroPorNumeroAsync(string nifEmisor, long numeroEnCadena, CancellationToken ct = default) =>
        _db.RegistrosFacturacion.AsNoTracking().FirstOrDefaultAsync(r => r.NifEmisor == nifEmisor && r.NumeroEnCadena == numeroEnCadena, ct);

    public Task GuardarAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
