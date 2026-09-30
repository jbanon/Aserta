using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

using Sga.Web.Infraestructura;

namespace Sga.Web.Pages.Cliente;

public class ImpuestosModel(SgaDb db, IReloj reloj, ServicioIva iva, ServicioCliente servicio) : PaginaCliente(db)
{
    public short Ejercicio { get; private set; }
    public string Periodo { get; private set; } = "3T";
    public ResultadoIva? Iva { get; private set; }
    public Obligacion? Obligacion { get; private set; }
    public Plazo? Plazo { get; private set; }
    public bool PuedeFraccionar { get; private set; }
    public sealed record FilaTrimestre(string Periodo, EstadoObligacion Estado, decimal? Repercutido, decimal? Soportado, decimal? Resultado, int? PresentacionId);
    public List<FilaTrimestre> Trimestres { get; private set; } = [];

    public async Task OnGetAsync()
    {
        (Ejercicio, Periodo) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        var todas = await Db.Obligaciones.AsNoTracking().Include(o => o.Presentacion).Where(o => o.ClienteId == ClienteId && o.Ejercicio == Ejercicio && o.Modelo == "303").OrderBy(o => o.Periodo).ToListAsync();
        Obligacion = todas.FirstOrDefault(o => o.Periodo == Periodo);
        if (Obligacion is null || Obligacion.Estado == EstadoObligacion.NoProcede) return;
        Iva = await iva.LiquidarAsync(ClienteId, Ejercicio, Periodo);
        Plazo = CalendarioFiscal.Buscar("303", Ejercicio, Periodo);
        PuedeFraccionar = Iva.Resultado >= ServicioIva.UmbralFraccionamiento && Obligacion.Estado != EstadoObligacion.Presentado && Obligacion.Fraccionamiento == EstadoFraccionamiento.NoDisponible;
        foreach (var o in todas)
        {
            if (o.Estado == EstadoObligacion.Presentado) Trimestres.Add(new(o.Periodo, o.Estado, o.RepercutidoLiquidado, o.SoportadoLiquidado, o.Resultado, o.Presentacion?.Id));
            else if (o.Periodo == Periodo) Trimestres.Add(new(o.Periodo, o.Estado, Iva.RepercutidoALiquidar, Iva.SoportadoALiquidar, Iva.Resultado, null));
            else Trimestres.Add(new(o.Periodo, o.Estado, null, null, null, null));
        }
    }

    public async Task<IActionResult> OnPostFraccionarAsync()
    {
        await servicio.SolicitarFraccionamientoAsync(ClienteId, User.NombreActual());
        AvisoOk = "Hemos recibido tu solicitud de fraccionamiento. Tu gestora te contesta en menos de 48 horas.";
        return RedirectToPage();
    }
}
