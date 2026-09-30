using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Servicios;

/// <summary>Emision de las facturas de alquiler por cuenta del arrendador, envio simulado y comprobacion de numeracion.</summary>
public sealed class ServicioFacturacion(SgaDb db, IReloj reloj)
{
    public async Task<FacturaEmitida> EmitirAlquilerAsync(int contratoId, int anio, int mes, CancellationToken ct = default)
    {
        var contrato = await db.Contratos.FirstAsync(c => c.Id == contratoId, ct);
        var fecha = new DateOnly(anio, mes, 1);
        var existentes = await db.FacturasEmitidas.CountAsync(f => f.ContratoId == contratoId && f.Fecha.Year == anio && f.Fecha.Month == mes, ct);
        var importes = CalculoFactura.Calcular(contrato.RentaMensual, contrato.TipoIva, contrato.TipoRetencion);
        var factura = new FacturaEmitida
        {
            ClienteId = contrato.ClienteId, ContratoId = contrato.Id, Numero = CalculoFactura.NumeroAlquiler(fecha, existentes + 1), Fecha = fecha,
            DestinatarioNombre = contrato.InquilinoNombre, DestinatarioNif = contrato.InquilinoNif,
            Concepto = $"Cuota alquiler {contrato.Inmueble} · {Infraestructura.Formato.NombreMes(mes)} {anio}",
            Base = importes.Base, TipoIva = importes.TipoIva, CuotaIva = importes.CuotaIva, TipoRetencion = importes.TipoRetencion, CuotaRetencion = importes.CuotaRetencion, Total = importes.Total,
            Origen = OrigenFactura.Sga, Estado = EstadoFacturaEmitida.Generada, RegistradaMonitor = true,
        };
        db.FacturasEmitidas.Add(factura);
        await db.SaveChangesAsync(ct);
        return factura;
    }

    /// <summary>Generacion en bloque al cierre del trimestre: una factura por contrato activo y mes que aun no la tenga.</summary>
    public async Task<List<FacturaEmitida>> GenerarBloqueTrimestreAsync(int clienteId, short ejercicio, string periodo, CancellationToken ct = default)
    {
        var (inicio, fin) = Aserta.Dominio.Catalogo.Periodo.Rango(ejercicio, periodo);
        var contratos = await db.Contratos.Where(c => c.ClienteId == clienteId && c.Activo).ToListAsync(ct);
        var generadas = new List<FacturaEmitida>();
        foreach (var c in contratos)
            for (var m = inicio; m <= fin; m = m.AddMonths(1))
                if (!await db.FacturasEmitidas.AnyAsync(f => f.ContratoId == c.Id && f.Fecha.Year == m.Year && f.Fecha.Month == m.Month, ct))
                    generadas.Add(await EmitirAlquilerAsync(c.Id, m.Year, m.Month, ct));
        return generadas;
    }

    /// <summary>Envio simulado al inquilino con copia al cliente: cambia el estado y deja un aviso al cliente.</summary>
    public async Task EnviarAsync(int facturaId, CancellationToken ct = default)
    {
        var f = await db.FacturasEmitidas.FirstAsync(x => x.Id == facturaId, ct);
        if (f.Estado >= EstadoFacturaEmitida.EnviadaInquilino) return;
        f.Estado = EstadoFacturaEmitida.EnviadaInquilino;
        f.EnviadaUtc = reloj.AhoraUtc;
        db.Avisos.Add(new Aviso { ClienteId = f.ClienteId, FechaUtc = reloj.AhoraUtc, Texto = $"Hemos enviado la factura {f.Numero} a {f.DestinatarioNombre}. Tienes tu copia en «Mis facturas».", Enlace = "/cliente/facturas" });
        await db.SaveChangesAsync(ct);
    }

    public async Task<InformeNumeracion> ComprobarNumeracionAsync(int clienteId, short ejercicio, CancellationToken ct = default)
    {
        var facturas = await db.FacturasEmitidas.AsNoTracking().Where(f => f.ClienteId == clienteId && f.Fecha.Year == ejercicio && f.Origen == OrigenFactura.Cliente)
            .Select(f => new FacturaNumerada(f.Numero, f.Fecha)).ToListAsync(ct);
        return ComprobadorNumeracion.Analizar(facturas);
    }
}
