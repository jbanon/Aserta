using Aserta.Dominio.Clientes;
using Aserta.Dominio.Nucleo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aserta.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionCliente : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("Cliente", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nif).HasMaxLength(9).IsRequired();
        b.Property(x => x.RazonSocial).HasMaxLength(200).IsRequired();
        b.Property(x => x.NombreComercial).HasMaxLength(200);
        b.Property(x => x.FormaJuridica).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Telefono).HasMaxLength(30);
        b.Property(x => x.DireccionCalle).HasMaxLength(200);
        b.Property(x => x.DireccionCodigoPostal).HasMaxLength(10);
        b.Property(x => x.DireccionMunicipio).HasMaxLength(100);
        b.Property(x => x.DireccionProvincia).HasMaxLength(100);
        b.Property(x => x.FechaAlta).HasColumnType("date");
        b.Property(x => x.FechaBaja).HasColumnType("date");
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Notas);
        b.Ignore(x => x.NombreParaMostrar);
        b.Ignore(x => x.PerfilActual);
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AsesorResponsableId).OnDelete(DeleteBehavior.NoAction);
        b.HasMany(x => x.Perfiles).WithOne().HasForeignKey(p => p.ClienteId).OnDelete(DeleteBehavior.NoAction);
        b.Navigation(x => x.Perfiles).AutoInclude(false);
    }
}

public sealed class ConfiguracionPerfilFiscal : IEntityTypeConfiguration<PerfilFiscal>
{
    public void Configure(EntityTypeBuilder<PerfilFiscal> b)
    {
        b.ToTable("PerfilFiscal", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.VigenteDesde).HasColumnType("date");
        b.Property(x => x.VigenteHasta).HasColumnType("date");
        b.Property(x => x.RegimenIrpf).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.RegimenIva).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.PeriodicidadIva).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.Property(x => x.Territorio).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.Property(x => x.CierreEjercicioMes).HasColumnType("tinyint");
        b.Property(x => x.CierreEjercicioDia).HasColumnType("tinyint");
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
    }
}
