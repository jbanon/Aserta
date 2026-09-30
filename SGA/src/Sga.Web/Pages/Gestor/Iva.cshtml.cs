using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Gestor;

public class IvaModel(SgaDb db, IReloj reloj, ServicioIva iva) : PaginaGestor(db)
{
    public Sga.Nucleo.Modelo.Cliente Cliente { get; private set; } = default!;
    public short Ejercicio; public string Periodo = "3T"; public string Libro = "emitidas";
    public ResultadoIva Iva { get; private set; } = default!;
    public Obligacion? Obligacion { get; private set; }
    public List<FacturaEmitida> Emitidas { get; private set; } = [];
    public List<FacturaRecibida> Recibidas { get; private set; } = [];

    private async Task<bool> CargarAsync(int id, string? periodo, string? libro)
    {
        var c = await Db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return false;
        Cliente = c;
        (Ejercicio, Periodo) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        if (periodo is "1T" or "2T" or "3T" or "4T") Periodo = periodo;
        Libro = libro == "recibidas" ? "recibidas" : "emitidas";
        Iva = await iva.LiquidarAsync(id, Ejercicio, Periodo);
        Obligacion = await Db.Obligaciones.AsNoTracking().Include(o => o.Presentacion).FirstOrDefaultAsync(o => o.ClienteId == id && o.Ejercicio == Ejercicio && o.Periodo == Periodo && o.Modelo == "303");
        var (_, fin) = Aserta.Dominio.Catalogo.Periodo.Rango(Ejercicio, Periodo);
        var inicio = new DateOnly(Ejercicio, 1, 1);
        Emitidas = await Db.FacturasEmitidas.AsNoTracking().Where(f => f.ClienteId == id && f.Fecha >= inicio && f.Fecha <= fin).OrderBy(f => f.Fecha).ThenBy(f => f.Numero).ToListAsync();
        Recibidas = await Db.FacturasRecibidas.AsNoTracking().Where(f => f.ClienteId == id && f.Fecha >= inicio && f.Fecha <= fin).OrderBy(f => f.Fecha).ToListAsync();
        return true;
    }

    public async Task<IActionResult> OnGetAsync(int id, string? periodo, string? libro) => await CargarAsync(id, periodo, libro) ? Page() : NotFound();

    /// <summary>Exportacion al programa de gestion (Monitor): CSV de los libros del periodo. No hay integracion real.</summary>
    public async Task<IActionResult> OnGetCsvAsync(int id, string? periodo)
    {
        if (!await CargarAsync(id, periodo, null)) return NotFound();
        var sb = new StringBuilder("Libro;Fecha;Numero;NIF;Nombre;Concepto;Base;TipoIVA;Cuota;Retencion;Total\n");
        var es = Infraestructura.Formato.Es;
        foreach (var f in Emitidas) sb.AppendLine($"Emitidas;{f.Fecha:dd/MM/yyyy};{f.Numero};{f.DestinatarioNif};{f.DestinatarioNombre};{f.Concepto};{f.Base.ToString("0.00", es)};{f.TipoIva.ToString("0.00", es)};{f.CuotaIva.ToString("0.00", es)};{f.CuotaRetencion.ToString("0.00", es)};{f.Total.ToString("0.00", es)}");
        foreach (var f in Recibidas) sb.AppendLine($"Recibidas;{f.Fecha:dd/MM/yyyy};{f.Numero};{f.ProveedorNif};{f.ProveedorNombre};{f.Concepto};{f.Base.ToString("0.00", es)};{f.TipoIva.ToString("0.00", es)};{f.CuotaIva.ToString("0.00", es)};0,00;{f.Total.ToString("0.00", es)}");
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(), "text/csv; charset=utf-8", $"monitor-{Cliente.Nif}-{Ejercicio}-{Periodo}.csv");
    }

    /// <summary>Lee un Excel (.xlsx) de los suyos y cuenta las filas con importes de cada hoja. Demostracion: muestra que se puede leer; no sustituye los libros.</summary>
    public async Task<IActionResult> OnPostImportarAsync(int id, string? periodo, IFormFile? excel)
    {
        if (excel is null || excel.Length == 0) { AvisoError = "Elige un fichero .xlsx."; return Redirect($"/gestor/clientes/{id}/iva?periodo={periodo}"); }
        try
        {
            using var ms = new MemoryStream(); await excel.CopyToAsync(ms); ms.Position = 0;
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var wb = XDocument.Load(zip.GetEntry("xl/workbook.xml")!.Open());
            var hojas = wb.Descendants(ns + "sheet").Select(s => (string?)s.Attribute("name") ?? "?").ToList();
            var partes = new List<string>();
            int i = 0;
            foreach (var e in zip.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/sheet")).OrderBy(e => e.FullName))
            {
                var doc = XDocument.Load(e.Open());
                int filas = doc.Descendants(ns + "row").Count(r => r.Elements(ns + "c").Any(c => c.Attribute("t") == null && c.Element(ns + "v") != null));
                partes.Add($"«{(i < hojas.Count ? hojas[i] : e.Name)}»: {filas} filas con números");
                i++;
            }
            AvisoOk = $"Leído {excel.FileName} ({hojas.Count} hojas): {string.Join(" · ", partes)}. En la demo el cálculo sigue usando los libros registrados; la importación real mapearía estas filas a los libros.";
        }
        catch (Exception ex) { AvisoError = "No se ha podido leer el fichero como Excel (.xlsx): " + ex.Message; }
        return Redirect($"/gestor/clientes/{id}/iva?periodo={periodo}");
    }
}
