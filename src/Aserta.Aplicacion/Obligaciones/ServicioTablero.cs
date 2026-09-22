using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Nucleo;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Aplicacion.Obligaciones;

/// <summary>Filtro comun del Kanban, el calendario y la carga por asesor.</summary>
public sealed class FiltroTablero
{
    public int Ejercicio { get; set; }
    public Guid? AsesorId { get; set; }
    public string? Modelo { get; set; }
    public string? Texto { get; set; }
    public bool SoloConIngreso { get; set; }
}

public sealed record TarjetaObligacion(Obligacion Obligacion, string Cliente, string Asesor, string IniciativasAsesor, bool PlazoConfirmado);

public sealed record CargaAsesor(Guid? AsesorId, string Nombre, IReadOnlyDictionary<EstadoObligacion, int> PorEstado, int Abiertas, int VencenEn7, int Vencidas, int Clientes);

/// <summary>Consultas de lectura para el tablero (M4). Solo lectura: no muta nada.</summary>
public sealed class ServicioTablero
{
    private readonly IAsertaDb _db;
    private readonly ServicioClientes _clientes;
    private readonly ServicioUsuarios _usuarios;
    private readonly IRelojSistema _reloj;

    public ServicioTablero(IAsertaDb db, ServicioClientes clientes, ServicioUsuarios usuarios, IRelojSistema reloj)
    {
        _db = db;
        _clientes = clientes;
        _usuarios = usuarios;
        _reloj = reloj;
    }

    public DateOnly Hoy => _reloj.Hoy;

    /// <summary>Obligaciones visibles con su cliente y asesor, aplicando RD-07 y el filtro.</summary>
    public async Task<List<TarjetaObligacion>> TarjetasAsync(FiltroTablero filtro, bool incluirFinales = true, CancellationToken ct = default)
    {
        var visibles = await _clientes.ConsultaVisiblesAsync(ct);
        var clientes = await visibles.ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? c.RazonSocial, ct);
        var asesores = (await _usuarios.ListarDeGestoriaAsync(ct)).ToDictionary(u => u.Id, u => u.NombreCompleto);
        var confirmados = (await _db.PlazosModelo.AsNoTracking().Where(p => p.Ejercicio == filtro.Ejercicio && p.Confirmado).Select(p => p.ModeloCodigo + "|" + p.Periodo).ToListAsync(ct)).ToHashSet();

        var ids = clientes.Keys.ToList();
        var q = _db.Obligaciones.AsNoTracking().Where(o => o.Ejercicio == filtro.Ejercicio && ids.Contains(o.ClienteId) && o.Estado != EstadoObligacion.NoAplica);
        if (!incluirFinales) q = q.Where(o => o.Estado != EstadoObligacion.Cerrado);
        if (filtro.AsesorId is Guid a) q = q.Where(o => o.AsesorId == a);
        if (!string.IsNullOrWhiteSpace(filtro.Modelo)) q = q.Where(o => o.ModeloCodigo == filtro.Modelo.Trim());
        if (filtro.SoloConIngreso) q = q.Where(o => o.FechaLimiteDomiciliacion != null);

        var lista = await q.ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var t = filtro.Texto.Trim();
            var idsTexto = clientes.Where(kv => kv.Value.Contains(t, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Key).ToHashSet();
            lista = lista.Where(o => idsTexto.Contains(o.ClienteId) || o.ModeloCodigo.Contains(t, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return lista
            .OrderBy(o => o.OrdenEnColumna).ThenBy(Semaforo.FechaDeReferencia).ThenBy(o => o.ModeloCodigo)
            .Select(o => new TarjetaObligacion(o, clientes.GetValueOrDefault(o.ClienteId, "—"),
                o.AsesorId is Guid ai ? asesores.GetValueOrDefault(ai, "—") : "Sin asignar",
                Iniciales(o.AsesorId is Guid ai2 ? asesores.GetValueOrDefault(ai2, "") : ""),
                confirmados.Contains(o.ModeloCodigo + "|" + o.Periodo)))
            .ToList();
    }

    public async Task<TarjetaObligacion> TarjetaAsync(Guid id, CancellationToken ct = default)
    {
        var o = await _db.Obligaciones.AsNoTracking().Include(x => x.Historial).FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new Comun.ExcepcionNoEncontrado("Obligación", id);
        var cliente = await _db.Clientes.AsNoTracking().Where(c => c.Id == o.ClienteId).Select(c => c.NombreComercial ?? c.RazonSocial).FirstAsync(ct);
        var asesor = o.AsesorId is Guid a ? await _db.Usuarios.AsNoTracking().Where(u => u.Id == a).Select(u => u.NombreCompleto).FirstOrDefaultAsync(ct) ?? "—" : "Sin asignar";
        var confirmado = await _db.PlazosModelo.AsNoTracking().AnyAsync(p => p.ModeloCodigo == o.ModeloCodigo && p.Ejercicio == o.Ejercicio && p.Periodo == o.Periodo && p.Confirmado, ct);
        return new TarjetaObligacion(o, cliente, asesor, Iniciales(asesor), confirmado);
    }

    /// <summary>Obligaciones abiertas cuya fecha de referencia cae en el intervalo (para el calendario).</summary>
    public async Task<List<TarjetaObligacion>> EntreFechasAsync(DateOnly desde, DateOnly hasta, Guid? asesorId, CancellationToken ct = default)
    {
        var visibles = await _clientes.ConsultaVisiblesAsync(ct);
        var clientes = await visibles.ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? c.RazonSocial, ct);
        var asesores = (await _usuarios.ListarDeGestoriaAsync(ct)).ToDictionary(u => u.Id, u => u.NombreCompleto);
        var ids = clientes.Keys.ToList();

        var q = _db.Obligaciones.AsNoTracking().Where(o => ids.Contains(o.ClienteId) && o.Estado != EstadoObligacion.NoAplica
            && ((o.FechaLimiteDomiciliacion != null && o.FechaLimiteDomiciliacion >= desde && o.FechaLimiteDomiciliacion <= hasta)
             || (o.FechaLimiteDomiciliacion == null && o.FechaLimitePresentacion >= desde && o.FechaLimitePresentacion <= hasta)));
        if (asesorId is Guid a) q = q.Where(o => o.AsesorId == a);

        return (await q.ToListAsync(ct))
            .OrderBy(Semaforo.FechaDeReferencia).ThenBy(o => o.ModeloCodigo)
            .Select(o => new TarjetaObligacion(o, clientes.GetValueOrDefault(o.ClienteId, "—"),
                o.AsesorId is Guid ai ? asesores.GetValueOrDefault(ai, "—") : "Sin asignar",
                Iniciales(o.AsesorId is Guid ai2 ? asesores.GetValueOrDefault(ai2, "") : ""), false))
            .ToList();
    }

    /// <summary>Carga de trabajo por asesor para el ejercicio: cuenta por estado, vencimientos y clientes asignados.</summary>
    public async Task<List<CargaAsesor>> CargaPorAsesorAsync(int ejercicio, CancellationToken ct = default)
    {
        var hoy = Hoy;
        var asesores = await _usuarios.ListarDeGestoriaAsync(ct);
        var obligaciones = await _db.Obligaciones.AsNoTracking().Where(o => o.Ejercicio == ejercicio && o.Estado != EstadoObligacion.NoAplica).ToListAsync(ct);
        var clientesPorAsesor = await _db.Clientes.AsNoTracking().Where(c => c.Estado == Dominio.Clientes.EstadoCliente.Activo)
            .GroupBy(c => c.AsesorResponsableId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);

        var filas = new List<CargaAsesor>();
        foreach (var grupo in obligaciones.GroupBy(o => o.AsesorId).OrderBy(g => g.Key is null))
        {
            var nombre = grupo.Key is Guid a ? asesores.FirstOrDefault(u => u.Id == a)?.NombreCompleto ?? "—" : "Sin asignar";
            var porEstado = EstadoObligacionExtensiones.ColumnasKanban.ToDictionary(e => e, e => grupo.Count(o => o.Estado == e));
            var abiertas = grupo.Where(o => !o.Estado.EsFinalOPresentado()).ToList();
            filas.Add(new CargaAsesor(grupo.Key, nombre, porEstado, abiertas.Count,
                abiertas.Count(o => Semaforo.DiasRestantes(o, hoy) is >= 0 and <= 7),
                abiertas.Count(o => Semaforo.DiasRestantes(o, hoy) < 0),
                grupo.Key is Guid a2 ? clientesPorAsesor.GetValueOrDefault(a2) : 0));
        }
        // Asesores sin obligaciones tambien aparecen (carga cero)
        foreach (var u in asesores.Where(u => u.Estado == EstadoUsuario.Activo && filas.All(f => f.AsesorId != u.Id)))
            filas.Add(new CargaAsesor(u.Id, u.NombreCompleto, EstadoObligacionExtensiones.ColumnasKanban.ToDictionary(e => e, _ => 0), 0, 0, 0, clientesPorAsesor.GetValueOrDefault(u.Id)));
        return filas.OrderByDescending(f => f.Abiertas).ToList();
    }

    public static string Iniciales(string nombre)
    {
        var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length == 0 ? "—" : string.Concat(partes.Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }
}
