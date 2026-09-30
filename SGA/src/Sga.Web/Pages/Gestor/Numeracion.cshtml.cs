using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;
using Sga.Web.Servicios;

namespace Sga.Web.Pages.Gestor;

public class NumeracionModel(SgaDb db, IReloj reloj, ServicioFacturacion facturacion) : PaginaGestor(db)
{
    public Sga.Nucleo.Modelo.Cliente Cliente { get; private set; } = default!;
    public short Ejercicio;
    public InformeNumeracion Informe { get; private set; } = default!;
    public List<FacturaEmitida> Facturas { get; private set; } = [];
    public bool IncidenciaAbierta; public int IncidenciaId;

    private async Task<bool> CargarAsync(int id)
    {
        var c = await Db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return false;
        Cliente = c; Ejercicio = (short)reloj.Hoy.Year;
        Informe = await facturacion.ComprobarNumeracionAsync(id, Ejercicio);
        Facturas = await Db.FacturasEmitidas.AsNoTracking().Where(f => f.ClienteId == id && f.Fecha.Year == Ejercicio && f.Origen == OrigenFactura.Cliente).OrderBy(f => f.Numero).ThenBy(f => f.Fecha).ToListAsync();
        var inc = await Db.Incidencias.AsNoTracking().FirstOrDefaultAsync(i => i.ClienteId == id && i.Tipo == TipoIncidencia.Numeracion && i.Estado != EstadoIncidencia.Resuelta);
        IncidenciaAbierta = inc is not null; IncidenciaId = inc?.Id ?? 0;
        return true;
    }

    public async Task<IActionResult> OnGetAsync(int id) => await CargarAsync(id) ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await CargarAsync(id)) return NotFound();
        if (Informe.Correcta || IncidenciaAbierta) return Redirect($"/gestor/clientes/{id}/numeracion");
        var texto = "Hola. Al revisar tus facturas del año veo: " + string.Join(" ", Informe.Anomalias.Select(a => $"{a.Numero}: {a.Detalle}")) + " ¿Puedes mirarlo? Si alguna se anuló, dímelo y lo anoto.";
        var inc = new Incidencia { ClienteId = id, Tipo = TipoIncidencia.Numeracion, Titulo = $"Numeración de facturas: {Informe.Huecos} huecos, {Informe.Duplicados} duplicados", CreadaUtc = reloj.AhoraUtc, GestorId = GestorId };
        inc.Mensajes.Add(new Mensaje { Autor = Gestor.Nombre, EsGestor = true, Texto = texto, FechaUtc = reloj.AhoraUtc, LeidoPorGestor = true });
        Db.Incidencias.Add(inc);
        Db.Avisos.Add(new Aviso { ClienteId = id, FechaUtc = reloj.AhoraUtc, Texto = $"{Gestor.Nombre.Split(' ')[0]} te ha escrito sobre la numeración de tus facturas.", Enlace = "/cliente/mensajes" });
        await Db.SaveChangesAsync();
        AvisoOk = "Aviso enviado al cliente.";
        return Redirect($"/gestor/clientes/{id}/numeracion");
    }
}
