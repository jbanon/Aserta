using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Modelo;

namespace Sga.Web.Datos;

/// <summary>
/// Base de datos SGA (SQL Server). Decision documentada en SGA/docs/decisiones.md: para la demo el esquema
/// lo crea EF Core (EnsureCreated) y se recrea con Demo:Reiniciar, en lugar de los scripts numerados de Aserta.
/// </summary>
public sealed class SgaDb(DbContextOptions<SgaDb> opciones) : DbContext(opciones)
{
    public DbSet<Gestor> Gestores => Set<Gestor>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Actividad> Actividades => Set<Actividad>();
    public DbSet<ContratoAlquiler> Contratos => Set<ContratoAlquiler>();
    public DbSet<Obligacion> Obligaciones => Set<Obligacion>();
    public DbSet<Presentacion> Presentaciones => Set<Presentacion>();
    public DbSet<FacturaEmitida> FacturasEmitidas => Set<FacturaEmitida>();
    public DbSet<FacturaRecibida> FacturasRecibidas => Set<FacturaRecibida>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<MovimientoBancario> Movimientos => Set<MovimientoBancario>();
    public DbSet<Incidencia> Incidencias => Set<Incidencia>();
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();
    public DbSet<FacturaHonorarios> Honorarios => Set<FacturaHonorarios>();
    public DbSet<Aviso> Avisos => Set<Aviso>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        foreach (var p in mb.Model.GetEntityTypes().SelectMany(e => e.GetProperties()).Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            p.SetColumnType("decimal(14,2)");
        foreach (var p in mb.Model.GetEntityTypes().SelectMany(e => e.GetProperties()).Where(p => p.ClrType == typeof(string) && p.GetMaxLength() is null))
            p.SetMaxLength(400);

        mb.Entity<Gestor>().ToTable("Gestor");
        mb.Entity<Cliente>(e =>
        {
            e.ToTable("Cliente");
            e.HasIndex(c => c.Nif).IsUnique();
            e.HasOne(c => c.Gestor).WithMany().HasForeignKey(c => c.GestorId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(c => c.Actividades).WithOne().HasForeignKey(a => a.ClienteId);
            e.HasMany(c => c.Contratos).WithOne().HasForeignKey(a => a.ClienteId);
            e.Ignore(c => c.Iniciales);
        });
        mb.Entity<Actividad>().ToTable("Actividad");
        mb.Entity<ContratoAlquiler>().ToTable("ContratoAlquiler");
        mb.Entity<Obligacion>(e =>
        {
            e.ToTable("Obligacion");
            e.HasIndex(o => new { o.ClienteId, o.Ejercicio, o.Periodo, o.Modelo }).IsUnique();
            e.Property(o => o.Periodo).HasMaxLength(2);
            e.Property(o => o.Modelo).HasMaxLength(10);
            e.HasOne(o => o.Cliente).WithMany().HasForeignKey(o => o.ClienteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(o => o.Gestor).WithMany().HasForeignKey(o => o.GestorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Presentacion).WithOne().HasForeignKey<Presentacion>(p => p.ObligacionId);
        });
        mb.Entity<Presentacion>().ToTable("Presentacion");
        mb.Entity<FacturaEmitida>(e => { e.ToTable("FacturaEmitida"); e.HasIndex(f => new { f.ClienteId, f.Fecha }); });
        mb.Entity<FacturaRecibida>(e => { e.ToTable("FacturaRecibida"); e.HasIndex(f => new { f.ClienteId, f.Fecha }); });
        mb.Entity<Documento>(e => { e.ToTable("Documento"); e.HasIndex(d => new { d.ClienteId, d.Ejercicio, d.Periodo }); });
        mb.Entity<MovimientoBancario>(e => { e.ToTable("MovimientoBancario"); e.HasIndex(m => new { m.ClienteId, m.Fecha }); });
        mb.Entity<Incidencia>(e =>
        {
            e.ToTable("Incidencia");
            e.HasOne(i => i.Cliente).WithMany().HasForeignKey(i => i.ClienteId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(i => i.Mensajes).WithOne().HasForeignKey(m => m.IncidenciaId);
        });
        mb.Entity<Mensaje>(e => { e.ToTable("Mensaje"); e.Property(m => m.Texto).HasMaxLength(4000); });
        mb.Entity<FacturaHonorarios>().ToTable("FacturaHonorarios");
        mb.Entity<Aviso>().ToTable("Aviso");
    }
}
