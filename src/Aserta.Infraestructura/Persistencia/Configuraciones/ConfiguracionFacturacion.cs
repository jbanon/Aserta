using Aserta.Dominio.Clientes;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Facturacion;
using Aserta.Dominio.Nucleo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aserta.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionSerieFacturacion : IEntityTypeConfiguration<SerieFacturacion>
{
    public void Configure(EntityTypeBuilder<SerieFacturacion> b)
    {
        b.ToTable("SerieFacturacion", "vf");
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(100).IsRequired();
        b.Property(x => x.Ejercicio).HasColumnType("smallint");
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteEmisorId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionDestinatario : IEntityTypeConfiguration<Destinatario>
{
    public void Configure(EntityTypeBuilder<Destinatario> b)
    {
        b.ToTable("Destinatario", "vf");
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nif).HasMaxLength(20);
        b.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        b.Property(x => x.Direccion).HasMaxLength(300);
        b.Property(x => x.Pais).HasColumnType("char(2)").IsRequired();
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteEmisorId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionArticuloServicio : IEntityTypeConfiguration<ArticuloServicio>
{
    public void Configure(EntityTypeBuilder<ArticuloServicio> b)
    {
        b.ToTable("ArticuloServicio", "vf");
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Descripcion).HasMaxLength(200).IsRequired();
        b.Property(x => x.PrecioUnitario).HasColumnType("decimal(18,2)");
        b.Property(x => x.TipoIva).HasColumnType("decimal(5,2)");
        b.Property(x => x.TipoRecargo).HasColumnType("decimal(5,2)");
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteEmisorId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionCadenaEmisor : IEntityTypeConfiguration<CadenaEmisor>
{
    public void Configure(EntityTypeBuilder<CadenaEmisor> b)
    {
        b.ToTable("CadenaEmisor", "vf");
        b.HasKey(x => x.NifEmisor);
        b.Property(x => x.NifEmisor).HasColumnType("char(9)");
        b.Property(x => x.UltimaHuella).HasColumnType("char(64)");
        b.Property(x => x.FechaUltimoRegistroUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.ProximoEnvioPermitidoUtc).HasColumnType("datetime2(3)");
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteEmisorId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionFacturaEmitida : IEntityTypeConfiguration<FacturaEmitida>
{
    public void Configure(EntityTypeBuilder<FacturaEmitida> b)
    {
        b.ToTable("FacturaEmitida", "vf");
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.NifEmisor).HasColumnType("char(9)").IsRequired();
        b.Property(x => x.NumSerieFactura).HasMaxLength(60).IsRequired();
        b.Property(x => x.FechaExpedicion).HasColumnType("date");
        b.Property(x => x.TipoFactura).HasConversion<string>().HasColumnType("char(2)").IsRequired();
        b.Property(x => x.TipoRectificativa).HasConversion<string>().HasColumnType("char(1)");
        b.Property(x => x.MotivoRectificacion).HasMaxLength(300);
        b.Property(x => x.DestinatarioNif).HasMaxLength(20);
        b.Property(x => x.DestinatarioNombre).HasMaxLength(120);
        foreach (var p in new[] { nameof(FacturaEmitida.BaseTotal), nameof(FacturaEmitida.CuotaTotal), nameof(FacturaEmitida.CuotaRecargoTotal), nameof(FacturaEmitida.RetencionTotal), nameof(FacturaEmitida.ImporteTotal) })
            b.Property(p).HasColumnType("decimal(18,2)");
        b.Property(x => x.PorcentajeRetencion).HasColumnType("decimal(5,2)");
        b.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
        b.Property(x => x.FechaHoraCreacionUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.UrlQr).HasMaxLength(400).IsRequired();
        b.Ignore(x => x.EsSimplificada);
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteEmisorId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<SerieFacturacion>().WithMany().HasForeignKey(x => x.SerieId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<FacturaEmitida>().WithMany().HasForeignKey(x => x.FacturaRectificadaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
        b.HasMany(x => x.Lineas).WithOne().HasForeignKey(l => l.FacturaEmitidaId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionLineaFactura : IEntityTypeConfiguration<LineaFactura>
{
    public void Configure(EntityTypeBuilder<LineaFactura> b)
    {
        b.ToTable("LineaFactura", "vf");
        b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Descripcion).HasMaxLength(300).IsRequired();
        b.Property(x => x.Cantidad).HasColumnType("decimal(18,3)");
        b.Property(x => x.PrecioUnitario).HasColumnType("decimal(18,2)");
        b.Property(x => x.TipoIva).HasColumnType("decimal(5,2)");
        b.Property(x => x.TipoRecargo).HasColumnType("decimal(5,2)");
        b.Property(x => x.BaseLinea).HasColumnType("decimal(18,2)");
    }
}

public sealed class ConfiguracionRegistroFacturacion : IEntityTypeConfiguration<RegistroFacturacion>
{
    public void Configure(EntityTypeBuilder<RegistroFacturacion> b)
    {
        b.ToTable("RegistroFacturacion", "vf");
        b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.NifEmisor).HasColumnType("char(9)").IsRequired();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.Property(x => x.HuellaAnterior).HasColumnType("char(64)");
        b.Property(x => x.Huella).HasColumnType("char(64)").IsRequired();
        b.Property(x => x.FechaHoraHusoGenRegistro).HasColumnType("datetimeoffset(0)");
        b.Property(x => x.FechaHoraHusoGenRegistroTexto).HasColumnType("varchar(25)").IsRequired();
        b.Property(x => x.CadenaHuella).HasMaxLength(1000).IsRequired();
        b.Property(x => x.XmlRegistro).IsRequired();
        b.Property(x => x.IdSistemaInformatico).HasMaxLength(2).IsRequired();
        b.Property(x => x.VersionSistemaInformatico).HasMaxLength(50).IsRequired();
        b.Property(x => x.NumeroInstalacion).HasMaxLength(100).IsRequired();
        b.HasOne<FacturaEmitida>().WithMany().HasForeignKey(x => x.FacturaEmitidaId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionEstadoEnvioRegistro : IEntityTypeConfiguration<EstadoEnvioRegistro>
{
    public void Configure(EntityTypeBuilder<EstadoEnvioRegistro> b)
    {
        b.ToTable("EstadoEnvioRegistro", "vf");
        b.HasKey(x => x.RegistroFacturacionId); b.Property(x => x.RegistroFacturacionId).ValueGeneratedNever();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.FechaEnvioUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.FechaRespuestaUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CsvAeat).HasMaxLength(100);
        b.Property(x => x.CodigoErrorAeat).HasMaxLength(20);
        b.Property(x => x.DescripcionErrorAeat).HasMaxLength(1000);
        b.HasOne<RegistroFacturacion>().WithOne().HasForeignKey<EstadoEnvioRegistro>(x => x.RegistroFacturacionId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionEnvioPendiente : IEntityTypeConfiguration<EnvioPendiente>
{
    public void Configure(EntityTypeBuilder<EnvioPendiente> b)
    {
        b.ToTable("EnvioPendiente", "vf");
        b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.NifEmisor).HasColumnType("char(9)").IsRequired();
        b.Property(x => x.FechaAltaUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.ProximoIntentoUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.TomadoUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.UltimoError).HasMaxLength(1000);
        b.HasOne<RegistroFacturacion>().WithMany().HasForeignKey(x => x.RegistroFacturacionId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionLoteEnvio : IEntityTypeConfiguration<LoteEnvio>
{
    public void Configure(EntityTypeBuilder<LoteEnvio> b)
    {
        b.ToTable("LoteEnvio", "vf");
        b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.NifEmisor).HasColumnType("char(9)").IsRequired();
        b.Property(x => x.FechaEnvioUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.Cliente).HasMaxLength(40).IsRequired();
        b.Property(x => x.Resultado).HasMaxLength(30).IsRequired();
        b.Property(x => x.Error).HasMaxLength(1000);
    }
}

public sealed class ConfiguracionFacturaPdf : IEntityTypeConfiguration<FacturaPdf>
{
    public void Configure(EntityTypeBuilder<FacturaPdf> b)
    {
        b.ToTable("FacturaPdf", "vf");
        b.HasKey(x => x.FacturaEmitidaId); b.Property(x => x.FacturaEmitidaId).ValueGeneratedNever();
        b.Property(x => x.VersionPlantilla).HasMaxLength(20);
        b.Property(x => x.Motor).HasMaxLength(30);
        b.Property(x => x.FechaGeneracionUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.Error).HasMaxLength(1000);
        b.Ignore(x => x.Generado);
        b.HasOne<FacturaEmitida>().WithOne().HasForeignKey<FacturaPdf>(x => x.FacturaEmitidaId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionCertificado : IEntityTypeConfiguration<Certificado>
{
    public void Configure(EntityTypeBuilder<Certificado> b)
    {
        b.ToTable("Certificado", "vf");
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.NifTitular).HasColumnType("char(9)").IsRequired();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Alias).HasMaxLength(100).IsRequired();
        b.Property(x => x.HuellaDigital).HasMaxLength(64).IsRequired();
        b.Property(x => x.ValidoDesde).HasColumnType("date");
        b.Property(x => x.ValidoHasta).HasColumnType("date");
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionApoderamiento : IEntityTypeConfiguration<Apoderamiento>
{
    public void Configure(EntityTypeBuilder<Apoderamiento> b)
    {
        b.ToTable("Apoderamiento", "vf");
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Alcance).HasMaxLength(100).IsRequired();
        b.Property(x => x.FechaAlta).HasColumnType("date");
        b.Property(x => x.FechaFin).HasColumnType("date");
        b.HasOne<Gestoria>().WithMany().HasForeignKey(x => x.GestoriaId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Documento>().WithMany().HasForeignKey(x => x.DocumentoRespaldoId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionAccesoCertificadoLog : IEntityTypeConfiguration<AccesoCertificadoLog>
{
    public void Configure(EntityTypeBuilder<AccesoCertificadoLog> b)
    {
        b.ToTable("AccesoCertificadoLog", "vf");
        b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.FechaUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.Motivo).HasMaxLength(200).IsRequired();
        b.Property(x => x.NifObligado).HasColumnType("char(9)").IsRequired();
        b.HasOne<Certificado>().WithMany().HasForeignKey(x => x.CertificadoId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ConfiguracionDeclaracionResponsableHistorico : IEntityTypeConfiguration<DeclaracionResponsableHistorico>
{
    public void Configure(EntityTypeBuilder<DeclaracionResponsableHistorico> b)
    {
        b.ToTable("DeclaracionResponsableHistorico", "vf");
        b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Version).HasMaxLength(50).IsRequired();
        b.Property(x => x.FechaSuscripcion).HasColumnType("date");
        b.Property(x => x.Contenido).IsRequired();
        b.Property(x => x.HashSha256).HasColumnType("char(64)").IsRequired();
        b.Property(x => x.FechaRegistroUtc).HasColumnType("datetime2(3)");
    }
}
