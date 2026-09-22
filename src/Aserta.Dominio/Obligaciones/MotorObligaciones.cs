using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;

namespace Aserta.Dominio.Obligaciones;

/// <summary>
/// RD-01: las obligaciones se DERIVAN del perfil fiscal evaluando las reglas del
/// catalogo (filas, no codigo). Reentrante e idempotente (riesgo D1):
///  - genera lo que falte,
///  - marca NoAplica lo que sobre,
///  - reactiva lo NoAplica que vuelva a proceder,
///  - actualiza plazos si el catalogo cambio,
///  - NUNCA toca obligaciones presentadas ni cerradas.
/// Es logica pura: sin base de datos, sin reloj, sin E/S. Se prueba en Aserta.Dominio.Tests.
/// </summary>
public sealed class MotorObligaciones
{
    private readonly IReadOnlyList<ReglaObligacion> _reglas;
    private readonly IReadOnlyDictionary<(string Modelo, int Ejercicio, string Periodo), PlazoModelo> _plazos;

    public MotorObligaciones(IEnumerable<ReglaObligacion> reglas, IEnumerable<PlazoModelo> plazos)
    {
        _reglas = reglas.ToList();
        _plazos = plazos.ToDictionary(p => (p.ModeloCodigo, (int)p.Ejercicio, p.Periodo));
    }

    /// <summary>
    /// Criterio de resolucion del perfil (04-modelo-datos.md §3.2, [VERIFICAR] con
    /// un asesor fiscal): el vigente AL INICIO del periodo. Si el cliente no tenia
    /// perfil entonces (alta a mitad de periodo), se usa el primero que empiece
    /// dentro del periodo. Esta aislado aqui a proposito para poder cambiarlo en
    /// un unico sitio.
    /// </summary>
    public static PerfilFiscal? ResolverPerfil(Cliente cliente, int ejercicio, string periodo)
    {
        var (inicio, fin) = Periodo.Rango(ejercicio, periodo);
        return cliente.PerfilVigenteEn(inicio)
               ?? cliente.Perfiles.Where(p => p.VigenteDesde > inicio && p.VigenteDesde <= fin).OrderBy(p => p.VigenteDesde).FirstOrDefault();
    }

    public ResultadoMotor Evaluar(Cliente cliente, int ejercicio, IReadOnlyCollection<Obligacion> existentes, DateTime ahoraUtc)
    {
        var resultado = new ResultadoMotor(cliente.Id, ejercicio);

        // 1. Que obligaciones DEBERIA tener el cliente este ejercicio
        var esperadas = new Dictionary<(string Modelo, string Periodo), ReglaObligacion>();
        foreach (var regla in _reglas.Where(r => r.VigenteEn(ejercicio)).OrderBy(r => r.Prioridad).ThenBy(r => r.Id))
        {
            foreach (var periodo in Periodo.DePeriodicidad(regla.Periodicidad))
            {
                var clave = (regla.ModeloCodigo, periodo);
                if (esperadas.ContainsKey(clave)) continue; // otra regla de mayor prioridad ya la genero

                var (inicio, fin) = Periodo.Rango(ejercicio, periodo);
                if (!cliente.EstabaDeAltaEn(inicio, fin)) continue; // RD-10

                var perfil = ResolverPerfil(cliente, ejercicio, periodo);
                if (perfil is null) continue;
                if (perfil.Territorio == Territorio.Foral) continue; // DA-14: fuera de alcance, sin obligaciones

                if (regla.SeCumple(perfil.ComoAtributos(cliente.FormaJuridica)))
                    esperadas[clave] = regla;
            }
        }

        // 2. Cruce con lo existente
        var existentesPorClave = existentes
            .Where(o => o.ClienteId == cliente.Id && o.Ejercicio == ejercicio)
            .ToDictionary(o => (o.ModeloCodigo, o.Periodo));

        foreach (var ((modelo, periodo), regla) in esperadas)
        {
            if (!_plazos.TryGetValue((modelo, ejercicio, periodo), out var plazo))
            {
                resultado.Avisos.Add($"Falta el plazo de {modelo} {Periodo.Etiqueta(ejercicio, periodo)} en el catálogo; no se ha generado la obligación.");
                continue;
            }

            if (existentesPorClave.TryGetValue((modelo, periodo), out var existente))
            {
                if (existente.Estado == EstadoObligacion.NoAplica)
                {
                    var anterior = existente.Estado;
                    existente.Estado = EstadoObligacion.PendienteDocumentacion;
                    existente.ReglaOrigenId = regla.Id;
                    existente.Historial.Add(new ObligacionHistorial
                    {
                        GestoriaId = existente.GestoriaId, ObligacionId = existente.Id, FechaUtc = ahoraUtc,
                        TipoEvento = TiposEventoHistorial.Reactivada, EstadoAnterior = anterior, EstadoNuevo = existente.Estado,
                        Comentario = $"Vuelve a proceder según la regla «{regla.Descripcion}»."
                    });
                    resultado.Reactivadas.Add(existente);
                }
                else if (!existente.Estado.EsFinalOPresentado() &&
                         (existente.FechaLimitePresentacion != plazo.FechaLimitePresentacion || existente.FechaLimiteDomiciliacion != plazo.FechaLimiteDomiciliacion))
                {
                    var comentario = $"Plazo actualizado: {existente.FechaLimitePresentacion:dd/MM/yyyy} → {plazo.FechaLimitePresentacion:dd/MM/yyyy}.";
                    existente.FechaLimitePresentacion = plazo.FechaLimitePresentacion;
                    existente.FechaLimiteDomiciliacion = plazo.FechaLimiteDomiciliacion;
                    existente.Historial.Add(new ObligacionHistorial
                    {
                        GestoriaId = existente.GestoriaId, ObligacionId = existente.Id, FechaUtc = ahoraUtc,
                        TipoEvento = TiposEventoHistorial.PlazoActualizado, Comentario = comentario
                    });
                    resultado.PlazosActualizados.Add(existente);
                }
                else
                {
                    resultado.SinCambios.Add(existente);
                }
                continue;
            }

            var nueva = new Obligacion
            {
                Id = Guid.NewGuid(),
                GestoriaId = cliente.GestoriaId,
                ClienteId = cliente.Id,
                ModeloCodigo = modelo,
                Ejercicio = (short)ejercicio,
                Periodo = periodo,
                Estado = EstadoObligacion.PendienteDocumentacion,
                AsesorId = cliente.AsesorResponsableId,
                FechaLimiteDomiciliacion = plazo.FechaLimiteDomiciliacion,
                FechaLimitePresentacion = plazo.FechaLimitePresentacion,
                ReglaOrigenId = regla.Id,
                FechaGeneracionUtc = ahoraUtc,
            };
            nueva.Historial.Add(new ObligacionHistorial
            {
                GestoriaId = nueva.GestoriaId, ObligacionId = nueva.Id, FechaUtc = ahoraUtc,
                TipoEvento = TiposEventoHistorial.Generada, EstadoNuevo = nueva.Estado,
                Comentario = $"Generada por la regla «{regla.Descripcion}»."
            });
            resultado.Nuevas.Add(nueva);
        }

        // 3. Lo que existe y ya no procede => NoAplica (nunca borrar, nunca tocar presentadas/cerradas)
        foreach (var (clave, existente) in existentesPorClave)
        {
            if (esperadas.ContainsKey(clave)) continue;
            if (existente.Estado.EsFinalOPresentado()) continue;

            var anterior = existente.Estado;
            existente.Estado = EstadoObligacion.NoAplica;
            existente.Historial.Add(new ObligacionHistorial
            {
                GestoriaId = existente.GestoriaId, ObligacionId = existente.Id, FechaUtc = ahoraUtc,
                TipoEvento = TiposEventoHistorial.MarcadaNoAplica, EstadoAnterior = anterior, EstadoNuevo = EstadoObligacion.NoAplica,
                Comentario = "Deja de proceder tras reevaluar el perfil fiscal o la situación del cliente."
            });
            resultado.MarcadasNoAplica.Add(existente);
        }

        return resultado;
    }
}

/// <summary>Salida del motor: que se creo, que se marco y que se dejo igual.</summary>
public sealed class ResultadoMotor(Guid clienteId, int ejercicio)
{
    public Guid ClienteId { get; } = clienteId;
    public int Ejercicio { get; } = ejercicio;
    public List<Obligacion> Nuevas { get; } = [];
    public List<Obligacion> MarcadasNoAplica { get; } = [];
    public List<Obligacion> Reactivadas { get; } = [];
    public List<Obligacion> PlazosActualizados { get; } = [];
    public List<Obligacion> SinCambios { get; } = [];
    public List<string> Avisos { get; } = [];

    public bool HuboCambios => Nuevas.Count + MarcadasNoAplica.Count + Reactivadas.Count + PlazosActualizados.Count > 0;

    public string Resumen =>
        $"{Nuevas.Count} nuevas, {MarcadasNoAplica.Count} marcadas como no aplica, {Reactivadas.Count} reactivadas, {PlazosActualizados.Count} plazos actualizados, {SinCambios.Count} sin cambios" +
        (Avisos.Count > 0 ? $", {Avisos.Count} avisos" : string.Empty);
}
