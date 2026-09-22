using Aserta.Dominio.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aserta.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionModeloTributario : IEntityTypeConfiguration<ModeloTributario>
{
    public void Configure(EntityTypeBuilder<ModeloTributario> b)
    {
        b.ToTable("ModeloTributario", "cat");
        b.HasKey(x => x.Codigo);
        b.Property(x => x.Codigo).HasMaxLength(10);
        b.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(1000).IsRequired();
        b.Property(x => x.DescripcionCliente).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Periodicidad).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.EsResumenAnualDe).HasMaxLength(10);
        b.Property(x => x.Organismo).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.HasOne<ModeloTributario>().WithMany().HasForeignKey(x => x.EsResumenAnualDe).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionPlazoModelo : IEntityTypeConfiguration<PlazoModelo>
{
    public void Configure(EntityTypeBuilder<PlazoModelo> b)
    {
        b.ToTable("PlazoModelo", "cat");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.ModeloCodigo).HasMaxLength(10).IsRequired();
        b.Property(x => x.Ejercicio).HasColumnType("smallint");
        b.Property(x => x.Periodo).HasMaxLength(2).IsRequired();
        b.Property(x => x.FechaInicioPresentacion).HasColumnType("date");
        b.Property(x => x.FechaLimiteDomiciliacion).HasColumnType("date");
        b.Property(x => x.FechaLimitePresentacion).HasColumnType("date");
        b.Property(x => x.Fuente).HasMaxLength(300).IsRequired();
        b.HasOne<ModeloTributario>().WithMany().HasForeignKey(x => x.ModeloCodigo).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionReglaObligacion : IEntityTypeConfiguration<ReglaObligacion>
{
    public void Configure(EntityTypeBuilder<ReglaObligacion> b)
    {
        b.ToTable("ReglaObligacion", "cat");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Clave).HasMaxLength(60).IsRequired();
        b.Property(x => x.ModeloCodigo).HasMaxLength(10).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(300).IsRequired();
        b.Property(x => x.Periodicidad).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.VigenteDesdeEjercicio).HasColumnType("smallint");
        b.Property(x => x.VigenteHastaEjercicio).HasColumnType("smallint");
        b.HasOne<ModeloTributario>().WithMany().HasForeignKey(x => x.ModeloCodigo).OnDelete(DeleteBehavior.NoAction);
        b.HasMany(x => x.Condiciones).WithOne().HasForeignKey(c => c.ReglaId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ConfiguracionReglaCondicion : IEntityTypeConfiguration<ReglaCondicion>
{
    public void Configure(EntityTypeBuilder<ReglaCondicion> b)
    {
        b.ToTable("ReglaCondicion", "cat");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Atributo).HasMaxLength(60).IsRequired();
        b.Property(x => x.Operador).HasMaxLength(4).IsRequired();
        b.Property(x => x.Valor).HasMaxLength(200).IsRequired();
    }
}

public sealed class ConfiguracionDiaInhabil : IEntityTypeConfiguration<DiaInhabil>
{
    public void Configure(EntityTypeBuilder<DiaInhabil> b)
    {
        b.ToTable("DiaInhabil", "cat");
        b.HasKey(x => new { x.Fecha, x.Ambito });
        b.Property(x => x.Fecha).HasColumnType("date");
        b.Property(x => x.Ambito).HasMaxLength(50);
        b.Property(x => x.Descripcion).HasMaxLength(200).IsRequired();
    }
}
