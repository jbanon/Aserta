using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Pages.Cliente;

public class HonorariosModel(SgaDb db) : PaginaCliente(db)
{
    public List<FacturaHonorarios> Facturas { get; private set; } = [];
    public async Task OnGetAsync() => Facturas = await Db.Honorarios.AsNoTracking().Where(f => f.ClienteId == ClienteId).OrderByDescending(f => f.Fecha).ToListAsync();
}
