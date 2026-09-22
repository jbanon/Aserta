using System.Data;
using System.Data.Common;
using Aserta.Aplicacion.Puertos;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aserta.Infraestructura.Persistencia.Interceptores;

/// <summary>
/// Segunda barrera del aislamiento multi-tenant (RD-04, ADR-001 §2.4): al abrir
/// CADA conexion fija SESSION_CONTEXT('GestoriaId') y ('EsMantenimiento'), que
/// es lo que lee dbo.fn_FiltroTenant en la SECURITY POLICY.
/// Se fija en cada apertura, tambien cuando la conexion viene del pool, para que
/// nunca quede el contexto de otro tenant (riesgo M1 de 04-modelo-datos.md).
/// </summary>
public sealed class InterceptorSesionTenant : DbConnectionInterceptor
{
    private const string Sql = """
        EXEC sp_set_session_context @key = N'GestoriaId', @value = @gestoriaId;
        EXEC sp_set_session_context @key = N'EsMantenimiento', @value = @esMantenimiento;
        """;

    private readonly IContextoTenant _tenant;

    public InterceptorSesionTenant(IContextoTenant tenant) => _tenant = tenant;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var cmd = Preparar(connection);
        cmd.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var cmd = Preparar(connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand Preparar(DbConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = Sql;

        var pG = cmd.CreateParameter();
        pG.ParameterName = "@gestoriaId";
        pG.DbType = DbType.Guid;
        pG.Value = _tenant.GestoriaId.HasValue ? _tenant.GestoriaId.Value : DBNull.Value;
        cmd.Parameters.Add(pG);

        var pM = cmd.CreateParameter();
        pM.ParameterName = "@esMantenimiento";
        pM.DbType = DbType.Boolean;
        pM.Value = _tenant.EsMantenimiento;
        cmd.Parameters.Add(pM);
        return cmd;
    }
}
