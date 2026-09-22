using Aserta.Aplicacion.Documental;
using Aserta.Aplicacion.Nucleo;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Facturacion;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Aplicacion.CuadroMando;

public sealed record ClienteIncompleto(Guid ClienteId, string Nombre, int Faltantes, string Ejemplo);
public sealed record ProductividadAsesor(Guid AsesorId, string Nombre, int Presentadas30d, int Cerradas30d, int Abiertas, int Vencidas);
public sealed record AlertaCuadro(string Nivel, string Texto, string Enlace);

public sealed record CuadroMando(
    DateOnly Hoy,
    IReadOnlyDictionary<EstadoObligacion, int> PorEstado, int Abiertas, int VencenSemana, int Vencidas, int PresentadasMes,
    IReadOnlyList<Obligacion> VencimientosSemana, IReadOnlyDictionary<Guid, string> NombresClientes,
    IReadOnlyList<ClienteIncompleto> ClientesIncompletos, int ClientesActivos,
    IReadOnlyList<ProductividadAsesor> Productividad,
    int VfEnviados, int VfAceptados, int VfPendientes, int VfErrores, int VfFacturasMes, decimal VfImporteMes,
    int DocumentosPendientesRevisar, int MensajesSinLeer, IReadOnlyList<AlertaCuadro> Alertas);

/// <summary>M7: el cuadro de mando del socio, coherente con todo lo anterior (criterio de exito 7).</summary>
public sealed class ServicioCuadroMando
{
    private readonly IAsertaDb _db;
    private readonly IRelojSistema _reloj;
    private readonly ServicioRequisitos _requisitos;
    private readonly ServicioUsuarios _usuarios;

    public ServicioCuadroMando(IAsertaDb db, IRelojSistema reloj, ServicioRequisitos requisitos, ServicioUsuarios usuarios)
    {
        _db = db;
        _reloj = reloj;
        _requisitos = requisitos;
        _usuarios = usuarios;
    }

    public async Task<CuadroMando> ObtenerAsync(CancellationToken ct = default)
    {
        var hoy = _reloj.Hoy;
        var ahora = _reloj.AhoraUtc;
        var clientes = await _db.Clientes.AsNoTracking().Where(c => c.Estado == EstadoCliente.Activo).ToListAsync(ct);
        var nombres = clientes.ToDictionary(c => c.Id, c => c.NombreComercial ?? c.RazonSocial);
        var asesores = await _usuarios.ListarDeGestoriaAsync(ct);

        var obligaciones = await _db.Obligaciones.AsNoTracking().Where(o => o.Estado != EstadoObligacion.NoAplica && (o.Ejercicio == hoy.Year || o.Ejercicio == hoy.Year - 1)).ToListAsync(ct);
        var abiertas = obligaciones.Where(o => !o.Estado.EsFinalOPresentado()).ToList();
        var porEstado = EstadoObligacionExtensiones.ColumnasKanban.ToDictionary(e => e, e => obligaciones.Count(o => o.Estado == e && (o.Ejercicio == hoy.Year || !o.Estado.EsFinalOPresentado())));
        var finSemana = hoy.AddDays(7);
        var vencimientosSemana = abiertas.Where(o => { var f = Semaforo.FechaDeReferencia(o); return f >= hoy && f <= finSemana; }).OrderBy(Semaforo.FechaDeReferencia).ToList();
        var vencidas = abiertas.Where(o => Semaforo.DiasRestantes(o, hoy) < 0).ToList();
        var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        var presentadasMes = await _db.ObligacionHistoriales.AsNoTracking().CountAsync(h => h.TipoEvento == TiposEventoHistorial.CambioEstado && h.EstadoNuevo == EstadoObligacion.Presentado && h.FechaUtc >= inicioMes.ToDateTime(TimeOnly.MinValue), ct);

        // Clientes con documentacion incompleta (ultimos 6 meses)
        var incompletos = new List<ClienteIncompleto>();
        foreach (var c in clientes)
        {
            var estados = await _requisitos.EstadoAsync(c.Id, null, ct);
            var pendientes = estados.Where(e => !e.Completo && e.Requisito.Obligatorio && Periodo.Rango(e.Requisito.Ejercicio, e.Requisito.Periodo).Inicio >= hoy.AddMonths(-6)).ToList();
            int faltan = CompletitudDocumental.TotalFaltantes(pendientes);
            if (faltan > 0) incompletos.Add(new ClienteIncompleto(c.Id, nombres[c.Id], faltan, pendientes.OrderBy(p => Periodo.Orden(p.Requisito.Periodo)).First().Frase));
        }
        incompletos = incompletos.OrderByDescending(i => i.Faltantes).ToList();

        // Productividad por asesor (30 dias)
        var desde = ahora.AddDays(-30);
        var eventos = await _db.ObligacionHistoriales.AsNoTracking().Where(h => h.TipoEvento == TiposEventoHistorial.CambioEstado && h.FechaUtc >= desde && h.UsuarioId != null && (h.EstadoNuevo == EstadoObligacion.Presentado || h.EstadoNuevo == EstadoObligacion.Cerrado)).ToListAsync(ct);
        var productividad = asesores.Where(a => a.Estado == EstadoUsuario.Activo).Select(a => new ProductividadAsesor(a.Id, a.NombreCompleto,
            eventos.Count(e => e.UsuarioId == a.Id && e.EstadoNuevo == EstadoObligacion.Presentado), eventos.Count(e => e.UsuarioId == a.Id && e.EstadoNuevo == EstadoObligacion.Cerrado),
            abiertas.Count(o => o.AsesorId == a.Id), vencidas.Count(o => o.AsesorId == a.Id))).OrderByDescending(p => p.Presentadas30d).ToList();

        // Veri*Factu
        var estadosVf = await _db.EstadosEnvio.AsNoTracking().ToListAsync(ct);
        int vfAceptados = estadosVf.Count(e => e.Estado == EstadoEnvio.ACEPTADO);
        int vfPendientes = estadosVf.Count(e => e.Estado is EstadoEnvio.EN_COLA or EstadoEnvio.ENVIADO or EstadoEnvio.ERROR_TECNICO or EstadoEnvio.GENERADO);
        int vfErrores = estadosVf.Count(e => e.Estado is EstadoEnvio.ACEPTADO_CON_ERRORES or EstadoEnvio.RECHAZADO or EstadoEnvio.DEAD_LETTER or EstadoEnvio.ERROR_VALIDACION or EstadoEnvio.BLOQUEADO_SIN_CERTIFICADO);
        var facturasMes = await _db.FacturasEmitidas.AsNoTracking().Where(f => f.FechaExpedicion >= inicioMes).ToListAsync(ct);

        int docsPendientes = await _db.Documentos.CountAsync(d => d.Estado == EstadoDocumento.Recibido || d.Estado == EstadoDocumento.EnRevision, ct);
        int mensajes = await _db.Mensajes.CountAsync(m => m.LeidoPorGestoriaUtc == null, ct);

        var alertas = new List<AlertaCuadro>();
        int deadLetter = estadosVf.Count(e => e.Estado == EstadoEnvio.DEAD_LETTER), conErrores = estadosVf.Count(e => e.Estado == EstadoEnvio.ACEPTADO_CON_ERRORES), bloqueados = estadosVf.Count(e => e.Estado == EstadoEnvio.BLOQUEADO_SIN_CERTIFICADO);
        if (conErrores > 0) alertas.Add(new("error", $"{conErrores} registros Veri*Factu aceptados con errores: posible fallo estructural de la huella.", "/Facturacion/Envios?estado=ACEPTADO_CON_ERRORES"));
        if (deadLetter > 0) alertas.Add(new("error", $"{deadLetter} envíos a la AEAT con los reintentos agotados.", "/Facturacion/Envios?estado=DEAD_LETTER"));
        if (bloqueados > 0) alertas.Add(new("aviso", $"{bloqueados} registros bloqueados sin certificado ni apoderamiento.", "/Facturacion/Certificados"));
        foreach (var c in await _db.Certificados.AsNoTracking().Where(c => c.Estado == EstadoCertificado.Vigente).ToListAsync(ct))
        {
            int dias = c.DiasParaCaducar(hoy);
            if (dias <= 60) alertas.Add(new(dias <= 7 ? "error" : "aviso", $"El certificado «{c.Alias}» caduca en {dias} días ({c.ValidoHasta:dd/MM/yyyy}).", "/Facturacion/Certificados"));
        }
        if (vencidas.Count > 0) alertas.Add(new("error", $"{vencidas.Count} obligaciones vencidas sin presentar.", "/Obligaciones/Tablero"));
        int sinConfirmar = await _db.PlazosModelo.CountAsync(p => !p.Confirmado && p.Ejercicio == hoy.Year, ct);
        if (sinConfirmar > 0) alertas.Add(new("aviso", $"{sinConfirmar} plazos del ejercicio {hoy.Year} sin confirmar contra el calendario de la AEAT.", "/Catalogo?seccion=plazos"));

        return new CuadroMando(hoy, porEstado, abiertas.Count, vencimientosSemana.Count, vencidas.Count, presentadasMes, vencimientosSemana.Take(12).ToList(), nombres,
            incompletos.Take(8).ToList(), clientes.Count, productividad, estadosVf.Count, vfAceptados, vfPendientes, vfErrores, facturasMes.Count, facturasMes.Sum(f => f.ImporteTotal),
            docsPendientes, mensajes, alertas);
    }
}
