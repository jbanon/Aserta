using System.Globalization;
using System.Net;
using Aserta.Dominio.Facturacion;
using Aserta.Verifactu.Qr;

namespace Aserta.Verifactu.Servicios;

/// <summary>
/// HTML de la factura para el PDF (qr-y-pdf.md §2 y §5): QR tributario al
/// principio, centrado, con "QR tributario:" encima y la leyenda de verificable
/// debajo, ambos con el tamaño del cuerpo. Tamaño del QR fijado en mm por CSS
/// de impresion (35 mm, dentro de 30-40). Sin recursos externos.
/// </summary>
public static class PlantillaFacturaHtml
{
    public const string Version = "1";
    public const string Leyenda = "Factura verificable en la sede electrónica de la AEAT";

    public static string Generar(FacturaEmitida f, EmisorFacturacion emisor, string? nombreRectificada, bool entornoPruebas)
    {
        var es = CultureInfo.GetCultureInfo("es-ES");
        string E(string? s) => WebUtility.HtmlEncode(s ?? "");
        string N(decimal d) => d.ToString("N2", es);
        var qr = GeneradorQrVerifactu.DataUriPng(f.UrlQr, 8);
        var lineas = string.Join("", f.Lineas.OrderBy(l => l.Orden).Select(l =>
            $"<tr><td>{E(l.Descripcion)}</td><td class='n'>{l.Cantidad.ToString("0.###", es)}</td><td class='n'>{N(l.PrecioUnitario)}</td><td class='n'>{(l.Exenta ? "Exenta" : N(l.TipoIva) + " %")}</td><td class='n'>{N(l.BaseLinea)}</td></tr>"));
        var desglose = string.Join("", f.Lineas.GroupBy(l => (l.Exenta ? 0m : l.TipoIva, l.Exenta, l.Exenta ? null : l.TipoRecargo)).OrderByDescending(g => g.Key.Item1).Select(g =>
        {
            var baseG = g.Sum(l => l.BaseLinea);
            var cuota = g.Key.Exenta ? 0m : Math.Round(baseG * g.Key.Item1 / 100m, 2, MidpointRounding.AwayFromZero);
            var recargo = g.Key.Item3 is decimal tr ? Math.Round(baseG * tr / 100m, 2, MidpointRounding.AwayFromZero) : 0m;
            return $"<tr><td>{(g.Key.Exenta ? "Exenta" : "IVA " + N(g.Key.Item1) + " %")}</td><td class='n'>{N(baseG)}</td><td class='n'>{N(cuota)}</td><td class='n'>{(g.Key.Item3 is decimal t2 ? N(t2) + " % · " + N(recargo) : "—")}</td></tr>";
        }));
        string tipo = f.TipoFactura.EsRectificativa() ? $"FACTURA RECTIFICATIVA ({f.TipoFactura})" : f.TipoFactura == TipoFactura.F2 ? "FACTURA SIMPLIFICADA" : "FACTURA";
        return $$"""
<!DOCTYPE html><html lang="es"><head><meta charset="utf-8"><title>{{E(f.NumSerieFactura)}}</title>
<style>
@page { size: A4; margin: 14mm 16mm; }
body { font-family: Helvetica, Arial, sans-serif; font-size: 10.5pt; color: #111; margin: 0; }
.qr { text-align: center; margin-bottom: 6mm; }
.qr img { width: 35mm; height: 35mm; display: block; margin: 2mm auto; }
.qr p { margin: 0; font-size: 10.5pt; }
h1 { font-size: 16pt; margin: 0 0 2mm; letter-spacing: .02em; }
.cab { display: flex; justify-content: space-between; gap: 10mm; border-bottom: 1px solid #999; padding-bottom: 4mm; margin-bottom: 4mm; }
.cab div { flex: 1; }
.eti { font-size: 8.5pt; color: #666; text-transform: uppercase; letter-spacing: .05em; }
table { width: 100%; border-collapse: collapse; margin-top: 4mm; }
th, td { padding: 1.6mm 2mm; border-bottom: 1px solid #ddd; text-align: left; vertical-align: top; }
th { font-size: 9pt; color: #444; background: #f3f3f3; }
td.n, th.n { text-align: right; font-variant-numeric: tabular-nums; }
.tot { margin-top: 4mm; margin-left: auto; width: 70mm; }
.tot td { border: 0; padding: 1mm 2mm; }
.tot tr.total td { border-top: 2px solid #111; font-weight: bold; font-size: 12pt; }
.pie { margin-top: 8mm; font-size: 8.5pt; color: #555; border-top: 1px solid #ccc; padding-top: 3mm; }
.aviso { background: #fff3cd; border: 1px solid #b45309; padding: 2mm; font-size: 9pt; margin-bottom: 4mm; }
</style></head><body>
<div class="qr"><p>QR tributario:</p><img src="{{qr}}" alt="QR tributario"><p>{{Leyenda}}</p></div>
{{(entornoPruebas ? "<div class='aviso'>ENTORNO DE PRUEBAS · datos ficticios de demostración · el QR apunta al portal de pruebas de la AEAT</div>" : "")}}
<div class="cab">
  <div><h1>{{tipo}}</h1><div class="eti">Número</div><div><strong>{{E(f.NumSerieFactura)}}</strong></div><div class="eti">Fecha de expedición</div><div>{{f.FechaExpedicion.ToString("dd/MM/yyyy")}}</div>
  {{(nombreRectificada is null ? "" : $"<div class='eti'>Rectifica a</div><div>{E(nombreRectificada)}</div><div class='eti'>Motivo</div><div>{E(f.MotivoRectificacion)}</div>")}}</div>
  <div><div class="eti">Emisor</div><div><strong>{{E(emisor.Nombre)}}</strong></div><div>NIF {{E(emisor.Nif)}}</div><div>{{E(emisor.Direccion)}}</div></div>
  <div><div class="eti">Destinatario</div>{{(f.DestinatarioNombre is null ? "<div>(factura simplificada)</div>" : $"<div><strong>{E(f.DestinatarioNombre)}</strong></div><div>NIF {E(f.DestinatarioNif)}</div>")}}</div>
</div>
<div class="eti">Concepto</div><div>{{E(f.Descripcion)}}</div>
<table><thead><tr><th>Descripción</th><th class="n">Cantidad</th><th class="n">Precio</th><th class="n">IVA</th><th class="n">Base</th></tr></thead><tbody>{{lineas}}</tbody></table>
<table><thead><tr><th>Tipo</th><th class="n">Base imponible</th><th class="n">Cuota IVA</th><th class="n">Recargo equivalencia</th></tr></thead><tbody>{{desglose}}</tbody></table>
<table class="tot"><tr><td>Base imponible</td><td class="n">{{N(f.BaseTotal)}} €</td></tr><tr><td>Cuota IVA</td><td class="n">{{N(f.CuotaTotal)}} €</td></tr>
{{(f.CuotaRecargoTotal != 0 ? $"<tr><td>Recargo de equivalencia</td><td class='n'>{N(f.CuotaRecargoTotal)} €</td></tr>" : "")}}
{{(f.RetencionTotal != 0 ? $"<tr><td>Retención IRPF {N(f.PorcentajeRetencion)} %</td><td class='n'>-{N(f.RetencionTotal)} €</td></tr>" : "")}}
<tr class="total"><td>TOTAL</td><td class="n">{{N(f.ImporteTotal)}} €</td></tr></table>
<div class="pie">Registro de facturación generado por un sistema informático de facturación en modo VERI*FACTU (RD 1007/2023). {{E(emisor.Nombre)}} · {{E(emisor.FormaJuridica)}}. Plantilla v{{Version}}.</div>
</body></html>
""";
    }
}
