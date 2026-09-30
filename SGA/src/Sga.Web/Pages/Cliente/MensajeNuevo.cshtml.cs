using Microsoft.AspNetCore.Mvc;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;
using Sga.Web.Datos;

using Sga.Web.Infraestructura;

namespace Sga.Web.Pages.Cliente;

public class MensajeNuevoModel(SgaDb db, IReloj reloj) : PaginaCliente(db)
{
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync(string titulo, string texto)
    {
        if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(texto)) { AvisoError = "Escribe un asunto y tu consulta."; return Page(); }
        var inc = new Incidencia { ClienteId = ClienteId, Tipo = TipoIncidencia.Consulta, Titulo = titulo.Trim(), CreadaUtc = reloj.AhoraUtc, GestorId = Cliente.GestorId };
        inc.Mensajes.Add(new Mensaje { Autor = User.NombreActual(), EsGestor = false, Texto = texto.Trim(), FechaUtc = reloj.AhoraUtc, LeidoPorCliente = true });
        Db.Incidencias.Add(inc);
        await Db.SaveChangesAsync();
        AvisoOk = "Consulta enviada.";
        return Redirect($"/cliente/mensajes/{inc.Id}");
    }
}
