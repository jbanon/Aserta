using System.Globalization;
using Aserta.Aplicacion.Nucleo;
using Aserta.Aplicacion.Obligaciones;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Obligaciones;

public class CalendarioModel : PaginaBase
{
    private readonly ServicioTablero _tablero;
    private readonly ServicioUsuarios _usuarios;
    private readonly IAsertaDb _db;

    public CalendarioModel(ServicioTablero tablero, ServicioUsuarios usuarios, IAsertaDb db)
    {
        _tablero = tablero;
        _usuarios = usuarios;
        _db = db;
    }

    [BindProperty(SupportsGet = true)] public string? MesTexto { get; set; }
    [BindProperty(SupportsGet = true, Name = "mes")] public string? MesParametro { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? Asesor { get; set; }

    public DateOnly Mes { get; private set; }
    public DateOnly Anterior => Mes.AddMonths(-1);
    public DateOnly Siguiente => Mes.AddMonths(1);
    public DateOnly Hoy => _tablero.Hoy;
    public List<DateOnly> Dias { get; private set; } = [];
    public List<TarjetaObligacion> Items { get; private set; } = [];
    public Dictionary<DateOnly, List<TarjetaObligacion>> PorDia { get; private set; } = [];
    public List<TarjetaObligacion> Proximas { get; private set; } = [];
    public Dictionary<Guid, string> NombresClientes { get; private set; } = [];
    public List<Usuario> Asesores { get; private set; } = [];
    private Dictionary<DateOnly, string> _inhabiles = [];

    public bool EsInhabil(DateOnly d) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || _inhabiles.ContainsKey(d);
    public string? NombreInhabil(DateOnly d) => _inhabiles.GetValueOrDefault(d);

    public async Task OnGetAsync()
    {
        Mes = DateOnly.TryParseExact(MesParametro, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var m) ? m : new DateOnly(Hoy.Year, Hoy.Month, 1);
        Mes = new DateOnly(Mes.Year, Mes.Month, 1);
        Asesores = await _usuarios.AsesoresActivosAsync();

        // Rejilla de lunes a domingo que cubre el mes
        var primero = Mes;
        int desplazamiento = ((int)primero.DayOfWeek + 6) % 7;
        var inicio = primero.AddDays(-desplazamiento);
        var finMes = Mes.AddMonths(1).AddDays(-1);
        var fin = finMes.AddDays((7 - ((int)finMes.DayOfWeek + 6) % 7 - 1));
        for (var d = inicio; d <= fin; d = d.AddDays(1)) Dias.Add(d);

        _inhabiles = await _db.DiasInhabiles.AsNoTracking().Where(x => x.Fecha >= inicio && x.Fecha <= fin).ToDictionaryAsync(x => x.Fecha, x => x.Descripcion);
        Items = await _tablero.EntreFechasAsync(inicio, fin, Asesor);
        PorDia = Items.GroupBy(i => Semaforo.FechaDeReferencia(i.Obligacion)).ToDictionary(g => g.Key, g => g.ToList());

        Proximas = (await _tablero.EntreFechasAsync(Hoy, Hoy.AddDays(30), Asesor)).Where(p => !p.Obligacion.Estado.EsFinalOPresentado()).ToList();
        NombresClientes = Proximas.GroupBy(p => p.Obligacion.ClienteId).ToDictionary(g => g.Key, g => g.First().Cliente);
    }
}
