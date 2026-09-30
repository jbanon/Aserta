using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Cliente;

public class IndexModel(SgaDb db, ServicioCliente servicio, IReloj reloj) : PaginaCliente(db)
{
    public ResumenCliente Resumen { get; private set; } = default!;
    public sealed record Hito(string Titulo, string Fecha, string Clase);
    public List<Hito> Hitos { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Resumen = await servicio.ResumenAsync(ClienteId);
        // Marcar avisos como leidos al verlos
        var noLeidos = await Db.Avisos.Where(a => a.ClienteId == ClienteId && !a.Leido).ToListAsync();
        if (noLeidos.Count > 0) { foreach (var a in noLeidos) a.Leido = true; await Db.SaveChangesAsync(); }
        var p = Resumen.Plazo303; var hoy = reloj.Hoy;
        bool docsOk = Resumen.DocsEsperados == 0 || Resumen.DocsRecibidos >= Resumen.DocsEsperados;
        bool presentado = Resumen.Obligacion303?.Estado == EstadoObligacion.Presentado || (Resumen.Aplicables > 0 && Resumen.Presentadas == Resumes());
        string Clase(bool hecho, bool actual) => hecho ? "hecho" : actual ? "actual" : "";
        Hitos =
        [
            new("Recopilar", p is null ? "" : $"{p.Inicio.Day}–10 {Infraestructura.Formato.MesCorto(p.Inicio.Month)}", Clase(docsOk, !docsOk)),
            new("Calculamos", "en cuanto esté todo", Clase(docsOk && (Resumen.Obligacion303?.Estado is EstadoObligacion.EnCurso or EstadoObligacion.Presentado), docsOk && Resumen.Obligacion303?.Estado == EstadoObligacion.Pendiente)),
            new("Presentamos", p is null ? "" : $"antes del {p.Fin.Day} {Infraestructura.Formato.MesCorto(p.Fin.Month)}", Clase(presentado, docsOk && !presentado && Resumen.Obligacion303?.Estado == EstadoObligacion.EnCurso)),
            new("Cargo en cuenta", p is null ? "" : $"{p.Fin.Day} {Infraestructura.Formato.MesCorto(p.Fin.Month)}", Clase(presentado && hoy > p!.Fin, presentado && hoy <= p!.Fin)),
        ];
    }
    private int Resumes() => Resumen.Aplicables;
}
