using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
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
    }
}
