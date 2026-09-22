using Aserta.Aplicacion.Comun;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aserta.Aplicacion.Documental;

/// <summary>
/// Requisitos documentales por cliente y periodo: generacion (motor), ajuste
/// manual, estado ("te faltan N…") y paso automatico a DocumentacionCompleta
/// cuando un periodo queda cubierto (reacciona al evento DocumentoValidado).
/// </summary>
public sealed class ServicioRequisitos : IManejadorEvento<DocumentoValidado>
{
    private readonly IAsertaDb _db;
    private readonly IRelojSistema _reloj;
    private readonly IContextoUsuarioActual _usuario;
    private readonly ILogger<ServicioRequisitos> _log;

    public ServicioRequisitos(IAsertaDb db, IRelojSistema reloj, IContextoUsuarioActual usuario, ILogger<ServicioRequisitos> log)
    {
        _db = db;
        _reloj = reloj;
        _usuario = usuario;
        _log = log;
    }

    public async Task<MotorRequisitos.Resultado> GenerarParaClienteAsync(Cliente cliente, IReadOnlyCollection<int> ejercicios, CancellationToken ct = default)
    {
        var reglas = await _db.ReglasRequisito.AsNoTracking().Include(r => r.Condiciones).Where(r => r.Activa).ToListAsync(ct);
        var motor = new MotorRequisitos(reglas);
        var existentes = await _db.RequisitosPeriodo.Where(r => r.ClienteId == cliente.Id && ejercicios.Contains(r.Ejercicio)).ToListAsync(ct);
        var total = new MotorRequisitos.Resultado();
        foreach (var ej in ejercicios)
        {
            var r = motor.Evaluar(cliente, ej, existentes, _reloj.Hoy);
            foreach (var n in r.Nuevos) _db.RequisitosPeriodo.Add(n);
            total.Nuevos.AddRange(r.Nuevos); total.MarcadosNoAplica.AddRange(r.MarcadosNoAplica); total.SinCambios += r.SinCambios;
        }
        return total;
    }

    /// <summary>Estado documental completo de un cliente (todos los ejercicios cargados).</summary>
    public async Task<List<EstadoRequisito>> EstadoAsync(Guid clienteId, int? ejercicio = null, CancellationToken ct = default)
    {
        if (_usuario.ClienteId is Guid propio && propio != clienteId) throw new ExcepcionNoAutorizado("No es su empresa.");
        var req = _db.RequisitosPeriodo.AsNoTracking().Where(r => r.ClienteId == clienteId);
        var docs = _db.Documentos.AsNoTracking().Where(d => d.ClienteId == clienteId);
        if (ejercicio is int e) { req = req.Where(r => r.Ejercicio == e); docs = docs.Where(d => d.Ejercicio == e); }
        return CompletitudDocumental.Evaluar(await req.ToListAsync(ct), await docs.ToListAsync(ct));
    }

    public async Task AjustarAsync(Guid requisitoId, int? cantidadEsperada, bool obligatorio, bool noAplica, CancellationToken ct = default)
    {
        if (_usuario.ClienteId is not null) throw new ExcepcionNoAutorizado("Solo la gestoría ajusta requisitos.");
        var r = await _db.RequisitosPeriodo.FirstOrDefaultAsync(x => x.Id == requisitoId, ct) ?? throw new ExcepcionNoEncontrado("Requisito", requisitoId);
        if (cantidadEsperada is < 0 or > 999) throw new ExcepcionValidacion("CantidadEsperada", "Cantidad entre 0 y 999 (vacío = al menos uno).");
        r.CantidadEsperada = cantidadEsperada; r.Obligatorio = obligatorio; r.NoAplica = noAplica;
        r.ReglaOrigenId = null; // ajustado a mano: el motor ya no lo toca
        await _db.GuardarCambiosAsync(ct);
    }

    public async Task AnadirAsync(Guid clienteId, int ejercicio, string periodo, TipoDocumento tipo, int? cantidad, string descripcion, CancellationToken ct = default)
    {
        if (_usuario.ClienteId is not null) throw new ExcepcionNoAutorizado("Solo la gestoría ajusta requisitos.");
        if (!Periodo.EsValido(periodo)) throw new ExcepcionValidacion("Periodo", "Periodo no válido.");
        if (await _db.RequisitosPeriodo.AnyAsync(r => r.ClienteId == clienteId && r.Ejercicio == ejercicio && r.Periodo == periodo && r.TipoDocumento == tipo, ct))
            throw new ExcepcionValidacion("TipoDocumento", "Ya existe ese requisito para el periodo.");
        var gestoriaId = _usuario.GestoriaId!.Value;
        _db.RequisitosPeriodo.Add(new RequisitoPeriodo { Id = Guid.NewGuid(), GestoriaId = gestoriaId, ClienteId = clienteId, Ejercicio = (short)ejercicio, Periodo = periodo, TipoDocumento = tipo, CantidadEsperada = cantidad, Obligatorio = true, Descripcion = string.IsNullOrWhiteSpace(descripcion) ? tipo.Etiqueta() : descripcion.Trim(), ReglaOrigenId = null });
        await _db.GuardarCambiosAsync(ct);
    }

    /// <summary>Evento DocumentoValidado: si el periodo queda cubierto, las obligaciones PendienteDocumentacion que lo contienen pasan a DocumentacionCompleta.</summary>
    public async Task ManejarAsync(DocumentoValidado e, CancellationToken ct)
    {
        var estados = await EstadoAsync(e.ClienteId, e.Ejercicio, ct);
        var obligaciones = await _db.Obligaciones.Include(o => o.Historial).Where(o => o.ClienteId == e.ClienteId && o.Ejercicio == e.Ejercicio && o.Estado == EstadoObligacion.PendienteDocumentacion).ToListAsync(ct);
        var (di, df) = Periodo.Rango(e.Ejercicio, e.Periodo);
        foreach (var o in obligaciones)
        {
            var (oi, of) = Periodo.Rango(o.Ejercicio, o.Periodo);
            if (o.Periodo == "AN" || oi > di || of < df) continue;   // la obligacion debe contener el periodo del documento
            if (!CompletitudDocumental.PeriodoCompleto(estados, o.Ejercicio, o.Periodo)) continue;
            o.CambiarEstado(EstadoObligacion.DocumentacionCompleta, true, _usuario.UsuarioId, _reloj.AhoraUtc, "Todos los requisitos documentales del periodo están cubiertos.");
            _log.LogInformation("Obligación {Titulo} pasa a Documentación completa", o.Titulo);
        }
    }
}

/// <summary>Tarea diaria ReclamacionDocumentalWorker (ADR-001 §2.5): avisa al cliente de lo que le falta cuando una obligacion abierta vence en 15 dias.</summary>
public sealed class ServicioReclamacionDocumental : ITareaDiaria
{
    private readonly IAsertaDb _db;
    private readonly ServicioRequisitos _requisitos;
    private readonly INotificador _notificador;
    private readonly IRelojSistema _reloj;

    public ServicioReclamacionDocumental(IAsertaDb db, ServicioRequisitos requisitos, INotificador notificador, IRelojSistema reloj)
    {
        _db = db;
        _requisitos = requisitos;
        _notificador = notificador;
        _reloj = reloj;
    }

    public string Nombre => "ReclamacionDocumental";

    public async Task EjecutarParaTenantAsync(CancellationToken ct) => await GenerarAsync(ct);

    public async Task<int> GenerarAsync(CancellationToken ct = default)
    {
        var hoy = _reloj.Hoy;
        var proximas = await _db.Obligaciones.AsNoTracking()
            .Where(o => o.Estado == EstadoObligacion.PendienteDocumentacion && (o.FechaLimiteDomiciliacion ?? o.FechaLimitePresentacion) <= hoy.AddDays(15) && o.Periodo != "AN")
            .ToListAsync(ct);
        int n = 0;
        foreach (var grupo in proximas.GroupBy(o => o.ClienteId))
        {
            var usuariosCliente = await _db.Usuarios.AsNoTracking().Where(u => u.ClienteId == grupo.Key && u.Estado == EstadoUsuario.Activo).Select(u => u.Id).ToListAsync(ct);
            if (usuariosCliente.Count == 0) continue;
            var estados = await _requisitos.EstadoAsync(grupo.Key, null, ct);
            foreach (var o in grupo)
            {
                var (oi, of) = Periodo.Rango(o.Ejercicio, o.Periodo);
                var faltan = estados.Where(e => !e.Completo && e.Requisito.Obligatorio && e.Requisito.Ejercicio == o.Ejercicio && Periodo.Rango(e.Requisito.Ejercicio, e.Requisito.Periodo).Inicio >= oi && Periodo.Rango(e.Requisito.Ejercicio, e.Requisito.Periodo).Fin <= of).ToList();
                if (faltan.Count == 0) continue;
                var cuerpo = string.Join(" · ", faltan.Take(4).Select(f => f.Frase));
                foreach (var u in usuariosCliente)
                    if (await _notificador.NotificarAsync(new Notificacion(u, TiposAviso.DocumentacionRecibida, $"Para el {o.ModeloCodigo} de {o.EtiquetaPeriodo} nos falta documentación", cuerpo, nameof(Obligacion), o.Id.ToString(), $"reclamacion:{o.Id}:{hoy:yyyyMMdd}"), ct)) n++;
            }
        }
        if (n > 0) await _db.GuardarCambiosAsync(ct);
        return n;
    }
}
