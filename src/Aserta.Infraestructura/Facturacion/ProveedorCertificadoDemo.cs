using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Facturacion;
using Aserta.Infraestructura.Persistencia;
using Aserta.Verifactu.Servicios;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Infraestructura.Facturacion;

/// <summary>
/// ADR-002 §3.1, resolucion en cascada por obligado: (1) certificado propio del
/// obligado vigente; (2) certificado de la gestoria vigente + apoderamiento del
/// cliente vigente; (3) ninguno => BLOQUEADO_SIN_CERTIFICADO. Nunca cae al
/// certificado del productor. Cada resolucion queda en vf.AccesoCertificadoLog.
/// En la demo el simulador no valida certificados: solo se resuelve la cascada.
/// </summary>
public sealed class ProveedorCertificadoDemo : IProveedorCertificado
{
    private readonly AsertaDbContext _db;
    private readonly IRelojSistema _reloj;
    private readonly IContextoUsuarioActual _usuario;

    public ProveedorCertificadoDemo(AsertaDbContext db, IRelojSistema reloj, IContextoUsuarioActual usuario)
    {
        _db = db;
        _reloj = reloj;
        _usuario = usuario;
    }

    public async Task<CertificadoResuelto?> ObtenerAsync(Guid gestoriaId, string nifObligado, Guid clienteObligadoId, CancellationToken ct = default)
    {
        var hoy = _reloj.Hoy;
        var certificados = await _db.Certificados.AsNoTracking().Where(c => c.GestoriaId == gestoriaId && c.Estado == EstadoCertificado.Vigente).ToListAsync(ct);

        var propio = certificados.FirstOrDefault(c => c.Tipo == TipoCertificado.Obligado && c.NifTitular == nifObligado && c.VigenteEn(hoy));
        if (propio is not null) return await ResolverAsync(propio, nifObligado, "Certificado propio del obligado", ct);

        var deGestoria = certificados.FirstOrDefault(c => c.Tipo == TipoCertificado.Gestoria && c.VigenteEn(hoy));
        if (deGestoria is null) return null;
        bool apoderado = (await _db.Apoderamientos.AsNoTracking().Where(a => a.ClienteId == clienteObligadoId).ToListAsync(ct)).Any(a => a.VigenteEn(hoy));
        if (!apoderado) return null;
        return await ResolverAsync(deGestoria, nifObligado, "Certificado de la gestoría con apoderamiento", ct);
    }

    private async Task<CertificadoResuelto> ResolverAsync(Certificado c, string nifObligado, string motivo, CancellationToken ct)
    {
        _db.AccesosCertificado.Add(new AccesoCertificadoLog { GestoriaId = c.GestoriaId, CertificadoId = c.Id, UsuarioId = _usuario.UsuarioId, FechaUtc = _reloj.AhoraUtc, Motivo = motivo, NifObligado = nifObligado });
        await _db.SaveChangesAsync(ct);
        return new CertificadoResuelto(c.Id, c.Tipo, c.HuellaDigital, c.NifTitular);
    }
}
