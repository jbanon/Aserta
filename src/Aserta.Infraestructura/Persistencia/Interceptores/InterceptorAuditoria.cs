using System.Text.Json;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aserta.Infraestructura.Persistencia.Interceptores;

/// <summary>
/// RD-08: toda alta, modificacion o baja de una entidad de negocio deja una
/// fila en dbo.Auditoria (quien, que, cuando, desde que IP, y el JSON de cambios).
/// </summary>
public sealed class InterceptorAuditoria : SaveChangesInterceptor
{
    private static readonly HashSet<Type> TiposAuditados =
    [
        typeof(Gestoria), typeof(Usuario), typeof(Cliente), typeof(PerfilFiscal), typeof(Obligacion),
        typeof(Dominio.Documental.Documento), typeof(Dominio.Documental.RequisitoPeriodo)
    ];

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly IContextoUsuarioActual _usuario;
    private readonly IContextoTenant _tenant;
    private readonly IRelojSistema _reloj;

    public InterceptorAuditoria(IContextoUsuarioActual usuario, IContextoTenant tenant, IRelojSistema reloj)
    {
        _usuario = usuario;
        _tenant = tenant;
        _reloj = reloj;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Anotar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Anotar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Anotar(DbContext? ctx)
    {
        if (ctx is null) return;
        var ahora = _reloj.AhoraUtc;
        var nuevas = new List<Auditoria>();

        foreach (var entrada in ctx.ChangeTracker.Entries().Where(e => TiposAuditados.Contains(e.Metadata.ClrType)).ToList())
        {
            if (entrada.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            var gestoriaId = ResolverGestoria(entrada);
            if (gestoriaId is null) continue;

            var detalle = entrada.State switch
            {
                EntityState.Added => entrada.Properties.ToDictionary(p => p.Metadata.Name, p => (object?)Formatear(p.CurrentValue)),
                EntityState.Deleted => entrada.Properties.ToDictionary(p => p.Metadata.Name, p => (object?)Formatear(p.OriginalValue)),
                _ => entrada.Properties.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                        .ToDictionary(p => p.Metadata.Name, p => (object?)new { antes = Formatear(p.OriginalValue), despues = Formatear(p.CurrentValue) })
            };
            if (entrada.State == EntityState.Modified && detalle.Count == 0) continue;

            nuevas.Add(new Auditoria
            {
                GestoriaId = gestoriaId.Value,
                UsuarioId = _usuario.UsuarioId,
                FechaUtc = ahora,
                Accion = entrada.State switch { EntityState.Added => AccionesAuditoria.Alta, EntityState.Deleted => AccionesAuditoria.Baja, _ => AccionesAuditoria.Modificacion },
                EntidadTipo = entrada.Metadata.ClrType.Name,
                EntidadId = ClavePrimaria(entrada),
                Detalle = JsonSerializer.Serialize(detalle, Json),
                DireccionIp = _usuario.DireccionIp,
                AgenteUsuario = Recortar(_usuario.AgenteUsuario, 500),
            });
        }

        if (nuevas.Count > 0) ctx.Set<Auditoria>().AddRange(nuevas);
    }

    private Guid? ResolverGestoria(EntityEntry entrada)
    {
        if (entrada.Entity is Gestoria g) return g.Id;
        var prop = entrada.Properties.FirstOrDefault(p => p.Metadata.Name == "GestoriaId");
        if (prop?.CurrentValue is Guid gid && gid != Guid.Empty) return gid;
        return _tenant.GestoriaId;
    }

    private static string ClavePrimaria(EntityEntry entrada) =>
        string.Join("|", entrada.Properties.Where(p => p.Metadata.IsPrimaryKey()).Select(p => p.CurrentValue?.ToString()));

    private static object? Formatear(object? v) => v switch
    {
        null => null,
        DateOnly d => d.ToString("yyyy-MM-dd"),
        DateTime dt => dt.ToString("O"),
        Enum e => e.ToString(),
        _ => v
    };

    private static string? Recortar(string? s, int max) => s is null ? null : (s.Length <= max ? s : s[..max]);
}
