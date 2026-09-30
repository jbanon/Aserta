using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Pages.Gestor;

public class CalendarioModel(SgaDb db, IReloj reloj) : PaginaGestor(db)
{
    public DateOnly Mes { get; private set; }
    public DateOnly Anterior => Mes.AddMonths(-1); public DateOnly Siguiente => Mes.AddMonths(1);
    public sealed record Evento(string Texto, bool Domiciliacion);
    public sealed record Dia(DateOnly Fecha, bool Inhabil, string? Festivo, List<Evento> Eventos);
    public sealed record Proximo(Plazo Plazo, int Clientes);
    public List<Dia> Dias { get; private set; } = [];
    public List<Proximo> Proximos { get; private set; } = [];

    public async Task OnGetAsync(string? mes)
    {
        Mes = DateOnly.TryParseExact(mes, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var m) ? m : new DateOnly(reloj.Hoy.Year, reloj.Hoy.Month, 1);
        var primero = Mes; int desplaz = ((int)primero.DayOfWeek + 6) % 7; var inicio = primero.AddDays(-desplaz);
        var plazos = CalendarioFiscal.Plazos.Where(p => p.Modelo is "303" or "111" or "115" or "130" or "202" or "349" or "390" or "347" or "190" or "200" or "CCAA" or "LIBROS" or "100").ToList();
        for (int i = 0; i < 42; i++)
        {
            var f = inicio.AddDays(i);
            var eventos = new List<Evento>();
            foreach (var p in plazos.Where(p => p.Fin == f).GroupBy(p => p.Modelo).Select(g => g.First())) eventos.Add(new($"{p.Modelo} {p.Periodo}", false));
            foreach (var p in plazos.Where(p => p.Domiciliacion == f).GroupBy(p => p.Modelo).Select(g => g.First())) eventos.Add(new($"dom. {p.Modelo}", true));
            Dias.Add(new Dia(f, !CalendarioFiscal.Habil.EsHabil(f), CalendarioFiscal.Festivos.GetValueOrDefault(f), eventos));
        }
        var hasta = reloj.Hoy.AddDays(90);
        var afectados = await Db.Obligaciones.AsNoTracking().Where(o => o.Estado != EstadoObligacion.NoProcede).GroupBy(o => new { o.Modelo, o.Ejercicio, o.Periodo }).Select(g => new { g.Key.Modelo, g.Key.Ejercicio, g.Key.Periodo, N = g.Count() }).ToListAsync();
        Proximos = plazos.Where(p => p.Fin >= reloj.Hoy && p.Fin <= hasta).GroupBy(p => (p.Modelo, p.Ejercicio, p.Periodo)).Select(g => g.First()).OrderBy(p => p.Fin)
            .Select(p => new Proximo(p, afectados.FirstOrDefault(a => a.Modelo == p.Modelo && a.Periodo == p.Periodo && (a.Ejercicio == p.Ejercicio || (p.Periodo == "AN" && a.Ejercicio == p.Ejercicio)))?.N ?? 0)).ToList();
    }
}
