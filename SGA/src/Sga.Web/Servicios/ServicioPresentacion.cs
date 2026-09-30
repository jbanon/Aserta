using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Servicios;

/// <summary>
/// Presentacion SIMULADA de un modelo: genera un justificante con el mismo aspecto que el de la AEAT
/// (expediente, CSV, numero de justificante, presentador "Colaborador") sin ninguna conexion real.
/// </summary>
public sealed class ServicioPresentacion(SgaDb db, IReloj reloj, ServicioIva iva)
{
    public async Task<Presentacion> PresentarAsync(int obligacionId, int gestorId, CancellationToken ct = default)
    {
        var o = await db.Obligaciones.Include(x => x.Cliente).Include(x => x.Presentacion).FirstAsync(x => x.Id == obligacionId, ct);
        if (o.Presentacion is not null) return o.Presentacion;
        var gestor = await db.Gestores.FirstAsync(g => g.Id == gestorId, ct);
        decimal importe = 0m;
        if (o.Modelo == "303")
        {
            var r = await iva.LiquidarAsync(o.ClienteId, o.Ejercicio, o.Periodo, ct);
            importe = r.Resultado;
            o.RepercutidoLiquidado = r.RepercutidoALiquidar;
            o.SoportadoLiquidado = r.SoportadoALiquidar;
        }
        else importe = o.Resultado ?? 0m;
        var p = new Presentacion
        {
            ObligacionId = o.Id, FechaHora = reloj.AhoraUtc, Expediente = Justificantes.Expediente(o.Ejercicio, o.Modelo), Csv = Justificantes.Csv(),
            NumeroJustificante = Justificantes.NumeroJustificante(o.Modelo), PresentadorNombre = gestor.Nombre, PresentadorNif = gestor.Nif,
            Importe = importe, Iban = importe > 0 && o.Cliente!.FormaPago == FormaPago.Domiciliacion ? o.Cliente.Iban : null,
        };
        o.Estado = EstadoObligacion.Presentado;
        o.GestorId = gestorId;
        o.FechaPresentacion = reloj.Hoy;
        o.Resultado = importe;
        o.Presentacion = p;
        db.Avisos.Add(new Aviso
        {
            ClienteId = o.ClienteId, FechaUtc = reloj.AhoraUtc, Enlace = "/cliente/presentaciones",
            Texto = importe > 0
                ? $"Hemos presentado tu modelo {o.Modelo} del {CalendarioFiscal.NombrePeriodo(o.Periodo)}. Se cargarán {Infraestructura.Formato.Euros(importe)} en tu cuenta{FechaCargo(o)}."
                : $"Hemos presentado tu modelo {o.Modelo} del {CalendarioFiscal.NombrePeriodo(o.Periodo)}. Ya puedes descargar el justificante.",
        });
        await db.SaveChangesAsync(ct);
        return p;
    }

    private static string FechaCargo(Obligacion o)
    {
        var plazo = CalendarioFiscal.Buscar(o.Modelo, o.Ejercicio, o.Periodo);
        return plazo?.Fin is DateOnly f ? $" el {Infraestructura.Formato.Fecha(f)}" : "";
    }
}
