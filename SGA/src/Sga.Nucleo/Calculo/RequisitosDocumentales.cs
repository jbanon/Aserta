using Sga.Nucleo.Modelo;

namespace Sga.Nucleo.Calculo;

/// <summary>Un documento que SGA espera del cliente cada trimestre, y cuantos han llegado.</summary>
public sealed record RequisitoDocumental(TipoDocumento Tipo, string Nombre, string Ayuda, int Esperados, int Recibidos)
{
    public bool Completo => Recibidos >= Esperados;
    public int Faltan => Math.Max(0, Esperados - Recibidos);
}

/// <summary>
/// Que tiene que enviar cada tipo de cliente (apuntes de la reunion): el arrendador nada (SGA lo genera todo);
/// el profesional al que SGA factura, sus facturas recibidas, gastos y extracto; el que factura el mismo, ademas
/// sus emitidas; la sociedad, emitidas, recibidas y el extracto (o la clave de consulta del banco).
/// </summary>
public static class RequisitosDocumentales
{
    public static IReadOnlyList<RequisitoDocumental> DelTrimestre(Cliente c, IEnumerable<Documento> documentosDelTrimestre)
    {
        var docs = documentosDelTrimestre.ToList();
        int N(TipoDocumento t) => docs.Count(d => d.Tipo == t);
        var lista = new List<RequisitoDocumental>();
        switch (c.Tipo)
        {
            case TipoCliente.Arrendador:
                // Nada que enviar: las facturas las emite SGA y la unica recibida es la de SGA
                break;
            case TipoCliente.Profesional:
                if (c.Emisor == QuienEmite.Cliente) lista.Add(new(TipoDocumento.FacturaEmitida, "Facturas que has emitido", "Todas las del trimestre, para comprobar la numeración.", 3, N(TipoDocumento.FacturaEmitida)));
                lista.Add(new(TipoDocumento.FacturaRecibida, "Facturas de compras y proveedores", "Foto o PDF de cada factura recibida.", 3, N(TipoDocumento.FacturaRecibida)));
                lista.Add(new(TipoDocumento.Ticket, "Tickets de gastos", "Los tickets de la actividad (material, dietas, transporte).", 3, N(TipoDocumento.Ticket)));
                lista.Add(new(TipoDocumento.SeguroOCuota, "Cuota de autónomos y seguros", "Los recibos de la cuota de cada mes y del seguro.", 3, N(TipoDocumento.SeguroOCuota)));
                if (c.TieneEmpleados) lista.Add(new(TipoDocumento.Nomina, "Nóminas del trimestre", "Las nóminas de tus empleados.", 3, N(TipoDocumento.Nomina)));
                lista.Add(new(TipoDocumento.ExtractoBancario, "Extracto bancario", "El extracto del trimestre, para cuadrar cobros y pagos.", 1, N(TipoDocumento.ExtractoBancario)));
                break;
            case TipoCliente.Sociedad:
                lista.Add(new(TipoDocumento.FacturaEmitida, "Facturas emitidas", "Las que ha emitido la empresa en el trimestre (subida o carpeta compartida).", 3, N(TipoDocumento.FacturaEmitida)));
                lista.Add(new(TipoDocumento.FacturaRecibida, "Facturas recibidas", "Las facturas de proveedores del trimestre.", 3, N(TipoDocumento.FacturaRecibida)));
                if (c.TieneEmpleados) lista.Add(new(TipoDocumento.Nomina, "Nóminas del trimestre", "Las nóminas de la plantilla.", 3, N(TipoDocumento.Nomina)));
                if (!c.ClaveConsultaBanco) lista.Add(new(TipoDocumento.ExtractoBancario, "Extracto bancario", "El extracto del trimestre para la conciliación.", 1, N(TipoDocumento.ExtractoBancario)));
                break;
        }
        return lista;
    }

    private static (string Singular, string Plural) Nombres(TipoDocumento t) => t switch
    {
        TipoDocumento.Ticket => ("ticket de gastos", "tickets de gastos"),
        TipoDocumento.FacturaRecibida => ("factura de compra", "facturas de compra"),
        TipoDocumento.FacturaEmitida => ("factura emitida", "facturas emitidas"),
        TipoDocumento.SeguroOCuota => ("recibo de autónomos o seguro", "recibos de autónomos o seguros"),
        TipoDocumento.Nomina => ("nómina", "nóminas"),
        TipoDocumento.ExtractoBancario => ("el extracto bancario", "extractos bancarios"),
        _ => ("documento", "documentos"),
    };

    /// <summary>Frase en lenguaje llano: "Te faltan 2 tickets de gastos y el extracto bancario".</summary>
    public static string Frase(IReadOnlyList<RequisitoDocumental> requisitos)
    {
        var faltan = requisitos.Where(r => !r.Completo).ToList();
        if (faltan.Count == 0) return "Tienes todo entregado. ¡Gracias!";
        var partes = faltan.Select(r => { var (sg, pl) = Nombres(r.Tipo); return r.Esperados == 1 ? sg : r.Faltan == 1 ? $"1 {sg}" : $"{r.Faltan} {pl}"; }).ToList();
        var texto = partes.Count == 1 ? partes[0] : string.Join(", ", partes.Take(partes.Count - 1)) + " y " + partes[^1];
        int total = faltan.Sum(r => r.Faltan);
        return (total == 1 ? "Te falta " : "Te faltan ") + texto + ".";
    }
}
