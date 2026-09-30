using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Gestor;

public class PresentarModel(SgaDb db, ServicioIva iva, ServicioPresentacion presentacion) : PaginaGestor(db)
{
    public Sga.Nucleo.Modelo.Cliente Cliente { get; private set; } = default!;
    public Obligacion Obligacion { get; private set; } = default!;
    public ResultadoIva? Iva { get; private set; }
    public decimal Importe { get; private set; }

    private async Task<bool> CargarAsync(int id, int obligacionId)
    {
        var c = await Db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        var o = await Db.Obligaciones.AsNoTracking().Include(x => x.Presentacion).FirstOrDefaultAsync(x => x.Id == obligacionId && x.ClienteId == id);
        if (c is null || o is null) return false;
        Cliente = c; Obligacion = o;
        if (o.Modelo == "303") { Iva = await iva.LiquidarAsync(id, o.Ejercicio, o.Periodo); Importe = Iva.Resultado; }
        else Importe = o.Resultado ?? (o.Modelo == "111" ? 420m + id * 37m : o.Modelo == "130" ? 260m + id * 41m : o.Modelo == "202" ? 1200m + id * 90m : 0m);
        return true;
    }

    public async Task<IActionResult> OnGetAsync(int id, int obligacionId)
    {
        if (!await CargarAsync(id, obligacionId)) return NotFound();
        if (Obligacion.Presentacion is not null) return Redirect($"/gestor/justificante/{Obligacion.Presentacion.Id}");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, int obligacionId, decimal? importe)
    {
        if (!await CargarAsync(id, obligacionId)) return NotFound();
        if (Obligacion.Modelo != "303" && importe is decimal imp) { var o = await Db.Obligaciones.FirstAsync(x => x.Id == obligacionId); o.Resultado = imp; await Db.SaveChangesAsync(); }
        var p = await presentacion.PresentarAsync(obligacionId, GestorId);
        AvisoOk = $"{Obligacion.Modelo} {Obligacion.Periodo} presentado (simulación). Justificante {p.NumeroJustificante} · CSV {p.Csv}.";
        return Redirect($"/gestor/clientes/{id}?pestana=trimestre&modelo={Obligacion.Modelo}&periodo={Obligacion.Periodo}");
    }
}
