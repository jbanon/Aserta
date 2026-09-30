using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Gestor;

public class ClientesModel(SgaDb db, IReloj reloj, ServicioGestor servicio) : PaginaGestor(db)
{
    public TipoCliente? Tipo { get; private set; }
    public IReadOnlyList<FilaMatriz> Filas { get; private set; } = [];
    public async Task OnGetAsync(TipoCliente? tipo)
    {
        Tipo = tipo;
        var (ej, per) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        Filas = (await servicio.MatrizAsync(ej, per, null, tipo)).Filas;
    }
}
