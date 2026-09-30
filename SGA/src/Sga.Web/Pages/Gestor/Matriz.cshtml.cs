using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Gestor;

public class MatrizModel(SgaDb db, IReloj reloj, ServicioGestor servicio) : PaginaGestor(db)
{
    public short Ejercicio; public string Periodo = "3T";
    public int? GestorFiltro; public TipoCliente? TipoFiltro; public string? ModeloFiltro;
    public Matriz Matriz { get; private set; } = default!;
    public List<Sga.Nucleo.Modelo.Gestor> Gestores { get; private set; } = [];
    public IReadOnlyList<(string Grupo, string[] Modelos)> Grupos { get; } =
    [
        ("IVA", ["303", "349"]), ("IRPF", ["130"]), ("Retenciones", ["111", "115", "123"]), ("Sociedades", ["202"]), ("Contabilidad", ["LIBROS", "CCAA"]),
    ];

    public async Task OnGetAsync(string? periodo, int? gestor, TipoCliente? tipo, string? modelo)
    {
        (Ejercicio, Periodo) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        if (periodo is "1T" or "2T" or "3T" or "4T") Periodo = periodo;
        GestorFiltro = gestor; TipoFiltro = tipo; ModeloFiltro = string.IsNullOrEmpty(modelo) ? null : modelo;
        Gestores = await Db.Gestores.AsNoTracking().OrderBy(g => g.Id).ToListAsync();
        Matriz = await servicio.MatrizAsync(Ejercicio, Periodo, gestor, tipo, ModeloFiltro);
    }
}
