using Aserta.Aplicacion.Puertos;
using Microsoft.AspNetCore.DataProtection;

namespace Aserta.Infraestructura.Documental;

/// <summary>
/// Almacen en sistema de ficheros, fuera del directorio de despliegue, con
/// cifrado en reposo mediante ASP.NET Core Data Protection (AES-256-CBC + HMAC,
/// claves en DataProtection:RutaClaves). Nombre de fichero = clave opaca.
/// Sustituible por S3/Azure Blob sin tocar la aplicacion.
/// </summary>
public sealed class AlmacenDocumentalFicheros : IAlmacenDocumental
{
    private readonly string _raiz;
    private readonly IDataProtector _protector;

    public AlmacenDocumentalFicheros(string raiz, IDataProtectionProvider proteccion)
    {
        _raiz = raiz;
        _protector = proteccion.CreateProtector("Aserta.AlmacenDocumental.v1");
        Directory.CreateDirectory(_raiz);
    }

    private string Ruta(Guid clave)
    {
        var nombre = clave.ToString("N");
        return Path.Combine(_raiz, nombre[..2], nombre + ".bin");   // 256 subcarpetas para no amontonar
    }

    public async Task<Guid> GuardarAsync(Stream contenido, CancellationToken ct = default)
    {
        var clave = Guid.NewGuid();
        using var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria, ct);
        var cifrado = _protector.Protect(memoria.ToArray());
        var ruta = Ruta(clave);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        await File.WriteAllBytesAsync(ruta, cifrado, ct);
        return clave;
    }

    public async Task<Stream> AbrirAsync(Guid clave, CancellationToken ct = default)
    {
        var ruta = Ruta(clave);
        if (!File.Exists(ruta)) throw new FileNotFoundException("El fichero no está en el almacén documental.", ruta);
        var cifrado = await File.ReadAllBytesAsync(ruta, ct);
        return new MemoryStream(_protector.Unprotect(cifrado), writable: false);
    }

    public Task<bool> ExisteAsync(Guid clave, CancellationToken ct = default) => Task.FromResult(File.Exists(Ruta(clave)));
}
