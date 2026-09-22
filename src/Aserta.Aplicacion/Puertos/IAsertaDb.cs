using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
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

    DbSet<Cliente> Clientes { get; }
    DbSet<PerfilFiscal> PerfilesFiscales { get; }

    DbSet<ModeloTributario> ModelosTributarios { get; }
    DbSet<PlazoModelo> PlazosModelo { get; }
    DbSet<ReglaObligacion> ReglasObligacion { get; }
    DbSet<ReglaCondicion> ReglasCondicion { get; }
    DbSet<DiaInhabil> DiasInhabiles { get; }

    DbSet<Obligacion> Obligaciones { get; }
    DbSet<ObligacionHistorial> ObligacionHistoriales { get; }

    Task<int> GuardarCambiosAsync(CancellationToken ct = default);
}
