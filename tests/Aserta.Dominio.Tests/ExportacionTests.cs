using System.Text;
using Aserta.Dominio.Exportacion;
using Aserta.Exportacion;

namespace Aserta.Dominio.Tests;

public class ExportacionTests
{
    private static ApunteExportable Apunte(string num, decimal baseI, DateOnly fecha, TipoApunte tipo = TipoApunte.FacturaRecibida) =>
        new(tipo, Guid.NewGuid(), fecha, num, "B12345674", "Proveedor; con punto y coma", baseI, 21m, Math.Round(baseI * 0.21m, 2), 0m, 0m, Math.Round(baseI * 1.21m, 2), "Concepto \"entre comillas\"", "OCR simulado", false);

    [Fact]
    public void Csv_generico_tiene_cabecera_fija_bom_y_escapa_campos()
    {
        var f = new ExportadorCsvGenerico().Exportar([Apunte("F-2", 100m, new DateOnly(2026, 7, 10)), Apunte("F-1", 50m, new DateOnly(2026, 7, 1), TipoApunte.FacturaEmitida)], "B12345674", 2026, "3T");
        Assert.Equal("aserta-B12345674-2026-3T-v1.csv", f.NombreFichero);
        Assert.Equal(0xEF, f.Contenido[0]); // BOM
        var texto = Encoding.UTF8.GetString(f.Contenido.Skip(3).ToArray());
        var lineas = texto.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(string.Join(';', ExportadorCsvGenerico.Cabecera), lineas[0].TrimEnd('\r'));
        Assert.StartsWith("Emitida;01/07/2026;F-1;B12345674;\"Proveedor; con punto y coma\";50,00;21,00;10,50;0,00;0,00;60,50;\"Concepto \"\"entre comillas\"\"\";OCR simulado;N;", lineas[1]);
        Assert.StartsWith("Recibida;10/07/2026;F-2", lineas[2]);
    }

    [Fact]
    public void A3_esta_modelado_pero_no_disponible()
    {
        var a3 = new ExportadorA3();
        Assert.False(a3.Disponible);
        Assert.Throws<NotSupportedException>(() => a3.Exportar([], "B12345674", 2026, "3T"));
        Assert.Equal(2, RegistroExportadores.Todos().Count);
    }
}
