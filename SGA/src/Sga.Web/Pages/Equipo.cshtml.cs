using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Pages;

public class EquipoModel(SgaDb db) : PaginaBase
{
    public List<Sga.Nucleo.Modelo.Gestor> Gestores { get; private set; } = [];
    public async Task OnGetAsync() => Gestores = await db.Gestores.AsNoTracking().OrderBy(g => g.Id).ToListAsync();
    public string Cita(Sga.Nucleo.Modelo.Gestor g) => g.Rol switch
    {
        "Socia directora" => "«Mi trabajo es que nadie se entere de que existe el día 20. Si el cliente no ha tenido que pensar en Hacienda, lo hemos hecho bien.»",
        "Gestora fiscal" => "«Lo que más me gusta es la llamada del día 8: “ya está todo, te falta un ticket”. Y ver cómo se les quita el peso de encima.»",
        _ => "«Cuadrar el banco de una empresa es como terminar un puzle. Cuando no casa, pregunto; nunca supongo.»",
    };
}
