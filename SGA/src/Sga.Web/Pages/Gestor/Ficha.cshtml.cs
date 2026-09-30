using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Gestor;

public class FichaModel(SgaDb db, IReloj reloj, ServicioIva iva) : PaginaGestor(db)
{
    public Sga.Nucleo.Modelo.Cliente Cliente { get; private set; } = default!;
    public string Pestana { get; private set; } = "trimestre";
    public short Ejercicio; public string Periodo = "3T"; public string? ModeloDestacado; public string? PeriodoDestacado;
    public List<Obligacion> Obligaciones { get; private set; } = [];
    public List<Obligacion> Historial { get; private set; } = [];
    public List<Documento> Documentos { get; private set; } = [];
    public List<FacturaEmitida> Emitidas { get; private set; } = [];
    public List<Incidencia> IncidenciasAbiertas { get; private set; } = [];
    public IReadOnlyList<RequisitoDocumental> Requisitos { get; private set; } = [];
    public string FraseDocumentos { get; private set; } = "";
    public ResultadoIva? Iva { get; private set; }
    public List<(string Modelo, bool Aplica)> ModelosAplicables { get; private set; } = [];

    private async Task<bool> CargarAsync(int id, string? pestana, string? modelo, string? periodo)
    {
        var c = await Db.Clientes.AsNoTracking().Include(x => x.Gestor).Include(x => x.Actividades).Include(x => x.Contratos).FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return false;
        Cliente = c;
        Pestana = pestana is "resumen" or "documentos" or "facturas" or "historial" ? pestana : "trimestre";
        (Ejercicio, Periodo) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        if (periodo is "1T" or "2T" or "3T" or "4T") Periodo = periodo;
        ModeloDestacado = modelo; PeriodoDestacado = periodo;
        var visibles = ServicioGestor.PeriodosVisibles(Periodo);
        short anterior = (short)(Ejercicio - 1);
        Obligaciones = await Db.Obligaciones.AsNoTracking().Include(o => o.Gestor).Include(o => o.Presentacion).Where(o => o.ClienteId == id && o.Estado != EstadoObligacion.NoProcede && ((o.Ejercicio == Ejercicio && visibles.Contains(o.Periodo) && o.Periodo != "AN") || (o.Ejercicio == anterior && o.Periodo == "AN"))).OrderBy(o => o.Periodo == Periodo ? 0 : 1).ThenBy(o => o.Modelo).ToListAsync();
        Historial = await Db.Obligaciones.AsNoTracking().Include(o => o.Presentacion).Where(o => o.ClienteId == id && o.Estado == EstadoObligacion.Presentado).OrderByDescending(o => o.FechaPresentacion).ToListAsync();
        Documentos = await Db.Documentos.AsNoTracking().Where(d => d.ClienteId == id).OrderByDescending(d => d.SubidoUtc).ToListAsync();
        Emitidas = await Db.FacturasEmitidas.AsNoTracking().Where(f => f.ClienteId == id).OrderByDescending(f => f.Fecha).ThenByDescending(f => f.Numero).ToListAsync();
        IncidenciasAbiertas = await Db.Incidencias.AsNoTracking().Where(i => i.ClienteId == id && i.Estado != EstadoIncidencia.Resuelta).ToListAsync();
        Requisitos = RequisitosDocumentales.DelTrimestre(c, Documentos.Where(d => d.Ejercicio == Ejercicio && d.Periodo == Periodo));
        FraseDocumentos = RequisitosDocumentales.Frase(Requisitos).Replace("Te falta", "Le falta").Replace("Tienes todo entregado. ¡Gracias!", "Tiene todo entregado.");
        if (Obligaciones.Any(o => o.Modelo == "303" && o.Periodo == Periodo)) Iva = await iva.LiquidarAsync(id, Ejercicio, Periodo);
        var todos = await Db.Obligaciones.AsNoTracking().Where(o => o.ClienteId == id).Select(o => new { o.Modelo, o.Estado }).ToListAsync();
        ModelosAplicables = todos.GroupBy(o => o.Modelo).Select(g => (g.Key, g.Any(x => x.Estado != EstadoObligacion.NoProcede))).OrderBy(x => x.Item1).ToList();
        return true;
    }

    public async Task<IActionResult> OnGetAsync(int id, string? pestana, string? modelo, string? periodo) => await CargarAsync(id, pestana, modelo, periodo) ? Page() : NotFound();

    public async Task<IActionResult> OnPostEmpezarAsync(int id, int obligacionId)
    {
        var o = await Db.Obligaciones.FirstOrDefaultAsync(x => x.Id == obligacionId && x.ClienteId == id);
        if (o is null) return NotFound();
        if (o.Estado == EstadoObligacion.Pendiente) { o.Estado = EstadoObligacion.EnCurso; o.GestorId = GestorId; await Db.SaveChangesAsync(); AvisoOk = $"{o.Modelo} {o.Periodo} asignado a ti."; }
        return Redirect($"/gestor/clientes/{id}?pestana=trimestre&modelo={o.Modelo}&periodo={o.Periodo}");
    }

    public async Task<IActionResult> OnPostRevisarAsync(int id, int documentoId)
    {
        var d = await Db.Documentos.FirstOrDefaultAsync(x => x.Id == documentoId && x.ClienteId == id);
        if (d is null) return NotFound();
        d.Estado = EstadoDocumento.Revisado; await Db.SaveChangesAsync();
        return Redirect($"/gestor/clientes/{id}?pestana=documentos");
    }

    public async Task<IActionResult> OnPostReclamarAsync(int id)
    {
        if (!await CargarAsync(id, "trimestre", null, null)) return NotFound();
        var faltan = Requisitos.Where(r => !r.Completo).ToList();
        if (faltan.Count > 0)
        {
            var frase = RequisitosDocumentales.Frase(Requisitos);
            var inc = new Incidencia { ClienteId = id, Tipo = TipoIncidencia.Documento, Titulo = $"Documentación del {Periodo}: {frase.TrimEnd('.')}", CreadaUtc = reloj.AhoraUtc, GestorId = GestorId };
            inc.Mensajes.Add(new Mensaje { Autor = Gestor.Nombre, EsGestor = true, Texto = $"Hola. Para cerrar el {CalendarioFiscal.NombrePeriodo(Periodo)} nos falta: {string.Join(", ", faltan.Select(f => f.Nombre.ToLowerInvariant() + (f.Esperados > 1 ? $" ({f.Faltan})" : "")))}. Con eso lo presentamos el mismo día.", FechaUtc = reloj.AhoraUtc, LeidoPorGestor = true });
            Db.Incidencias.Add(inc);
            Db.Avisos.Add(new Aviso { ClienteId = id, FechaUtc = reloj.AhoraUtc, Texto = $"{Gestor.Nombre.Split(' ')[0]} te ha escrito: {frase}", Enlace = "/cliente/documentos" });
            await Db.SaveChangesAsync();
            AvisoOk = "Reclamación enviada al cliente (aviso en su área y mensaje).";
        }
        return Redirect($"/gestor/clientes/{id}?pestana=trimestre");
    }
}
