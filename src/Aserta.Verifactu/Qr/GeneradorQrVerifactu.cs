using QRCoder;

namespace Aserta.Verifactu.Qr;

/// <summary>QR ISO/IEC 18004 con nivel de correccion M (qr-y-pdf.md §1), generado en servidor, sin servicios externos.</summary>
public static class GeneradorQrVerifactu
{
    public static byte[] Png(string url, int pixelesPorModulo = 6)
    {
        using var generador = new QRCodeGenerator();
        using var datos = generador.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        return new PngByteQRCode(datos).GetGraphic(pixelesPorModulo);
    }

    public static string DataUriPng(string url, int pixelesPorModulo = 6) => "data:image/png;base64," + Convert.ToBase64String(Png(url, pixelesPorModulo));

    /// <summary>Numero de modulos por lado (para fijar el tamaño fisico en el CSS de impresion).</summary>
    public static int Modulos(string url)
    {
        using var generador = new QRCodeGenerator();
        using var datos = generador.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        return datos.ModuleMatrix.Count;
    }
}
