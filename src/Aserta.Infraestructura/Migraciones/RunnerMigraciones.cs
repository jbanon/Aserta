using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Aserta.Infraestructura.Migraciones;

/// <summary>
/// Aplica al arrancar los scripts SQL idempotentes de Scripts/Migrations en orden
/// de nombre, registrando el hash SHA-256 de cada uno en dbo.MigracionAplicada.
/// Aborta el arranque si un script falla o si cambia el hash de uno ya aplicado
/// (ADR-001 §2.6: un script aplicado NUNCA se edita).
/// Usa SqlConnection directa, no EF, y fija EsMantenimiento = 1 para saltar RLS.
/// </summary>
public sealed partial class RunnerMigraciones
{
    private readonly string _cadenaConexion;
    private readonly string _carpeta;
    private readonly ILogger<RunnerMigraciones> _log;

    public RunnerMigraciones(string cadenaConexion, string carpeta, ILogger<RunnerMigraciones> log)
    {
        _cadenaConexion = cadenaConexion;
        _carpeta = carpeta;
        _log = log;
    }

    public sealed record Resumen(int Aplicados, int Omitidos, IReadOnlyList<string> Nombres);

    public async Task<Resumen> AplicarAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_carpeta))
            throw new DirectoryNotFoundException($"No existe la carpeta de scripts de migración: {_carpeta}");

        var ficheros = Directory.GetFiles(_carpeta, "*.sql").OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal).ToList();
        _log.LogInformation("Runner de migraciones: {N} scripts en {Carpeta}", ficheros.Count, _carpeta);

        await using var cn = new SqlConnection(_cadenaConexion);
        await cn.OpenAsync(ct);
        await EjecutarAsync(cn, null, "EXEC sp_set_session_context @key = N'EsMantenimiento', @value = 1;", ct);
        await EjecutarAsync(cn, null, SqlTablaControl, ct);

        var aplicados = await LeerAplicadosAsync(cn, ct);
        int nAplicados = 0, nOmitidos = 0;
        var nombres = new List<string>();

        foreach (var fichero in ficheros)
        {
            ct.ThrowIfCancellationRequested();
            var nombre = Path.GetFileName(fichero);
            var contenido = await File.ReadAllTextAsync(fichero, Encoding.UTF8, ct);
            var hash = CalcularHash(contenido);

            if (aplicados.TryGetValue(nombre, out var hashPrevio))
            {
                if (!string.Equals(hashPrevio, hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"El script '{nombre}' ya se aplicó con otro contenido (hash {hashPrevio[..12]}… ≠ {hash[..12]}…). " +
                        "Un script aplicado no se edita: revierta el cambio y escriba un script nuevo.");
                nOmitidos++;
                continue;
            }

            _log.LogInformation("Aplicando {Script}…", nombre);
            var cronometro = Stopwatch.StartNew();
            await using (var tx = await cn.BeginTransactionAsync(ct))
            {
                try
                {
                    int i = 0;
                    foreach (var lote in DividirEnLotes(contenido))
                    {
                        i++;
                        try
                        {
                            await EjecutarAsync(cn, (SqlTransaction)tx, lote, ct);
                        }
                        catch (SqlException ex)
                        {
                            throw new InvalidOperationException($"Error en '{nombre}', lote {i}: {ex.Message}\n--- lote ---\n{Recortar(lote)}", ex);
                        }
                    }
                    await EjecutarAsync(cn, (SqlTransaction)tx,
                        "INSERT INTO dbo.MigracionAplicada (Nombre, HashSha256, FechaAplicacionUtc, DuracionMs) VALUES (@n, @h, SYSUTCDATETIME(), @d);",
                        ct, ("@n", nombre), ("@h", hash), ("@d", (int)cronometro.ElapsedMilliseconds));
                    await tx.CommitAsync(ct);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
            }
            cronometro.Stop();
            _log.LogInformation("Aplicado {Script} en {Ms} ms", nombre, cronometro.ElapsedMilliseconds);
            nAplicados++;
            nombres.Add(nombre);
        }

        _log.LogInformation("Runner de migraciones: {A} aplicados, {O} ya estaban", nAplicados, nOmitidos);
        return new Resumen(nAplicados, nOmitidos, nombres);
    }

    private const string SqlTablaControl = """
        IF OBJECT_ID(N'dbo.MigracionAplicada', N'U') IS NULL
        CREATE TABLE dbo.MigracionAplicada
        (
            Nombre              nvarchar(200)  NOT NULL CONSTRAINT PK_MigracionAplicada PRIMARY KEY,
            HashSha256          char(64)       NOT NULL,
            FechaAplicacionUtc  datetime2(3)   NOT NULL CONSTRAINT DF_MigracionAplicada_Fecha DEFAULT SYSUTCDATETIME(),
            DuracionMs          int            NOT NULL CONSTRAINT DF_MigracionAplicada_Duracion DEFAULT 0
        );
        """;

    private static async Task<Dictionary<string, string>> LeerAplicadosAsync(SqlConnection cn, CancellationToken ct)
    {
        var r = new Dictionary<string, string>(StringComparer.Ordinal);
        await using var cmd = new SqlCommand("SELECT Nombre, HashSha256 FROM dbo.MigracionAplicada", cn);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct)) r[rd.GetString(0)] = rd.GetString(1).Trim();
        return r;
    }

    private static async Task EjecutarAsync(SqlConnection cn, SqlTransaction? tx, string sql, CancellationToken ct, params (string Nombre, object Valor)[] parametros)
    {
        await using var cmd = new SqlCommand(sql, cn, tx) { CommandTimeout = 300 };
        foreach (var (n, v) in parametros) cmd.Parameters.AddWithValue(n, v);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Hash sobre el contenido normalizado (saltos de linea LF, sin BOM, sin espacio final) para que git no lo altere.</summary>
    public static string CalcularHash(string contenido)
    {
        var normalizado = contenido.Replace("\r\n", "\n").TrimStart('﻿').TrimEnd();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizado)));
    }

    [GeneratedRegex(@"^\s*GO\s*(?:\d+)?\s*(?:--.*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SeparadorGo();

    public static IEnumerable<string> DividirEnLotes(string script) =>
        SeparadorGo().Split(script).Select(l => l.Trim()).Where(l => l.Length > 0);

    private static string Recortar(string s) => s.Length <= 600 ? s : s[..600] + "…";
}
