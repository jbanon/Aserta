using System.Collections.Concurrent;

namespace Sga.Web.Pages.Shared;

/// <summary>Lee una sola vez los ficheros estaticos que se incrustan en linea (el SVG animado).</summary>
public static class CacheEstaticos
{
    private static readonly ConcurrentDictionary<string, string> _cache = new();
    public static string Leer(string ruta) => _cache.GetOrAdd(ruta, r => File.Exists(r) ? File.ReadAllText(r) : "");
}
