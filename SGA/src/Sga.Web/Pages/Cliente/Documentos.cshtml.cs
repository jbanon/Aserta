using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

namespace Sga.Web.Pages.Cliente;

public class DocumentosModel(SgaDb db, IReloj reloj) : PaginaCliente(db)
{
    public short Ejercicio { get; private set; }
    public string Periodo { get; private set; } = "3T";
    public IReadOnlyList<RequisitoDocumental> Requisitos { get; private set; } = [];
    public string Frase { get; private set; } = "";
    public int Recibidos => Requisitos.Sum(r => Math.Min(r.Recibidos, r.Esperados));
    public int Esperados => Requisitos.Sum(r => r.Esperados);
    public List<Documento> Documentos { get; private set; } = [];

    public async Task OnGetAsync(string? periodo)
    {
        var (ej, per) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        Ejercicio = ej; Periodo = periodo is "1T" or "2T" or "3T" or "4T" ? periodo : per;
        Documentos = await Db.Documentos.AsNoTracking().Where(d => d.ClienteId == ClienteId && d.Ejercicio == Ejercicio && d.Periodo == Periodo).OrderByDescending(d => d.SubidoUtc).ToListAsync();
        Requisitos = RequisitosDocumentales.DelTrimestre(Cliente, Documentos);
        Frase = RequisitosDocumentales.Frase(Requisitos);
    }
}
