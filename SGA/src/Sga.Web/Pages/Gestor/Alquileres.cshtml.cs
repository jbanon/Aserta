using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Gestor;

public class AlquileresModel(SgaDb db, IReloj reloj, ServicioFacturacion facturacion) : PaginaGestor(db)
{
    public short Ejercicio; public string Periodo = "3T";
    public sealed record Bloque(Sga.Nucleo.Modelo.Cliente Cliente, List<FacturaEmitida> Facturas, int Esperadas);
    public List<Bloque> Bloques { get; private set; } = [];

    public async Task OnGetAsync(string? periodo)
    {
        (Ejercicio, Periodo) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        if (periodo is "1T" or "2T" or "3T" or "4T") Periodo = periodo;
        var (ini, fin) = Aserta.Dominio.Catalogo.Periodo.Rango(Ejercicio, Periodo);
        var arrendadores = await Db.Clientes.AsNoTracking().Include(c => c.Contratos).Where(c => c.Tipo == TipoCliente.Arrendador).OrderBy(c => c.NombreCorto).ToListAsync();
        foreach (var c in arrendadores)
        {
            var facturas = await Db.FacturasEmitidas.AsNoTracking().Where(f => f.ClienteId == c.Id && f.Fecha >= ini && f.Fecha <= fin).OrderBy(f => f.Fecha).ThenBy(f => f.ContratoId).ToListAsync();
            Bloques.Add(new Bloque(c, facturas, c.Contratos.Count(k => k.Activo) * 3));
        }
    }

    public async Task<IActionResult> OnPostGenerarAsync(int clienteId, string periodo)
    {
        var (ej, _) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        var generadas = await facturacion.GenerarBloqueTrimestreAsync(clienteId, ej, periodo);
        AvisoOk = generadas.Count == 0 ? "No había nada que generar." : $"Generadas {generadas.Count} facturas ({string.Join(", ", generadas.Select(f => f.Numero))}), registradas en Monitor. Ya se pueden enviar.";
        return Redirect($"/gestor/alquileres?periodo={periodo}#cliente-{clienteId}");
    }

    public async Task<IActionResult> OnPostEnviarAsync(int facturaId, string periodo)
    {
        await facturacion.EnviarAsync(facturaId);
        AvisoOk = "Factura enviada al inquilino con copia al cliente (simulación de correo).";
        return Redirect($"/gestor/alquileres?periodo={periodo}");
    }

    public async Task<IActionResult> OnPostEnviarTodasAsync(int clienteId, string periodo)
    {
        var (ej, _) = CalendarioFiscal.TrimestreEnCampana(reloj.Hoy);
        var (ini, fin) = Aserta.Dominio.Catalogo.Periodo.Rango(ej, periodo);
        var ids = await Db.FacturasEmitidas.Where(f => f.ClienteId == clienteId && f.Fecha >= ini && f.Fecha <= fin && f.Estado == EstadoFacturaEmitida.Generada).Select(f => f.Id).ToListAsync();
        foreach (var id in ids) await facturacion.EnviarAsync(id);
        AvisoOk = $"{ids.Count} facturas enviadas al inquilino con copia al cliente.";
        return Redirect($"/gestor/alquileres?periodo={periodo}#cliente-{clienteId}");
    }
}
