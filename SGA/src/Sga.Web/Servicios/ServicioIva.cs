using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Servicios;

/// <summary>La mesa de calculo de IVA: convierte los libros del cliente en el calculo acumulado de la hoja LIQ IVA.</summary>
public sealed class ServicioIva(SgaDb db)
{
    public async Task<ResultadoIva> LiquidarAsync(int clienteId, short ejercicio, string periodo, CancellationToken ct = default)
    {
        var emitidas = await db.FacturasEmitidas.AsNoTracking().Where(f => f.ClienteId == clienteId && f.Fecha.Year == ejercicio).Select(f => new LineaIva(f.Fecha, f.Base, f.TipoIva, f.CuotaIva, true)).ToListAsync(ct);
        var recibidas = await db.FacturasRecibidas.AsNoTracking().Where(f => f.ClienteId == clienteId && f.Fecha.Year == ejercicio).Select(f => new LineaIva(f.Fecha, f.Base, f.TipoIva, f.CuotaIva, f.Deducible)).ToListAsync(ct);
        var anteriores = await db.Obligaciones.AsNoTracking()
            .Where(o => o.ClienteId == clienteId && o.Ejercicio == ejercicio && o.Modelo == "303" && o.Estado == EstadoObligacion.Presentado && o.Periodo != periodo)
            .Select(o => new LiquidacionAnterior(o.Periodo, o.RepercutidoLiquidado ?? 0m, o.SoportadoLiquidado ?? 0m, o.Resultado ?? 0m)).ToListAsync(ct);
        return CalculoIva.Liquidar(ejercicio, periodo, emitidas, recibidas, anteriores);
    }

    /// <summary>Umbral a partir del cual se ofrece fraccionar el pago [VERIFICAR con SGA; supuesto de la demo].</summary>
    public const decimal UmbralFraccionamiento = 1000m;
}
