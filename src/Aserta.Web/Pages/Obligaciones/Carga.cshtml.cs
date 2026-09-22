using Aserta.Aplicacion.Obligaciones;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;

namespace Aserta.Web.Pages.Obligaciones;

public class CargaModel : PaginaBase
{
    private readonly ServicioTablero _tablero;
    public CargaModel(ServicioTablero tablero) => _tablero = tablero;

    [BindProperty(SupportsGet = true)] public int? Ejercicio { get; set; }
    public List<CargaAsesor> Filas { get; private set; } = [];
    public List<int> Ejercicios { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Ejercicio ??= _tablero.Hoy.Year;
        Ejercicios = [_tablero.Hoy.Year - 1, _tablero.Hoy.Year, _tablero.Hoy.Year + 1];
        Filas = await _tablero.CargaPorAsesorAsync(Ejercicio.Value);
    }
}
