using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Obligaciones;

namespace Aserta.Dominio.Documental;

/// <summary>
/// Deriva los RequisitoPeriodo de un cliente para un ejercicio a partir de las
/// reglas del catalogo y el perfil fiscal (misma filosofia que MotorObligaciones:
/// reentrante, idempotente, y respeta los ajustes manuales de la gestoria).
/// </summary>
public sealed class MotorRequisitos
{
    private readonly IReadOnlyList<ReglaRequisito> _reglas;

    public MotorRequisitos(IEnumerable<ReglaRequisito> reglas) => _reglas = reglas.ToList();

    public sealed class Resultado
    {
        public List<RequisitoPeriodo> Nuevos { get; } = [];
        public List<RequisitoPeriodo> MarcadosNoAplica { get; } = [];
        public int SinCambios { get; set; }
    }

    public Resultado Evaluar(Cliente cliente, int ejercicio, IReadOnlyCollection<RequisitoPeriodo> existentes, DateOnly hoy)
    {
        var r = new Resultado();
        var esperados = new Dictionary<(string Periodo, TipoDocumento Tipo), ReglaRequisito>();

        foreach (var regla in _reglas.Where(x => x.Activa).OrderBy(x => x.Prioridad).ThenBy(x => x.Id))
        {
            foreach (var periodo in Periodo.DePeriodicidad(regla.Periodicidad))
            {
                var (inicio, fin) = Periodo.Rango(ejercicio, periodo);
                if (fin >= hoy) continue;                         // solo se reclama documentacion de periodos ya terminados
                if (!cliente.EstabaDeAltaEn(inicio, fin)) continue;
                var perfil = MotorObligaciones.ResolverPerfil(cliente, ejercicio, periodo);
                if (perfil is null || perfil.Territorio == Territorio.Foral) continue;
                if (!regla.SeCumple(perfil.ComoAtributos(cliente.FormaJuridica))) continue;
                esperados.TryAdd((periodo, regla.TipoDocumento), regla);
            }
        }

        var porClave = existentes.Where(e => e.ClienteId == cliente.Id && e.Ejercicio == ejercicio).ToDictionary(e => (e.Periodo, e.TipoDocumento));

        foreach (var ((periodo, tipo), regla) in esperados)
        {
            if (porClave.TryGetValue((periodo, tipo), out var existente))
            {
                if (existente.NoAplica && existente.ReglaOrigenId is not null) { existente.NoAplica = false; }
                else r.SinCambios++;
                continue; // los ajustes manuales (cantidad, obligatorio) se respetan
            }
            int meses = Periodo.DePeriodicidad(regla.Periodicidad) == Periodo.Meses ? 1 : (Periodo.Rango(ejercicio, periodo).Fin.Month - Periodo.Rango(ejercicio, periodo).Inicio.Month + 1);
            r.Nuevos.Add(new RequisitoPeriodo
            {
                Id = Guid.NewGuid(), GestoriaId = cliente.GestoriaId, ClienteId = cliente.Id, Ejercicio = (short)ejercicio, Periodo = periodo,
                TipoDocumento = tipo, CantidadEsperada = regla.CantidadPorMes is int c ? c * meses : null, Obligatorio = regla.Obligatorio,
                Descripcion = regla.Descripcion, ReglaOrigenId = regla.Id,
            });
        }

        foreach (var (clave, existente) in porClave)
        {
            if (esperados.ContainsKey(clave) || existente.ReglaOrigenId is null || existente.NoAplica) continue;
            existente.NoAplica = true;
            r.MarcadosNoAplica.Add(existente);
        }
        return r;
    }
}
