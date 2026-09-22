using Aserta.Aplicacion.Mensajeria;
using Aserta.Aplicacion.PortalCliente;
using Aserta.Dominio.Mensajeria;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Authorization;

namespace Aserta.Web.Pages.Portal;

[Authorize(Policy = Politicas.Cliente)]
public class MensajesModel : PaginaBase
{
    private readonly ServicioMensajeria _mensajeria;
    private readonly ServicioPortal _portal;

    public MensajesModel(ServicioMensajeria mensajeria, ServicioPortal portal)
    {
        _mensajeria = mensajeria;
        _portal = portal;
    }

    public List<Hilo> Hilos { get; private set; } = [];

    public async Task OnGetAsync() => Hilos = await _mensajeria.DeClienteAsync(_portal.ClienteId);
}
