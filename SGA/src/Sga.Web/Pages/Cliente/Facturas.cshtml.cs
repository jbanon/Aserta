using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Cliente;

public class FacturasModel(SgaDb db, IReloj reloj, ServicioFacturacion facturacion, GeneradorPdf pdf) : PaginaCliente(db)
{
    public short Ejercicio { get; private set; }
    public List<IGrouping<string, FacturaEmitida>> PorMes { get; private set; } = [];
    public InformeNumeracion? Numeracion { get; private set; }
    public decimal TotalBase, TotalIva, TotalRetencion;

    public async Task OnGetAsync()
    {
        Ejercicio = (short)reloj.Hoy.Year;
        var facturas = await Db.FacturasEmitidas.AsNoTracking().Where(f => f.ClienteId == ClienteId && f.Fecha.Year == Ejercicio).OrderByDescending(f => f.Fecha).ThenByDescending(f => f.Numero).ToListAsync();
        PorMes = facturas.GroupBy(f => Infraestructura.Formato.Mes(f.Fecha)).ToList();
        TotalBase = facturas.Sum(f => f.Base); TotalIva = facturas.Sum(f => f.CuotaIva); TotalRetencion = facturas.Sum(f => f.CuotaRetencion);
        if (Cliente.Emisor == QuienEmite.Cliente) Numeracion = await facturacion.ComprobarNumeracionAsync(ClienteId, Ejercicio);
    }

    public async Task<IActionResult> OnGetPdfAsync(int id)
    {
        var f = await Db.FacturasEmitidas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ClienteId == ClienteId);
        if (f is null) return NotFound();
        var contrato = f.ContratoId is int cid ? await Db.Contratos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cid) : null;
        return File(pdf.FacturaAlquiler(f, Cliente, contrato), "application/pdf", $"factura-{f.Numero.Replace('/', '-')}.pdf");
    }
}
