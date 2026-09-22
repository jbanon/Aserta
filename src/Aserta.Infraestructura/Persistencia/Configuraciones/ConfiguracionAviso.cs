using Aserta.Dominio.Nucleo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aserta.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionAviso : IEntityTypeConfiguration<Aviso>
{
    public void Configure(EntityTypeBuilder<Aviso> b)
    {
        b.ToTable("Aviso", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Tipo).HasMaxLength(40).IsRequired();
        b.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
        b.Property(x => x.Cuerpo).HasMaxLength(1000);
        b.Property(x => x.EntidadTipo).HasMaxLength(100);
        b.Property(x => x.EntidadId).HasMaxLength(64);
        b.Property(x => x.Clave).HasMaxLength(200).IsRequired();
        b.Property(x => x.FechaUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.LeidoUtc).HasColumnType("datetime2(3)");
        b.Ignore(x => x.Leido);
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioDestinoId).OnDelete(DeleteBehavior.NoAction);
    }
}
