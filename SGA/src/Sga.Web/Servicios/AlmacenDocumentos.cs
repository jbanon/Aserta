namespace Sga.Web.Servicios;

/// <summary>Ficheros subidos por los clientes (fotos de tickets, PDF). Carpeta local configurable (Demo:Almacen), fuera del repositorio.</summary>
public sealed class AlmacenDocumentos(IConfiguration config, IWebHostEnvironment entorno)
{
    private string Raiz => Path.GetFullPath(Path.Combine(entorno.ContentRootPath, config["Demo:Almacen"] ?? "../../almacen-sga"));

    public async Task<string> GuardarAsync(int clienteId, string nombreOriginal, Stream contenido, CancellationToken ct = default)
    {
        var carpeta = Path.Combine(Raiz, clienteId.ToString());
        Directory.CreateDirectory(carpeta);
        var ext = Path.GetExtension(nombreOriginal).ToLowerInvariant();
        if (ext.Length > 6 || ext.Any(c => !char.IsLetterOrDigit(c) && c != '.')) ext = ".bin";
        var nombre = $"{Guid.NewGuid():N}{ext}";
        await using var fs = File.Create(Path.Combine(carpeta, nombre));
        await contenido.CopyToAsync(fs, ct);
        return $"{clienteId}/{nombre}";
    }

    public Stream? Abrir(string ruta)
    {
        var completo = Path.GetFullPath(Path.Combine(Raiz, ruta));
        if (!completo.StartsWith(Raiz, StringComparison.Ordinal) || !File.Exists(completo)) return null;
        return File.OpenRead(completo);
    }
}
