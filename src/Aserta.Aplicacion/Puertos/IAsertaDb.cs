using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Exportacion;
using Aserta.Dominio.Facturacion;
using Aserta.Dominio.Mensajeria;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Aplicacion.Puertos;

/// <summary>
/// La unidad de trabajo. Las entidades con tenant llegan YA filtradas por la
/// gestoria del contexto (HasQueryFilter) y ademas por RLS en SQL Server.
/// </summary>
public interface IAsertaDb
{
    DbSet<Gestoria> Gestorias { get; }
    DbSet<Usuario> Usuarios { get; }
    DbSet<Auditoria> Auditorias { get; }
    DbSet<EjecucionProgramada> EjecucionesProgramadas { get; }
    DbSet<Aviso> Avisos { get; }

    DbSet<Cliente> Clientes { get; }
    DbSet<PerfilFiscal> PerfilesFiscales { get; }

    DbSet<ModeloTributario> ModelosTributarios { get; }
    DbSet<PlazoModelo> PlazosModelo { get; }
    DbSet<ReglaObligacion> ReglasObligacion { get; }
    DbSet<ReglaCondicion> ReglasCondicion { get; }
    DbSet<DiaInhabil> DiasInhabiles { get; }

    DbSet<Obligacion> Obligaciones { get; }
    DbSet<ObligacionHistorial> ObligacionHistoriales { get; }

    DbSet<Documento> Documentos { get; }
    DbSet<DocumentoObligacion> DocumentosObligacion { get; }
    DbSet<RequisitoPeriodo> RequisitosPeriodo { get; }
    DbSet<ReglaRequisito> ReglasRequisito { get; }
    DbSet<Hilo> Hilos { get; }
    DbSet<Mensaje> Mensajes { get; }

    // Facturacion Veri*Factu (solo lectura desde la aplicacion web; la escritura pasa por Aserta.Verifactu y su repositorio)
    DbSet<SerieFacturacion> SeriesFacturacion { get; }
    DbSet<Destinatario> Destinatarios { get; }
    DbSet<ArticuloServicio> ArticulosServicio { get; }
    DbSet<CadenaEmisor> CadenasEmisor { get; }
    DbSet<FacturaEmitida> FacturasEmitidas { get; }
    DbSet<LineaFactura> LineasFactura { get; }
    DbSet<RegistroFacturacion> RegistrosFacturacion { get; }
    DbSet<EstadoEnvioRegistro> EstadosEnvio { get; }
    DbSet<EnvioPendiente> EnviosPendientes { get; }
    DbSet<LoteEnvio> LotesEnvio { get; }
    DbSet<FacturaPdf> FacturasPdf { get; }
    DbSet<Certificado> Certificados { get; }
    DbSet<Apoderamiento> Apoderamientos { get; }
    DbSet<AccesoCertificadoLog> AccesosCertificado { get; }
    DbSet<DeclaracionResponsableHistorico> DeclaracionesResponsables { get; }

    DbSet<ExportacionContable> Exportaciones { get; }
    DbSet<ExportacionDocumento> ExportacionesDocumento { get; }
    DbSet<ExportacionFactura> ExportacionesFactura { get; }

    Task<int> GuardarCambiosAsync(CancellationToken ct = default);
}
