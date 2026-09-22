using QRCoder;

namespace Aserta.Web.Infraestructura;

/// <summary>QR como data-URI PNG (la CSP permite img-src data:). Se reutilizara en la fase 4 para el QR de las facturas.</summary>
public static class GeneradorQr
{
    public static string DataUriPng(string contenido, int pixelesPorModulo = 5)
    {
        using var generador = new QRCodeGenerator();
        using var datos = generador.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(datos).GetGraphic(pixelesPorModulo);
        return "data:image/png;base64," + Convert.ToBase64String(png);
    }
}
