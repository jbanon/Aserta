using Aserta.Dominio.Clientes;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Exportacion;
using Aserta.Dominio.Facturacion;
using Aserta.Dominio.Nucleo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aserta.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionExportacionContable : IEntityTypeConfiguration<ExportacionContable>
{
    public void Configure(EntityTypeBuilder<ExportacionContable> b)
    {
        b.ToTable("ExportacionContable", "dbo");
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Formato).HasMaxLength(30).IsRequired();
        b.Property(x => x.Ejercicio).HasColumnType("smallint");
        b.Property(x => x.Periodo).HasMaxLength(2).IsRequired();
        b.Property(x => x.FechaGeneracionUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.NombreFichero).HasMaxLength(200).IsRequired();
        b.Property(x => x.HashSha256).HasColumnType("char(64)").IsRequired();
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionExportacionDocumento : IEntityTypeConfiguration<ExportacionDocumento>
{
    public void Configure(EntityTypeBuilder<ExportacionDocumento> b)
    {
        b.ToTable("ExportacionDocumento", "dbo");
        b.HasKey(x => new { x.ExportacionId, x.DocumentoId });
        b.HasOne<ExportacionContable>().WithMany().HasForeignKey(x => x.ExportacionId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Documento>().WithMany().HasForeignKey(x => x.DocumentoId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionExportacionFactura : IEntityTypeConfiguration<ExportacionFactura>
{
    public void Configure(EntityTypeBuilder<ExportacionFactura> b)
    {
        b.ToTable("ExportacionFactura", "dbo");
        b.HasKey(x => new { x.ExportacionId, x.FacturaEmitidaId });
        b.HasOne<ExportacionContable>().WithMany().HasForeignKey(x => x.ExportacionId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<FacturaEmitida>().WithMany().HasForeignKey(x => x.FacturaEmitidaId).OnDelete(DeleteBehavior.NoAction);
    }
}
