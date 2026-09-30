using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Servicios;

public sealed record ResumenCliente(
    Cliente Cliente, short Ejercicio, string Periodo, Plazo? Plazo303, int DiasParaElPlazo,
    IReadOnlyList<Obligacion> ObligacionesTrimestre, int Presentadas, int Aplicables,
    IReadOnlyList<RequisitoDocumental> Requisitos, string FraseDocumentos, int DocsRecibidos, int DocsEsperados,
    ResultadoIva? Iva, Obligacion? Obligacion303, EstadoFraccionamiento Fraccionamiento, bool PuedeFraccionar,
    IReadOnlyList<FacturaEmitida> FacturasTrimestre, int AvisosSinLeer, int MensajesSinLeer, IReadOnlyList<Aviso> UltimosAvisos)
{
    /// <summary>Avance del trimestre de 0 a 100 para el anillo: mezcla documentos entregados y modelos presentados.</summary>
    public int Avance
    {
        get
        {
            double docs = DocsEsperados == 0 ? 1 : (double)DocsRecibidos / DocsEsperados;
            double pres = Aplicables == 0 ? 1 : (double)Presentadas / Aplicables;
            return (int)Math.Round(100 * (DocsEsperados == 0 ? pres : 0.5 * docs + 0.5 * pres));
        }
    }
    public string Semaforo => Presentadas == Aplicables ? "verde" : DiasParaElPlazo < 0 ? "rojo" : DiasParaElPlazo <= 5 ? "ambar" : DocsEsperados > 0 && DocsRecibidos < DocsEsperados ? "ambar" : "verde";
}

/// <summary>Todo lo que el cliente ve en su inicio, calculado en una sola pasada.</summary>
public sealed class ServicioCliente(SgaDb db, IReloj reloj, ServicioIva iva)
{
    public async Task<ResumenCliente> ResumenAsync(int clienteId, CancellationToken ct = default)
    {
        var c = await db.Clientes.AsNoTracking().Include(x => x.Gestor).Include(x => x.Contratos).Include(x => x.Actividades).FirstAsync(x => x.Id == clienteId, ct);
        var (ej, per) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        var plazo = CalendarioFiscal.Buscar("303", ej, per);
        int dias = plazo is null ? 99 : plazo.Fin.DayNumber - reloj.Hoy.DayNumber;
        var obligaciones = await db.Obligaciones.AsNoTracking().Include(o => o.Presentacion).Where(o => o.ClienteId == clienteId && o.Ejercicio == ej && o.Periodo == per && o.Estado != EstadoObligacion.NoProcede).OrderBy(o => o.Modelo).ToListAsync(ct);
        var docs = await db.Documentos.AsNoTracking().Where(d => d.ClienteId == clienteId && d.Ejercicio == ej && d.Periodo == per).ToListAsync(ct);
        var requisitos = RequisitosDocumentales.DelTrimestre(c, docs);
        var o303 = obligaciones.FirstOrDefault(o => o.Modelo == "303");
        ResultadoIva? r = o303 is null ? null : await iva.LiquidarAsync(clienteId, ej, per, ct);
        var facturas = await db.FacturasEmitidas.AsNoTracking().Where(f => f.ClienteId == clienteId && f.Fecha.Year == ej).OrderByDescending(f => f.Fecha).ToListAsync(ct);
        var (ini, fin) = Aserta.Dominio.Catalogo.Periodo.Rango(ej, per);
        var facturasTri = facturas.Where(f => f.Fecha >= ini && f.Fecha <= fin).ToList();
        var avisos = await db.Avisos.AsNoTracking().Where(a => a.ClienteId == clienteId).OrderByDescending(a => a.FechaUtc).Take(5).ToListAsync(ct);
        int avisosSinLeer = await db.Avisos.CountAsync(a => a.ClienteId == clienteId && !a.Leido, ct);
        int mensajes = await db.Mensajes.CountAsync(m => db.Incidencias.Any(i => i.Id == m.IncidenciaId && i.ClienteId == clienteId) && m.EsGestor && !m.LeidoPorCliente, ct);
        var fracc = o303?.Fraccionamiento ?? EstadoFraccionamiento.NoDisponible;
        bool puede = r is not null && r.Resultado >= ServicioIva.UmbralFraccionamiento && o303?.Estado != EstadoObligacion.Presentado;
        if (puede && fracc == EstadoFraccionamiento.NoDisponible) fracc = EstadoFraccionamiento.Disponible;
        return new ResumenCliente(c, ej, per, plazo, dias, obligaciones, obligaciones.Count(o => o.Estado == EstadoObligacion.Presentado), obligaciones.Count,
            requisitos, RequisitosDocumentales.Frase(requisitos), requisitos.Sum(x => Math.Min(x.Recibidos, x.Esperados)), requisitos.Sum(x => x.Esperados),
            r, o303, fracc, puede, facturasTri, avisosSinLeer, mensajes, avisos);
    }

    public async Task SolicitarFraccionamientoAsync(int clienteId, string nombreCliente, CancellationToken ct = default)
    {
        var (ej, per) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        var o = await db.Obligaciones.FirstOrDefaultAsync(x => x.ClienteId == clienteId && x.Ejercicio == ej && x.Periodo == per && x.Modelo == "303", ct);
        if (o is null || o.Fraccionamiento is EstadoFraccionamiento.Solicitado or EstadoFraccionamiento.Concedido) return;
        var r = await iva.LiquidarAsync(clienteId, ej, per, ct);
        o.Fraccionamiento = EstadoFraccionamiento.Solicitado;
        o.FraccionamientoSolicitadoUtc = reloj.AhoraUtc;
        var cliente = await db.Clientes.FirstAsync(x => x.Id == clienteId, ct);
        var inc = new Incidencia { ClienteId = clienteId, Tipo = TipoIncidencia.Fraccionamiento, Titulo = $"Fraccionar el pago del 303 {per} ({Infraestructura.Formato.Euros(r.Resultado)})", CreadaUtc = reloj.AhoraUtc, GestorId = cliente.GestorId, ObligacionId = o.Id };
        inc.Mensajes.Add(new Mensaje { Autor = nombreCliente, EsGestor = false, FechaUtc = reloj.AhoraUtc, Texto = $"Solicito fraccionar el pago del IVA del {CalendarioFiscal.NombrePeriodo(per)} ({Infraestructura.Formato.Euros(r.Resultado)}).", LeidoPorCliente = true });
        db.Incidencias.Add(inc);
        await db.SaveChangesAsync(ct);
    }
}
