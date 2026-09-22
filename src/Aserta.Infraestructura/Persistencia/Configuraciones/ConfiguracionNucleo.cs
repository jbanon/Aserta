using Aserta.Dominio.Nucleo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aserta.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionGestoria : IEntityTypeConfiguration<Gestoria>
{
    public void Configure(EntityTypeBuilder<Gestoria> b)
    {
        b.ToTable("Gestoria", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        b.Property(x => x.Nif).HasMaxLength(9).IsRequired();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.FechaAlta).HasColumnType("date");
        b.Property(x => x.ZonaHoraria).HasMaxLength(64).IsRequired();
        b.Property(x => x.ExigeAprobacionCliente);
        b.Property(x => x.AsesorVeTodosLosClientes);
    }
}

public sealed class ConfiguracionUsuario : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("Usuario", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.NombreCompleto).HasMaxLength(200).IsRequired();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Ignore(x => x.EsDelLadoCliente);
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionAuditoria : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> b)
    {
        b.ToTable("Auditoria", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.FechaUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.Accion).HasMaxLength(50).IsRequired();
        b.Property(x => x.EntidadTipo).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntidadId).HasMaxLength(64).IsRequired();
        b.Property(x => x.Detalle);
        b.Property(x => x.DireccionIp).HasMaxLength(45);
        b.Property(x => x.AgenteUsuario).HasMaxLength(500);
    }
}

public sealed class ConfiguracionEjecucionProgramada : IEntityTypeConfiguration<EjecucionProgramada>
{
    public void Configure(EntityTypeBuilder<EjecucionProgramada> b)
    {
        b.ToTable("EjecucionProgramada", "dbo");
        b.HasKey(x => x.Tarea);
        b.Property(x => x.Tarea).HasMaxLength(100);
        b.Property(x => x.UltimaEjecucionUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.ProximaEjecucionUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.Estado).HasMaxLength(30).IsRequired();
    }
}

public sealed class ConfiguracionMigracionAplicada : IEntityTypeConfiguration<MigracionAplicada>
{
    public void Configure(EntityTypeBuilder<MigracionAplicada> b)
    {
        b.ToTable("MigracionAplicada", "dbo");
        b.HasKey(x => x.Nombre);
        b.Property(x => x.Nombre).HasMaxLength(200);
        b.Property(x => x.HashSha256).HasColumnType("char(64)").IsRequired();
        b.Property(x => x.FechaAplicacionUtc).HasColumnType("datetime2(3)");
    }
}
