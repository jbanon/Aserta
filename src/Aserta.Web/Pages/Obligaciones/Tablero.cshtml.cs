using Aserta.Aplicacion.Comun;
using Aserta.Aplicacion.Nucleo;
using Aserta.Aplicacion.Obligaciones;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Comun;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Obligaciones;

public class TableroModel : PaginaBase
{
    private readonly ServicioTablero _tablero;
    private readonly ServicioObligaciones _obligaciones;
    private readonly ServicioUsuarios _usuarios;
    private readonly IAsertaDb _db;

    public TableroModel(ServicioTablero tablero, ServicioObligaciones obligaciones, ServicioUsuarios usuarios, IAsertaDb db)
    {
        _tablero = tablero;
        _obligaciones = obligaciones;
        _usuarios = usuarios;
        _db = db;
    }

    [BindProperty(SupportsGet = true)] public FiltroTablero Filtro { get; set; } = new();
    [BindProperty(SupportsGet = true)] public bool OcultarCerradas { get; set; }
    public List<TarjetaObligacion> Tarjetas { get; private set; } = [];
    public List<Usuario> Asesores { get; private set; } = [];
    public List<int> Ejercicios { get; private set; } = [];
    public DateOnly Hoy => _tablero.Hoy;

    public async Task OnGetAsync()
    {
        if (Filtro.Ejercicio == 0) Filtro.Ejercicio = Hoy.Year;
        Ejercicios = [Hoy.Year - 1, Hoy.Year, Hoy.Year + 1];
        Asesores = await _usuarios.AsesoresActivosAsync();
        Tarjetas = await _tablero.TarjetasAsync(Filtro, incluirFinales: !OcultarCerradas);
    }

    /// <summary>Arrastre o menu "Mover a…": devuelve la tarjeta repintada; 400 con el motivo si la transicion no es valida.</summary>
    public async Task<IActionResult> OnPostMoverAsync(Guid id, EstadoObligacion destino, int? orden)
    {
        try
        {
            var o = await _obligaciones.MoverAsync(id, destino, orden);
            var tarjeta = await _tablero.TarjetaAsync(o.Id);
            Response.Headers["X-Aserta-Anuncio"] = Uri.EscapeDataString($"{o.Titulo} movida a {destino.Etiqueta()}");
            var vista = Partial("_Tarjeta", tarjeta);
            vista.ViewData["Hoy"] = Hoy;
            return vista;
        }
        catch (ExcepcionDominio ex) { return BadRequest(ex.Message); }
        catch (ExcepcionNoAutorizado ex) { return StatusCode(403, ex.Message); }
        catch (ExcepcionNoEncontrado ex) { return NotFound(ex.Message); }
    }

    public async Task<IActionResult> OnGetDetalleAsync(Guid id)
    {
        var tarjeta = await _tablero.TarjetaAsync(id);
        var nombres = await _db.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.NombreCompleto);
        var vista = Partial("_DetalleObligacion", tarjeta);
        vista.ViewData["Hoy"] = Hoy;
        vista.ViewData["NombresUsuarios"] = nombres;
        return vista;
    }
}
