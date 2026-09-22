using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Facturacion;
using Aserta.Dominio.Nucleo;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Aplicacion.CuadroMando;

/// <summary>AlertaCaducidadCertificadosWorker (ADR-001 §2.5, ADR-002 §3.2): aviso a los socios a 60/30/7 dias, exactamente uno por certificado y umbral.</summary>
public sealed class ServicioAlertaCertificados : ITareaDiaria
{
    private readonly IAsertaDb _db;
    private readonly INotificador _notificador;
    private readonly IRelojSistema _reloj;

    public ServicioAlertaCertificados(IAsertaDb db, INotificador notificador, IRelojSistema reloj)
    {
        _db = db;
        _notificador = notificador;
        _reloj = reloj;
    }

    public string Nombre => "AlertaCaducidadCertificados";

    public async Task EjecutarParaTenantAsync(CancellationToken ct) => await GenerarAsync(ct);

    public async Task<int> GenerarAsync(CancellationToken ct = default)
    {
        var hoy = _reloj.Hoy;
        var certificados = await _db.Certificados.AsNoTracking().Where(c => c.Estado == EstadoCertificado.Vigente).ToListAsync(ct);
        var socios = await _db.Usuarios.AsNoTracking().Where(u => u.ClienteId == null && u.Estado == EstadoUsuario.Activo).Select(u => u.Id).ToListAsync(ct);
        int n = 0;
        foreach (var c in certificados)
        {
            int dias = c.DiasParaCaducar(hoy);
            int? umbral = dias <= 7 ? 7 : dias <= 30 ? 30 : dias <= 60 ? 60 : null;
            if (umbral is null) continue;
            foreach (var s in socios)
                if (await _notificador.NotificarAsync(new Notificacion(s, TiposAviso.Sistema, $"El certificado «{c.Alias}» caduca en {dias} días", $"Válido hasta {c.ValidoHasta:dd/MM/yyyy}. Si es el de la gestoría, su caducidad detiene la facturación de toda la cartera.", nameof(Certificado), c.Id.ToString(), $"certificado:{c.Id}:{umbral}"), ct)) n++;
        }
        if (n > 0) await _db.GuardarCambiosAsync(ct);
        return n;
    }
}
