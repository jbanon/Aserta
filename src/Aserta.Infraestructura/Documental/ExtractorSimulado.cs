using System.Text.Json;
using Aserta.Aplicacion.Puertos;

namespace Aserta.Infraestructura.Documental;

/// <summary>
/// DA-08: adaptador de OCR/IA SIMULADO. Devuelve datos deterministas derivados
/// del hash del fichero para que la demo enseñe el contrato (datos sugeridos que
/// el gestor confirma) sin llamar a ningun proveedor. Se dice en voz alta que es
/// simulado: OrigenExtraccion = "Simulado".
/// </summary>
public sealed class ExtractorSimulado : IExtractorDocumental
{
    private static readonly string[] Proveedores = ["Suministros Iberia SL", "Harinas del Norte SA", "Energía Verde Comercializadora", "Telefonía Ejemplo SA", "Papelería Central", "Transportes Rápido SL", "Ferretería El Tornillo", "Seguros Ficticios SA"];

    public Task<DatosExtraidos?> ExtraerAsync(string nombreFichero, string tipoMime, string hashSha256, string tipoDocumento, CancellationToken ct = default)
    {
        if (tipoDocumento is not ("FacturaRecibida" or "FacturaEmitida" or "Ticket")) return Task.FromResult<DatosExtraidos?>(null);

        int semilla = Convert.ToInt32(hashSha256[..6], 16);
        var rnd = new Random(semilla);
        decimal baseImponible = Math.Round(15m + (decimal)rnd.NextDouble() * 1200m, 2);
        int tipoIva = new[] { 21, 21, 21, 10, 4 }[rnd.Next(5)];
        decimal cuota = Math.Round(baseImponible * tipoIva / 100m, 2);
        var fecha = DateOnly.FromDateTime(DateTime.Today).AddDays(-rnd.Next(5, 80));
        var datos = new
        {
            simulado = true,
            proveedor = Proveedores[rnd.Next(Proveedores.Length)],
            nifProveedor = "B" + rnd.Next(1000000, 9999999) + "X",
            numeroFactura = $"F{fecha.Year}-{rnd.Next(1, 9999):0000}",
            fecha = fecha.ToString("yyyy-MM-dd"),
            baseImponible,
            tipoIva,
            cuotaIva = cuota,
            total = baseImponible + cuota,
            confianza = Math.Round(0.72 + rnd.NextDouble() * 0.26, 2),
        };
        return Task.FromResult<DatosExtraidos?>(new DatosExtraidos("Simulado", JsonSerializer.Serialize(datos)));
    }
}
