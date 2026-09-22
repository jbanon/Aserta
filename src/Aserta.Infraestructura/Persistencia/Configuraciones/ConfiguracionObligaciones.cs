using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aserta.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionObligacion : IEntityTypeConfiguration<Obligacion>
{
    public void Configure(EntityTypeBuilder<Obligacion> b)
    {
        b.ToTable("Obligacion", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.ModeloCodigo).HasMaxLength(10).IsRequired();
        b.Property(x => x.Ejercicio).HasColumnType("smallint");
        b.Property(x => x.Periodo).HasMaxLength(2).IsRequired();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.FechaLimiteDomiciliacion).HasColumnType("date");
        b.Property(x => x.FechaLimitePresentacion).HasColumnType("date");
        b.Property(x => x.ImporteResultado).HasColumnType("decimal(18,2)");
        b.Property(x => x.SignoResultado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.FechaAprobacionClienteUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.FechaGeneracionUtc).HasColumnType("datetime2(3)");
        b.Ignore(x => x.EtiquetaPeriodo);
        b.Ignore(x => x.Titulo);
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<ModeloTributario>().WithMany().HasForeignKey(x => x.ModeloCodigo).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AsesorId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioAprobacionId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<ReglaObligacion>().WithMany().HasForeignKey(x => x.ReglaOrigenId).OnDelete(DeleteBehavior.NoAction);
        b.HasMany(x => x.Historial).WithOne().HasForeignKey(h => h.ObligacionId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.GestoriaId, x.ClienteId, x.ModeloCodigo, x.Ejercicio, x.Periodo }).IsUnique().HasDatabaseName("UX_Obligacion");
    }
}

public sealed class ConfiguracionObligacionHistorial : IEntityTypeConfiguration<ObligacionHistorial>
{
    public void Configure(EntityTypeBuilder<ObligacionHistorial> b)
    {
        b.ToTable("ObligacionHistorial", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.FechaUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.TipoEvento).HasMaxLength(40).IsRequired();
        b.Property(x => x.EstadoAnterior).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.EstadoNuevo).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Comentario).HasMaxLength(1000);
        b.Property(x => x.ReferenciaId).HasMaxLength(64);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
    }
}
