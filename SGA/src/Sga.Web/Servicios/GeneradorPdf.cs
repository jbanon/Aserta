using System.Globalization;
using System.Text;
using Sga.Nucleo.Modelo;

namespace Sga.Web.Servicios;

/// <summary>
/// PDF dibujado a mano (sin Chromium: el servidor solo puede permitirse una instancia, y ya la usa Aserta).
/// Suficiente para la factura de alquiler y el justificante: texto, lineas y rectangulos con las fuentes base del PDF.
/// </summary>
public sealed class GeneradorPdf
{
    private sealed class Pagina
    {
        public readonly StringBuilder Ops = new();
        private static readonly CultureInfo I = CultureInfo.InvariantCulture;
        private static string N(double v) => v.ToString("0.##", I);
        private static double X(double mm) => mm * 72 / 25.4;
        private static double Y(double mm) => (297 - mm) * 72 / 25.4;

        public void Texto(double xmm, double ymm, double tamano, string texto, bool negrita = false, string color = "0 0 0", bool derecha = false)
        {
            var escapado = Codificar(texto);
            if (derecha) xmm -= Ancho(texto, tamano);
            Ops.Append($"BT /{(negrita ? "F2" : "F1")} {N(tamano)} Tf {color} rg {N(X(xmm))} {N(Y(ymm))} Td ({escapado}) Tj ET\n");
        }
        public void Linea(double x1, double y1, double x2, double y2, double grosor = 0.5, string color = "0 0 0") =>
            Ops.Append($"{color} RG {N(grosor)} w {N(X(x1))} {N(Y(y1))} m {N(X(x2))} {N(Y(y2))} l S\n");
        public void Rect(double x, double y, double w, double h, string? relleno = null, string? borde = "0 0 0", double grosor = 0.5)
        {
            if (relleno is not null) Ops.Append($"{relleno} rg {N(X(x))} {N(Y(y + h))} {N(w * 72 / 25.4)} {N(h * 72 / 25.4)} re f\n");
            if (borde is not null) Ops.Append($"{borde} RG {N(grosor)} w {N(X(x))} {N(Y(y + h))} {N(w * 72 / 25.4)} {N(h * 72 / 25.4)} re S\n");
        }
        /// <summary>Ancho aproximado en mm (Helvetica ~0,5 em de media).</summary>
        public static double Ancho(string t, double tamano) => t.Length * tamano * 0.5 * 25.4 / 72;
        private static string Codificar(string t)
        {
            var sb = new StringBuilder();
            foreach (var ch in t)
            {
                if (ch is '(' or ')' or '\\') sb.Append('\\').Append(ch);
                else if (ch < 128) sb.Append(ch);
                else { var b = Encoding.Latin1.GetBytes(ch.ToString())[0]; sb.Append(b == (byte)'?' ? "?" : $"\\{Convert.ToString(b, 8).PadLeft(3, '0')}"); }
            }
            return sb.ToString();
        }
    }

    private static byte[] Ensamblar(IReadOnlyList<Pagina> paginas)
    {
        var objetos = new List<string>();
        objetos.Add("<< /Type /Catalog /Pages 2 0 R >>");
        var kids = string.Join(" ", paginas.Select((_, i) => $"{5 + i * 2} 0 R"));
        objetos.Add($"<< /Type /Pages /Kids [{kids}] /Count {paginas.Count} >>");
        objetos.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        objetos.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
        foreach (var p in paginas)
        {
            int idx = objetos.Count + 1;
            objetos.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595.28 841.89] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {idx + 1} 0 R >>");
            var contenido = p.Ops.ToString();
            objetos.Add($"<< /Length {Encoding.Latin1.GetByteCount(contenido)} >>\nstream\n{contenido}\nendstream");
        }
        var sb = new StringBuilder("%PDF-1.4\n%âãÏÓ\n");
        var offsets = new List<int>();
        for (int i = 0; i < objetos.Count; i++)
        {
            offsets.Add(Encoding.Latin1.GetByteCount(sb.ToString()));
            sb.Append($"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
        }
        int xref = Encoding.Latin1.GetByteCount(sb.ToString());
        sb.Append($"xref\n0 {objetos.Count + 1}\n0000000000 65535 f \n");
        foreach (var o in offsets) sb.Append($"{o:0000000000} 00000 n \n");
        sb.Append($"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.Latin1.GetBytes(sb.ToString());
    }

    private static string E(decimal v) => v.ToString("N2", Infraestructura.Formato.Es);

    /// <summary>Factura de alquiler con la misma composicion que la real: emisor arriba a la izquierda, inquilino a la derecha, cajas de fecha y numero, tabla de conceptos y pie con base, IVA, IRPF y total.</summary>
    public byte[] FacturaAlquiler(FacturaEmitida f, Cliente emisor, ContratoAlquiler? contrato)
    {
        var p = new Pagina();
        const string gris = "0.94 0.94 0.94"; const string borde = "0.6 0.6 0.6";
        p.Texto(15, 28, 14, emisor.Nombre.ToUpperInvariant(), negrita: true);
        p.Texto(15, 34, 8, emisor.Direccion.ToUpperInvariant());
        p.Texto(15, 39, 8, $"{emisor.CodigoPostal}   {emisor.Localidad.ToUpperInvariant()}");
        p.Texto(120, 28, 10, f.DestinatarioNombre.ToUpperInvariant(), negrita: true);
        if (contrato is not null) { p.Texto(120, 34, 9, contrato.InquilinoDireccion.ToUpperInvariant()); }
        p.Texto(120, 45, 9, $"CIF/NIF {f.DestinatarioNif}");

        double y = 58;
        void Caja(double x, double w, string titulo, string valor) { p.Texto(x + 3, y, 8, titulo); p.Rect(x, y + 2, w, 7, borde: borde); p.Texto(x + 3, y + 7, 9, valor); }
        Caja(15, 32, "Fecha de Factura", Infraestructura.Formato.FechaCorta(f.Fecha));
        Caja(50, 24, "Factura", f.Numero);
        Caja(77, 28, "Cliente Num.", (f.ContratoId ?? 1).ToString("00000"));
        Caja(150, 30, "NIF Cliente", f.DestinatarioNif);

        p.Rect(15, 78, 180, 8, relleno: gris, borde: null);
        p.Texto(19, 84, 9.5, "Descripción", negrita: true);
        p.Texto(150, 84, 8.5, "Subtotal", negrita: true, derecha: true);
        p.Texto(190, 84, 8.5, "Importe", negrita: true, derecha: true);
        p.Rect(15, 78, 180, 150, borde: borde);
        p.Linea(125, 86, 125, 228, 0.3, borde); p.Linea(155, 86, 155, 228, 0.3, borde);
        p.Texto(19, 92, 8.5, f.Concepto.ToUpperInvariant());
        p.Texto(150, 92, 8.5, E(f.Base), derecha: true);
        p.Texto(190, 92, 8.5, E(f.Base), derecha: true);

        y = 233;
        p.Rect(15, y, 120, 20, borde: borde);
        p.Texto(18, y + 5, 8, "Base", negrita: true); p.Texto(38, y + 5, 8, "IVA", negrita: true); p.Texto(60, y + 5, 8, "Total  IVA", negrita: true); p.Texto(85, y + 5, 8, "IRPF", negrita: true); p.Texto(115, y + 6, 11, "Total", negrita: true);
        p.Texto(18, y + 13, 8.5, E(f.Base) + " €"); p.Texto(38, y + 13, 8.5, $"{f.TipoIva:0.00} %"); p.Texto(60, y + 13, 8.5, E(f.CuotaIva) + " €"); p.Texto(85, y + 13, 8.5, E(f.CuotaRetencion) + " €"); p.Texto(133, y + 14, 12, E(f.Total) + " €", negrita: true, derecha: true);
        p.Rect(15, y + 23, 85, 18, borde: borde);
        p.Texto(18, y + 30, 8, "Forma de pago:", negrita: true); p.Texto(43, y + 30, 8, emisor.FormaPago == FormaPago.Domiciliacion ? "DOMICILIACIÓN BANCARIA" : "MTE TRANSFERENCIA VTO FECHA FAC");
        p.Linea(15, 278, 195, 278, 0.4);
        p.Texto(15, 282, 6.5, $"{emisor.Nombre.ToUpperInvariant()}   {emisor.Direccion.ToUpperInvariant()}   {emisor.CodigoPostal} {emisor.Localidad.ToUpperInvariant()}   NIF: {emisor.Nif}");
        p.Texto(195, 282, 6.5, $"Retención IRPF {f.TipoRetencion:0} % · Emitida por SGA Contabilizado por cuenta del arrendador · DEMO", derecha: true, color: "0.45 0.45 0.45");
        return Ensamblar([p]);
    }

    /// <summary>Justificante de presentacion simulado, con la estructura del real (Registro, Presentador, aviso de simulacion) y una segunda pagina con el resumen del calculo.</summary>
    public byte[] Justificante(Presentacion pr, Obligacion o, Cliente c, Sga.Nucleo.Calculo.ResultadoIva? iva)
    {
        var p1 = new Pagina();
        const string borde = "0.2 0.2 0.2";
        p1.Rect(15, 15, 180, 22, borde: borde, grosor: 0.8);
        p1.Texto(60, 24, 11, "INFORMACIÓN DE LA PRESENTACIÓN DE LA DECLARACIÓN", negrita: true);
        p1.Texto(95, 33, 11, $"Modelo {o.Modelo}", negrita: true);
        p1.Rect(17, 17, 18, 18, relleno: "0.93 0.90 0.96", borde: null);
        p1.Texto(19, 28, 7, "SGA", negrita: true, color: "0.29 0.15 0.38");
        p1.Rect(15, 42, 180, 9, relleno: "0.98 0.85 0.85", borde: null);
        p1.Texto(18, 48, 8.5, "SIMULACIÓN DE DEMOSTRACIÓN: este justificante no procede de la Agencia Tributaria ni tiene validez alguna.", negrita: true, color: "0.55 0.1 0.1");
        p1.Texto(17, 60, 9, "Registro", negrita: true);
        p1.Rect(15, 62, 180, 34, borde: borde, grosor: 0.8);
        double y = 69;
        void Fila(Pagina p, string etiqueta, string valor) { p.Texto(20, y, 8.5, etiqueta, negrita: true); p.Texto(20 + Pagina.Ancho(etiqueta, 8.5) + 2, y, 8.5, valor); y += 6.5; }
        Fila(p1, "Presentación realizada el:", pr.FechaHora.ToLocalTime().ToString("dd-MM-yyyy 'a las' HH:mm:ss", Infraestructura.Formato.Es));
        Fila(p1, "Expediente/Referencia (nº registro asignado):", pr.Expediente);
        Fila(p1, "Código Seguro de Verificación:", pr.Csv);
        Fila(p1, "Número de justificante:", pr.NumeroJustificante);
        Fila(p1, "Vía de entrada:", pr.ViaEntrada);
        y = 108;
        p1.Texto(17, 104, 9, "Presentador", negrita: true);
        p1.Rect(15, 106, 180, 22, borde: borde, grosor: 0.8);
        y = 113;
        Fila(p1, "NIF Presentador:", pr.PresentadorNif);
        Fila(p1, "Apellidos y Nombre / Razón social:", pr.PresentadorNombre.ToUpperInvariant());
        Fila(p1, "En calidad de:", "Colaborador");
        y = 140;
        p1.Texto(17, 136, 9, "Declarante", negrita: true);
        p1.Rect(15, 138, 180, 16, borde: borde, grosor: 0.8);
        y = 145;
        Fila(p1, "NIF:", c.Nif);
        Fila(p1, "Apellidos y nombre o Razón social:", c.Nombre.ToUpperInvariant());
        if (pr.Importe > 0)
        {
            p1.Texto(17, 164, 9, "DOMICILIACIÓN DEL IMPORTE A INGRESAR", negrita: true);
            p1.Texto(17, 171, 8.5, $"Importe: {E(pr.Importe)} €     IBAN: {pr.Iban ?? "—"}");
        }
        else if (pr.Importe < 0) p1.Texto(17, 164, 9, $"RESULTADO A COMPENSAR: {E(-pr.Importe)} €", negrita: true);
        p1.Texto(105, 285, 7, $"La autenticidad de este documento (SIMULADO) se comprobaría mediante el Código Seguro de Verificación {pr.Csv}", color: "0.3 0.3 0.3");
        var paginas = new List<Pagina> { p1 };
        if (iva is not null && o.Modelo == "303")
        {
            var p2 = new Pagina();
            p2.Texto(15, 20, 12, $"Modelo 303 · Ejercicio {o.Ejercicio} · Período {o.Periodo}", negrita: true);
            p2.Texto(15, 27, 8.5, $"{c.Nif}   {c.Nombre.ToUpperInvariant()}");
            p2.Rect(15, 32, 180, 6, relleno: "0.98 0.85 0.85", borde: null); p2.Texto(18, 36.5, 7.5, "Resumen de la liquidación calculado por SGA. Simulación sin validez.", color: "0.55 0.1 0.1");
            y = 48;
            void Casilla(string num, string texto, decimal valor) { p2.Texto(18, y, 8, $"[{num}]", negrita: true); p2.Texto(30, y, 8, texto); p2.Texto(190, y, 8.5, E(valor), derecha: true); p2.Linea(15, y + 2, 195, y + 2, 0.2, "0.7 0.7 0.7"); y += 7; }
            p2.Texto(15, y, 9.5, "IVA devengado", negrita: true); y += 7;
            foreach (var d in iva.DesgloseRepercutido.Where(d => d.Tipo > 0)) { Casilla("07", $"Base imponible al {d.Tipo:0.##} % (acumulado del año)", d.Base); Casilla("09", $"Cuota al {d.Tipo:0.##} %", d.Cuota); }
            Casilla("27", "Total cuota devengada a liquidar en el período", iva.RepercutidoALiquidar);
            y += 4; p2.Texto(15, y, 9.5, "IVA deducible", negrita: true); y += 7;
            Casilla("28", "Base de cuotas soportadas en operaciones interiores (período)", iva.BaseSoportadaTrimestre);
            Casilla("29", "Cuota soportada deducible (período)", iva.SoportadoALiquidar);
            Casilla("45", "Total a deducir", iva.SoportadoALiquidar);
            y += 4;
            Casilla("46", "Resultado régimen general (27 - 45)", iva.Resultado);
            Casilla("71", "Resultado de la autoliquidación", iva.Resultado);
            paginas.Add(p2);
        }
        return Ensamblar(paginas);
    }
}
