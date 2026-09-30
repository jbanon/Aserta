namespace Sga.Nucleo.Calendario;

public sealed record DescripcionModelo(string Codigo, string Nombre, string ParaElCliente, string Grupo, bool Anual);

/// <summary>Los modelos que aparecen en el Excel de control de SGA y en los apuntes, explicados para quien no sabe de impuestos.</summary>
public static class Modelos
{
    public static readonly IReadOnlyList<DescripcionModelo> Todos =
    [
        new("303", "IVA trimestral", "Tu declaración de IVA: lo que has cobrado de IVA menos lo que has pagado.", "IVA", false),
        new("349", "Operaciones intracomunitarias", "Solo si compras o vendes a empresas de otros países de la UE. Es informativo: no se paga nada.", "IVA", false),
        new("111", "Retenciones a trabajadores y profesionales", "El IRPF que has retenido en nóminas o a profesionales, y que ingresas por ellos.", "Retenciones", false),
        new("115", "Retenciones por alquileres", "El IRPF que retienes al pagar el alquiler de un local, e ingresas por el propietario.", "Retenciones", false),
        new("123", "Retenciones del capital", "Retenciones sobre intereses o dividendos que hayas pagado.", "Retenciones", false),
        new("130", "Pago a cuenta del IRPF", "Un adelanto trimestral del IRPF sobre el beneficio de tu actividad.", "IRPF", false),
        new("202", "Pago fraccionado de Sociedades", "Un adelanto del Impuesto de Sociedades, tres veces al año (abril, octubre y diciembre).", "Sociedades", false),
        new("390", "Resumen anual de IVA", "El resumen del IVA de todo el año. Informativo, no se paga nada.", "IVA", true),
        new("347", "Operaciones con terceros", "Lista de clientes y proveedores con los que has movido más de 3.005,06 € en el año [VERIFICAR umbral].", "Informativas", true),
        new("190", "Resumen anual de retenciones", "El resumen anual del modelo 111.", "Retenciones", true),
        new("180", "Resumen anual de retenciones por alquiler", "El resumen anual del modelo 115.", "Retenciones", true),
        new("200", "Impuesto de Sociedades", "La declaración anual del impuesto sobre el beneficio de la sociedad.", "Sociedades", true),
        new("LIBROS", "Legalización de libros", "Presentar los libros contables en el Registro Mercantil.", "Contabilidad", true),
        new("CCAA", "Cuentas anuales", "Depositar las cuentas del año en el Registro Mercantil.", "Contabilidad", true),
        new("100", "Renta", "Tu declaración de la renta.", "IRPF", true),
    ];

    /// <summary>Columnas de la matriz de control, en el orden del Excel: IVA (303, 349), retenciones (111, 115, 123), sociedades (202), contabilidad (Libros, CC.AA.).</summary>
    public static readonly IReadOnlyList<string> ColumnasMatriz = ["303", "349", "111", "115", "123", "202", "LIBROS", "CCAA"];

    public static readonly IReadOnlyList<(string Grupo, string[] Modelos)> GruposMatriz =
    [
        ("IVA", ["303", "349"]), ("Retenciones", ["111", "115", "123"]), ("Sociedades", ["202"]), ("Contabilidad", ["LIBROS", "CCAA"]),
    ];

    public static DescripcionModelo? Buscar(string codigo) => Todos.FirstOrDefault(m => m.Codigo == codigo);
    public static string Nombre(string codigo) => Buscar(codigo)?.Nombre ?? codigo;
}
