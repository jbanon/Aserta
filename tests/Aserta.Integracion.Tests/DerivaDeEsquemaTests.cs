using Aserta.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Aserta.Integracion.Tests;

/// <summary>
/// ADR-001 §2.6: sin migraciones de EF, este test es la red de seguridad que
/// detecta cualquier deriva entre el modelo EF y el esquema creado por los scripts.
/// Compara tablas, columnas, tipos y nulabilidad contra INFORMATION_SCHEMA.
/// </summary>
[Collection("aplicacion")]
public class DerivaDeEsquemaTests
{
    private readonly FabricaAplicacion _app;
    public DerivaDeEsquemaTests(FabricaAplicacion app) => _app = app;

    private sealed record Columna(string Esquema, string Tabla, string Nombre, string Tipo, bool Nula);

    [Fact]
    public async Task El_modelo_EF_coincide_con_la_base_de_datos()
    {
        using var scope = _app.AmbitoComo(null, mantenimiento: true);
        var db = scope.ServiceProvider.GetRequiredService<AsertaDbContext>();

        var reales = (await db.Database.SqlQueryRaw<ColumnaBd>(
            "SELECT TABLE_SCHEMA AS Esquema, TABLE_NAME AS Tabla, COLUMN_NAME AS Nombre, DATA_TYPE AS Tipo, CHARACTER_MAXIMUM_LENGTH AS Longitud, NUMERIC_PRECISION AS Precision, NUMERIC_SCALE AS Escala, DATETIME_PRECISION AS PrecisionFecha, IS_NULLABLE AS Nula FROM INFORMATION_SCHEMA.COLUMNS")
            .ToListAsync()).Select(c => new Columna(c.Esquema, c.Tabla, c.Nombre, TipoSql(c), c.Nula == "YES")).ToList();

        var diferencias = new List<string>();
        foreach (var et in db.Model.GetEntityTypes())
        {
            var tabla = et.GetTableName();
            if (tabla is null) continue;
            var esquema = et.GetSchema() ?? "dbo";
            var sid = StoreObjectIdentifier.Table(tabla, et.GetSchema());
            var columnasReales = reales.Where(c => c.Esquema == esquema && c.Tabla == tabla).ToDictionary(c => c.Nombre, StringComparer.OrdinalIgnoreCase);
            if (columnasReales.Count == 0) { diferencias.Add($"Falta la tabla {esquema}.{tabla}"); continue; }

            foreach (var p in et.GetProperties())
            {
                var nombre = p.GetColumnName(sid)!;
                if (!columnasReales.TryGetValue(nombre, out var real)) { diferencias.Add($"{esquema}.{tabla}.{nombre}: falta en la base de datos"); continue; }
                var tipoEf = p.GetColumnType(sid).ToLowerInvariant();
                if (tipoEf == "datetimeoffset") tipoEf = "datetimeoffset(7)";
                if (!string.Equals(tipoEf, real.Tipo, StringComparison.OrdinalIgnoreCase)) diferencias.Add($"{esquema}.{tabla}.{nombre}: EF {tipoEf} ≠ BD {real.Tipo}");
                if (p.IsNullable != real.Nula) diferencias.Add($"{esquema}.{tabla}.{nombre}: EF {(p.IsNullable ? "NULL" : "NOT NULL")} ≠ BD {(real.Nula ? "NULL" : "NOT NULL")}");
                columnasReales.Remove(nombre);
            }
            foreach (var sobrante in columnasReales.Keys) diferencias.Add($"{esquema}.{tabla}.{sobrante}: existe en la base de datos pero no en el modelo EF");
        }

        Assert.True(diferencias.Count == 0, "Deriva de esquema:\n - " + string.Join("\n - ", diferencias));
    }

    private static string TipoSql(ColumnaBd c) => c.Tipo switch
    {
        "nvarchar" or "varchar" or "char" or "nchar" or "varbinary" => c.Longitud == -1 ? $"{c.Tipo}(max)" : $"{c.Tipo}({c.Longitud})",
        "decimal" or "numeric" => $"{c.Tipo}({c.Precision},{c.Escala})",
        "datetime2" or "datetimeoffset" or "time" => $"{c.Tipo}({c.PrecisionFecha})",
        _ => c.Tipo
    };

    private sealed class ColumnaBd
    {
        public string Esquema { get; set; } = "";
        public string Tabla { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public int? Longitud { get; set; }
        public byte? Precision { get; set; }
        public int? Escala { get; set; }
        public short? PrecisionFecha { get; set; }
        public string Nula { get; set; } = "";
    }
}
