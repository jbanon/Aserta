using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Verifactu.Cliente;
using Aserta.Verifactu.Servicios;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aserta.Web.Pages.Facturacion;

[Authorize(Policy = Politicas.Gestoria)]
public class SimuladorModel : PaginaBase
{
    private readonly EstadoSimuladorAeat _estado;
    private readonly OpcionesVerifactu _opciones;
    private readonly IRegistroAuditoria _auditoria;

    public SimuladorModel(EstadoSimuladorAeat estado, OpcionesVerifactu opciones, IRegistroAuditoria auditoria)
    {
        _estado = estado;
        _opciones = opciones;
        _auditoria = auditoria;
    }

    public string Modo => _estado.Modo.ToString();
    public int LatenciaMs => _estado.LatenciaMs;
    public int TiempoEspera => _estado.TiempoEsperaSegundos;
    public string CodigoRechazo => _estado.CodigoRechazo;
    public string DescripcionRechazo => _estado.DescripcionRechazo;
    public int Lotes => _estado.LotesRecibidos;
    public int Registros => _estado.RegistrosRecibidos;
    public DateTime? UltimoLote => _estado.UltimoLoteUtc;

    public IActionResult OnGet() => _opciones.UsarSimulador ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync(ModoSimulador modo, int latenciaMs, int tiempoEspera, string? codigoRechazo, string? descripcionRechazo)
    {
        if (!_opciones.UsarSimulador) return NotFound();
        _estado.Configurar(modo, latenciaMs, tiempoEspera, codigoRechazo, descripcionRechazo);
        await _auditoria.RegistrarYGuardarAsync(AccionesAuditoria.Configuracion, "SimuladorAeat", "global", new { modo = modo.ToString(), latenciaMs, tiempoEspera });
        AvisoOk = $"Simulador en modo {modo}.";
        return RedirectToPage();
    }
}
