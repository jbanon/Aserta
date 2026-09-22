using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Exportacion;
using Aserta.Dominio.Facturacion;
using Aserta.Dominio.Mensajeria;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Aserta.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Infraestructura.Persistencia;

/// <summary>
/// Unidad de trabajo. El esquema lo definen los scripts de Scripts/Migrations;
/// este modelo se mantiene alineado A MANO y DerivaDeEsquemaTests vigila la deriva.
/// Primera barrera multi-tenant: HasQueryFilter por GestoriaId en toda entidad con tenant.
/// </summary>
public sealed class AsertaDbContext : IdentityDbContext<UsuarioIdentity, RolIdentity, Guid>, IAsertaDb
{
    private readonly IContextoTenant _tenant;

    public AsertaDbContext(DbContextOptions<AsertaDbContext> opciones, IContextoTenant tenant) : base(opciones)
    {
        _tenant = tenant;
    }

    public DbSet<Gestoria> Gestorias => Set<Gestoria>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<EjecucionProgramada> EjecucionesProgramadas => Set<EjecucionProgramada>();
    public DbSet<MigracionAplicada> MigracionesAplicadas => Set<MigracionAplicada>();
    public DbSet<Aviso> Avisos => Set<Aviso>();

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<PerfilFiscal> PerfilesFiscales => Set<PerfilFiscal>();

    public DbSet<ModeloTributario> ModelosTributarios => Set<ModeloTributario>();
    public DbSet<PlazoModelo> PlazosModelo => Set<PlazoModelo>();
    public DbSet<ReglaObligacion> ReglasObligacion => Set<ReglaObligacion>();
    public DbSet<ReglaCondicion> ReglasCondicion => Set<ReglaCondicion>();
    public DbSet<DiaInhabil> DiasInhabiles => Set<DiaInhabil>();

    public DbSet<Obligacion> Obligaciones => Set<Obligacion>();
    public DbSet<ObligacionHistorial> ObligacionHistoriales => Set<ObligacionHistorial>();

    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<DocumentoObligacion> DocumentosObligacion => Set<DocumentoObligacion>();
    public DbSet<RequisitoPeriodo> RequisitosPeriodo => Set<RequisitoPeriodo>();
    public DbSet<ReglaRequisito> ReglasRequisito => Set<ReglaRequisito>();
    public DbSet<Hilo> Hilos => Set<Hilo>();
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();

    public DbSet<SerieFacturacion> SeriesFacturacion => Set<SerieFacturacion>();
    public DbSet<Destinatario> Destinatarios => Set<Destinatario>();
    public DbSet<ArticuloServicio> ArticulosServicio => Set<ArticuloServicio>();
    public DbSet<CadenaEmisor> CadenasEmisor => Set<CadenaEmisor>();
    public DbSet<FacturaEmitida> FacturasEmitidas => Set<FacturaEmitida>();
    public DbSet<LineaFactura> LineasFactura => Set<LineaFactura>();
    public DbSet<RegistroFacturacion> RegistrosFacturacion => Set<RegistroFacturacion>();
    public DbSet<EstadoEnvioRegistro> EstadosEnvio => Set<EstadoEnvioRegistro>();
    public DbSet<EnvioPendiente> EnviosPendientes => Set<EnvioPendiente>();
    public DbSet<LoteEnvio> LotesEnvio => Set<LoteEnvio>();
    public DbSet<FacturaPdf> FacturasPdf => Set<FacturaPdf>();
    public DbSet<Certificado> Certificados => Set<Certificado>();
    public DbSet<Apoderamiento> Apoderamientos => Set<Apoderamiento>();
    public DbSet<AccesoCertificadoLog> AccesosCertificado => Set<AccesoCertificadoLog>();
    public DbSet<DeclaracionResponsableHistorico> DeclaracionesResponsables => Set<DeclaracionResponsableHistorico>();
    public DbSet<ExportacionContable> Exportaciones => Set<ExportacionContable>();
    public DbSet<ExportacionDocumento> ExportacionesDocumento => Set<ExportacionDocumento>();
    public DbSet<ExportacionFactura> ExportacionesFactura => Set<ExportacionFactura>();

    public Task<int> GuardarCambiosAsync(CancellationToken ct = default) => SaveChangesAsync(ct);

    // Se leen como parametros en cada consulta: cambiar el ambito cambia el filtro sin recompilar el modelo.
    private bool Mantenimiento => _tenant.EsMantenimiento;
    private Guid? GestoriaActual => _tenant.GestoriaId;

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.ApplyConfigurationsFromAssembly(typeof(AsertaDbContext).Assembly);

        // Filtros de tenant (primera barrera, RD-04)
        mb.Entity<Gestoria>().HasQueryFilter(g => Mantenimiento || g.Id == GestoriaActual);
        mb.Entity<Usuario>().HasQueryFilter(u => Mantenimiento || u.GestoriaId == GestoriaActual);
        mb.Entity<Auditoria>().HasQueryFilter(a => Mantenimiento || a.GestoriaId == GestoriaActual);
        mb.Entity<Cliente>().HasQueryFilter(c => Mantenimiento || c.GestoriaId == GestoriaActual);
        mb.Entity<PerfilFiscal>().HasQueryFilter(p => Mantenimiento || p.GestoriaId == GestoriaActual);
        mb.Entity<Obligacion>().HasQueryFilter(o => Mantenimiento || o.GestoriaId == GestoriaActual);
        mb.Entity<ObligacionHistorial>().HasQueryFilter(h => Mantenimiento || h.GestoriaId == GestoriaActual);
        mb.Entity<Aviso>().HasQueryFilter(a => Mantenimiento || a.GestoriaId == GestoriaActual);
        mb.Entity<Documento>().HasQueryFilter(d => Mantenimiento || d.GestoriaId == GestoriaActual);
        mb.Entity<DocumentoObligacion>().HasQueryFilter(d => Mantenimiento || d.GestoriaId == GestoriaActual);
        mb.Entity<RequisitoPeriodo>().HasQueryFilter(r => Mantenimiento || r.GestoriaId == GestoriaActual);
        mb.Entity<Hilo>().HasQueryFilter(h => Mantenimiento || h.GestoriaId == GestoriaActual);
        mb.Entity<Mensaje>().HasQueryFilter(m => Mantenimiento || m.GestoriaId == GestoriaActual);
        mb.Entity<SerieFacturacion>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<Destinatario>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<ArticuloServicio>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<CadenaEmisor>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<FacturaEmitida>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<LineaFactura>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<RegistroFacturacion>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<EstadoEnvioRegistro>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<EnvioPendiente>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<LoteEnvio>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<FacturaPdf>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<Certificado>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<Apoderamiento>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<AccesoCertificadoLog>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<ExportacionContable>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<ExportacionDocumento>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
        mb.Entity<ExportacionFactura>().HasQueryFilter(x => Mantenimiento || x.GestoriaId == GestoriaActual);
    }
}
