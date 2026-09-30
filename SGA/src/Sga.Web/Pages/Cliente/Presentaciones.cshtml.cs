using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Pages.Cliente;

public class PresentacionesModel(SgaDb db) : PaginaCliente(db)
{
    public List<IGrouping<string, Obligacion>> Grupos { get; private set; } = [];
    public async Task OnGetAsync()
    {
        var lista = await Db.Obligaciones.AsNoTracking().Include(o => o.Presentacion).Where(o => o.ClienteId == ClienteId && o.Estado == EstadoObligacion.Presentado && o.Presentacion != null).OrderByDescending(o => o.FechaPresentacion).ToListAsync();
        Grupos = lista.GroupBy(o => o.Periodo == "AN" ? $"Anuales del ejercicio {o.Ejercicio}" : $"{CalendarioFiscal.NombrePeriodo(o.Periodo)} de {o.Ejercicio}".Replace("primer", "Primer").Replace("segundo", "Segundo").Replace("tercer", "Tercer").Replace("cuarto", "Cuarto")).ToList();
    }
}
