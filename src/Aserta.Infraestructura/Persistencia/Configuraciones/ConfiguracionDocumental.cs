using Aserta.Dominio.Clientes;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Mensajeria;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aserta.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionDocumento : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> b)
    {
        b.ToTable("Documento", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(40).IsRequired();
        b.Property(x => x.Ejercicio).HasColumnType("smallint");
        b.Property(x => x.Periodo).HasMaxLength(2).IsRequired();
        b.Property(x => x.NombreOriginal).HasMaxLength(260).IsRequired();
        b.Property(x => x.HashSha256).HasColumnType("char(64)").IsRequired();
        b.Property(x => x.TipoMime).HasMaxLength(100).IsRequired();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.MotivoRechazo).HasMaxLength(500);
        b.Property(x => x.FechaSubidaUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.FechaRevisionUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.OrigenExtraccion).HasMaxLength(40);
        b.Property(x => x.Notas).HasMaxLength(1000);
        b.Ignore(x => x.EsImagen); b.Ignore(x => x.EsPdf); b.Ignore(x => x.CuentaParaRequisitos);
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.SubidoPorId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RevisadoPorId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionDocumentoObligacion : IEntityTypeConfiguration<DocumentoObligacion>
{
    public void Configure(EntityTypeBuilder<DocumentoObligacion> b)
    {
        b.ToTable("DocumentoObligacion", "dbo");
        b.HasKey(x => new { x.DocumentoId, x.ObligacionId });
        b.HasOne<Documento>().WithMany().HasForeignKey(x => x.DocumentoId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Obligacion>().WithMany().HasForeignKey(x => x.ObligacionId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionRequisitoPeriodo : IEntityTypeConfiguration<RequisitoPeriodo>
{
    public void Configure(EntityTypeBuilder<RequisitoPeriodo> b)
    {
        b.ToTable("RequisitoPeriodo", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Ejercicio).HasColumnType("smallint");
        b.Property(x => x.Periodo).HasMaxLength(2).IsRequired();
        b.Property(x => x.TipoDocumento).HasConversion<string>().HasMaxLength(40).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(300).IsRequired();
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionReglaRequisito : IEntityTypeConfiguration<ReglaRequisito>
{
    public void Configure(EntityTypeBuilder<ReglaRequisito> b)
    {
        b.ToTable("ReglaRequisito", "cat");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Clave).HasMaxLength(60).IsRequired();
        b.Property(x => x.TipoDocumento).HasConversion<string>().HasMaxLength(40).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(300).IsRequired();
        b.Property(x => x.Periodicidad).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.HasMany(x => x.Condiciones).WithOne().HasForeignKey(c => c.ReglaId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ConfiguracionReglaRequisitoCondicion : IEntityTypeConfiguration<ReglaRequisitoCondicion>
{
    public void Configure(EntityTypeBuilder<ReglaRequisitoCondicion> b)
    {
        b.ToTable("ReglaRequisitoCondicion", "cat");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Atributo).HasMaxLength(60).IsRequired();
        b.Property(x => x.Operador).HasMaxLength(4).IsRequired();
        b.Property(x => x.Valor).HasMaxLength(200).IsRequired();
    }
}

public sealed class ConfiguracionHilo : IEntityTypeConfiguration<Hilo>
{
    public void Configure(EntityTypeBuilder<Hilo> b)
    {
        b.ToTable("Hilo", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Asunto).HasMaxLength(200).IsRequired();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.FechaUltimoMensajeUtc).HasColumnType("datetime2(3)");
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Obligacion>().WithMany().HasForeignKey(x => x.ObligacionId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Documento>().WithMany().HasForeignKey(x => x.DocumentoId).OnDelete(DeleteBehavior.NoAction);
        b.HasMany(x => x.Mensajes).WithOne().HasForeignKey(m => m.HiloId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionMensaje : IEntityTypeConfiguration<Mensaje>
{
    public void Configure(EntityTypeBuilder<Mensaje> b)
    {
        b.ToTable("Mensaje", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.FechaUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.Cuerpo).HasMaxLength(4000).IsRequired();
        b.Property(x => x.LeidoPorClienteUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.LeidoPorGestoriaUtc).HasColumnType("datetime2(3)");
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AutorId).OnDelete(DeleteBehavior.NoAction);
    }
}
