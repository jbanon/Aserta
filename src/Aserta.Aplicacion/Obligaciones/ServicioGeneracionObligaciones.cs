using Aserta.Aplicacion.Comun;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aserta.Aplicacion.Obligaciones;

/// <summary>
/// Orquesta el motor de obligaciones: carga catalogo + cliente + obligaciones
/// existentes, ejecuta el motor (puro) y persiste el resultado. Reentrante.
/// </summary>
public sealed class ServicioGeneracionObligaciones
{
    private readonly IAsertaDb _db;
    private readonly IRelojSistema _reloj;
    private readonly IRegistroAuditoria _auditoria;
    private readonly Documental.ServicioRequisitos _requisitos;
    private readonly ILogger<ServicioGeneracionObligaciones> _log;

    public ServicioGeneracionObligaciones(IAsertaDb db, IRelojSistema reloj, IRegistroAuditoria auditoria, Documental.ServicioRequisitos requisitos, ILogger<ServicioGeneracionObligaciones> log)
    {
        _db = db;
        _reloj = reloj;
        _auditoria = auditoria;
        _requisitos = requisitos;
        _log = log;
    }

    /// <summary>Ejercicios que se generan por defecto: el actual y el siguiente (la demo funciona sobre 2026 y 2027).</summary>
    public IReadOnlyList<int> EjerciciosPorDefecto()
    {
        int actual = _reloj.Hoy.Year;
        return [actual, actual + 1];
    }

    public async Task<MotorObligaciones> CargarMotorAsync(IReadOnlyCollection<int> ejercicios, CancellationToken ct = default)
    {
        var reglas = await _db.ReglasObligacion.AsNoTracking().Include(r => r.Condiciones).Where(r => r.Activa).ToListAsync(ct);
        var plazos = await _db.PlazosModelo.AsNoTracking().Where(p => ejercicios.Contains(p.Ejercicio)).ToListAsync(ct);
        return new MotorObligaciones(reglas, plazos);
    }

    public async Task<IReadOnlyList<ResultadoMotor>> GenerarParaClienteAsync(Guid clienteId, IReadOnlyCollection<int>? ejercicios = null, CancellationToken ct = default)
    {
        ejercicios ??= EjerciciosPorDefecto();
        var motor = await CargarMotorAsync(ejercicios, ct);
        var cliente = await _db.Clientes.Include(c => c.Perfiles).FirstOrDefaultAsync(c => c.Id == clienteId, ct)
                      ?? throw new ExcepcionNoEncontrado("Cliente", clienteId);
        var resultados = await GenerarAsync(motor, cliente, ejercicios, ct);
        await _db.GuardarCambiosAsync(ct);
        return resultados;
    }

    /// <summary>Reejecuta el motor para todos los clientes de la gestoria (p. ej. tras actualizar el catalogo).</summary>
    public async Task<IReadOnlyList<ResultadoMotor>> GenerarParaTodosAsync(IReadOnlyCollection<int>? ejercicios = null, CancellationToken ct = default)
    {
        ejercicios ??= EjerciciosPorDefecto();
        var motor = await CargarMotorAsync(ejercicios, ct);
        var clientes = await _db.Clientes.Include(c => c.Perfiles).ToListAsync(ct);
        var todos = new List<ResultadoMotor>();
        foreach (var cliente in clientes)
            todos.AddRange(await GenerarAsync(motor, cliente, ejercicios, ct));
        await _db.GuardarCambiosAsync(ct);
        return todos;
    }

    private async Task<List<ResultadoMotor>> GenerarAsync(MotorObligaciones motor, Cliente cliente, IReadOnlyCollection<int> ejercicios, CancellationToken ct)
    {
        var existentes = await _db.Obligaciones
            .Where(o => o.ClienteId == cliente.Id && ejercicios.Contains(o.Ejercicio))
            .ToListAsync(ct);

        // Los requisitos documentales se derivan junto con las obligaciones (04-modelo-datos.md §5.4)
        await _requisitos.GenerarParaClienteAsync(cliente, ejercicios, ct);

        var resultados = new List<ResultadoMotor>();
        foreach (var ejercicio in ejercicios.OrderBy(e => e))
        {
            var r = motor.Evaluar(cliente, ejercicio, existentes, _reloj.AhoraUtc);
            foreach (var nueva in r.Nuevas) _db.Obligaciones.Add(nueva);
            // Las obligaciones existentes ya estan rastreadas: sus cambios y su historial nuevo se guardan solos.
            resultados.Add(r);

            if (r.HuboCambios || r.Avisos.Count > 0)
            {
                _auditoria.Registrar(AccionesAuditoria.GeneracionObligaciones, nameof(Cliente), cliente.Id.ToString(),
                    new { ejercicio, resumen = r.Resumen, avisos = r.Avisos });
                _log.LogInformation("Motor de obligaciones · cliente {Cliente} · {Ejercicio}: {Resumen}", cliente.RazonSocial, ejercicio, r.Resumen);
            }
        }
        return resultados;
    }
}
