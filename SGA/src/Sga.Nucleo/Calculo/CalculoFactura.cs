namespace Sga.Nucleo.Calculo;

/// <summary>Importes de una factura con IVA y retencion de IRPF (la factura de alquiler real: 2.000 + 21 % - 19 % = 2.040).</summary>
public readonly record struct ImportesFactura(decimal Base, decimal TipoIva, decimal CuotaIva, decimal TipoRetencion, decimal CuotaRetencion, decimal Total);

public static class CalculoFactura
{
    public static ImportesFactura Calcular(decimal baseImponible, decimal tipoIva, decimal tipoRetencion)
    {
        var cuotaIva = Redondear(baseImponible * tipoIva / 100m);
        var cuotaRet = Redondear(baseImponible * tipoRetencion / 100m);
        return new ImportesFactura(Redondear(baseImponible), tipoIva, cuotaIva, tipoRetencion, cuotaRet, Redondear(baseImponible + cuotaIva - cuotaRet));
    }

    /// <summary>Factura de alquiler de local: IVA 21 % y retencion de IRPF 19 % [VERIFICAR tipos vigentes; son los de la factura de referencia].</summary>
    public static ImportesFactura Alquiler(decimal renta, decimal tipoIva = 21m, decimal tipoRetencion = 19m) => Calcular(renta, tipoIva, tipoRetencion);

    public static decimal Redondear(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    /// <summary>Numero de factura de alquiler con el formato que usa SGA: "26/8" (ano/mes). Si hay mas de una en el mes, "26/8-2".</summary>
    public static string NumeroAlquiler(DateOnly fecha, int ordinalEnMes = 1) =>
        ordinalEnMes <= 1 ? $"{fecha.Year % 100:00}/{fecha.Month}" : $"{fecha.Year % 100:00}/{fecha.Month}-{ordinalEnMes}";
}
