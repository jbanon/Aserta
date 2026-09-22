using System.Globalization;
using System.Text;
using Aserta.Dominio.Facturacion;
using Aserta.Verifactu.Servicios;
using QRCoder;

namespace Aserta.Infraestructura.Facturacion;

/// <summary>
/// Respaldo sin navegador: PDF A4 dibujado a mano (texto Helvetica + QR como
/// rectangulos vectoriales de 35 mm). Cumple la posicion y los textos del QR
/// (qr-y-pdf.md §2) aunque sin la maquetacion de la plantilla HTML. Se usa solo
/// si Chromium no esta disponible; el motor queda anotado en vf.FacturaPdf.
/// </summary>
public sealed class GeneradorPdfBasico : IGeneradorPdf
{
    public string Motor => "Basico";

    public Task<byte[]> GenerarAsync(ContenidoFacturaPdf c, CancellationToken ct = default)
    {
        var f = c.Factura;
        var es = CultureInfo.GetCultureInfo("es-ES");
        var sb = new StringBuilder();
        const double mm = 72.0 / 25.4;
        double ancho = 210 * mm, alto = 297 * mm;

        // QR: matriz de modulos, 35 mm, centrado arriba, con zona de silencio
        using var gen = new QRCodeGenerator();
        using var datos = gen.CreateQrCode(c.UrlQr, QRCodeGenerator.ECCLevel.M);
        var matriz = datos.ModuleMatrix;
        int n = matriz.Count;
        double lado = 35 * mm, modulo = lado / n, x0 = (ancho - lado) / 2, y0 = alto - 20 * mm - lado;
        sb.Append("0 g\n");
        for (int fila = 0; fila < n; fila++)
            for (int col = 0; col < n; col++)
                if (matriz[fila][col]) sb.Append(F(x0 + col * modulo)).Append(' ').Append(F(y0 + (n - 1 - fila) * modulo)).Append(' ').Append(F(modulo)).Append(' ').Append(F(modulo)).Append(" re f\n");

        double y = alto - 14 * mm;
        void Texto(string t, double x, double yy, double tam = 10.5, bool centrado = false)
        {
            var enc = Esc(t);
            double ancho1 = t.Length * tam * 0.5;
            double xx = centrado ? (ancho - ancho1) / 2 : x;
            sb.Append("BT /F1 ").Append(F(tam)).Append(" Tf ").Append(F(xx)).Append(' ').Append(F(yy)).Append(" Td (").Append(enc).Append(") Tj ET\n");
        }
        Texto("QR tributario:", 0, y, 10.5, true);
        y = y0 - 5 * mm;
        Texto(PlantillaFacturaHtml.Leyenda, 0, y, 10.5, true);
        y -= 10 * mm;
        if (c.EntornoPruebas) { Texto("ENTORNO DE PRUEBAS - datos ficticios de demostracion", 16 * mm, y, 9); y -= 6 * mm; }
        string tipo = f.TipoFactura.EsRectificativa() ? $"FACTURA RECTIFICATIVA ({f.TipoFactura})" : f.TipoFactura == TipoFactura.F2 ? "FACTURA SIMPLIFICADA" : "FACTURA";
        Texto($"{tipo}  {f.NumSerieFactura}  Fecha {f.FechaExpedicion:dd/MM/yyyy}", 16 * mm, y, 14); y -= 8 * mm;
        Texto($"Emisor: {c.Emisor.Nombre}  NIF {c.Emisor.Nif}", 16 * mm, y); y -= 5 * mm;
        if (!string.IsNullOrEmpty(c.Emisor.Direccion)) { Texto(c.Emisor.Direccion!, 16 * mm, y); y -= 5 * mm; }
        Texto(f.DestinatarioNombre is null ? "Destinatario: (factura simplificada)" : $"Destinatario: {f.DestinatarioNombre}  NIF {f.DestinatarioNif}", 16 * mm, y); y -= 5 * mm;
        if (c.NumRectificada is not null) { Texto($"Rectifica a {c.NumRectificada}: {f.MotivoRectificacion}", 16 * mm, y); y -= 5 * mm; }
        Texto($"Concepto: {f.Descripcion}", 16 * mm, y); y -= 8 * mm;
        Texto("Descripcion / Cantidad / Precio / IVA / Base", 16 * mm, y, 9); y -= 5 * mm;
        foreach (var l in f.Lineas.OrderBy(l => l.Orden))
        { Texto($"{l.Descripcion}  {l.Cantidad.ToString("0.###", es)} x {l.PrecioUnitario.ToString("N2", es)}  {(l.Exenta ? "Exenta" : l.TipoIva.ToString("N2", es) + " %")}  {l.BaseLinea.ToString("N2", es)}", 16 * mm, y); y -= 5 * mm; }
        y -= 4 * mm;
        Texto($"Base imponible {f.BaseTotal.ToString("N2", es)} EUR   Cuota IVA {f.CuotaTotal.ToString("N2", es)} EUR", 16 * mm, y); y -= 5 * mm;
        if (f.CuotaRecargoTotal != 0) { Texto($"Recargo de equivalencia {f.CuotaRecargoTotal.ToString("N2", es)} EUR", 16 * mm, y); y -= 5 * mm; }
        if (f.RetencionTotal != 0) { Texto($"Retencion IRPF {f.PorcentajeRetencion.ToString("N2", es)} %  -{f.RetencionTotal.ToString("N2", es)} EUR", 16 * mm, y); y -= 5 * mm; }
        Texto($"TOTAL {f.ImporteTotal.ToString("N2", es)} EUR", 16 * mm, y, 13); y -= 12 * mm;
        Texto("Registro de facturacion generado por un sistema informatico de facturacion en modo VERI*FACTU (RD 1007/2023).", 16 * mm, y, 8);

        return Task.FromResult(Ensamblar(sb.ToString()));
    }

    private static string F(double d) => d.ToString("0.###", CultureInfo.InvariantCulture);
    private static string Esc(string t) => Latin(t).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string Latin(string t) => Encoding.Latin1.GetString(Encoding.Latin1.GetBytes(t));   // caracteres fuera de Latin-1 => '?'

    private static byte[] Ensamblar(string contenido)
    {
        var objetos = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595.28 841.89] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            $"<< /Length {Encoding.Latin1.GetByteCount(contenido)} >>\nstream\n{contenido}\nendstream",
        };
        var sb = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (int i = 0; i < objetos.Count; i++) { offsets.Add(Encoding.Latin1.GetByteCount(sb.ToString())); sb.Append($"{i + 1} 0 obj\n{objetos[i]}\nendobj\n"); }
        int xref = Encoding.Latin1.GetByteCount(sb.ToString());
        sb.Append($"xref\n0 {objetos.Count + 1}\n0000000000 65535 f \n");
        foreach (var o in offsets) sb.Append($"{o:0000000000} 00000 n \n");
        sb.Append($"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.Latin1.GetBytes(sb.ToString());
    }
}
