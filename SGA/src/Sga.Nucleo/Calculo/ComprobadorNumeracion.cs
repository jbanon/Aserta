using System.Text.RegularExpressions;

namespace Sga.Nucleo.Calculo;

public readonly record struct FacturaNumerada(string Numero, DateOnly Fecha);

public enum TipoAnomalia { Hueco, Duplicado, FechaDesordenada, FormatoDistinto }

public sealed record Anomalia(TipoAnomalia Tipo, string Numero, string Detalle);

public sealed record InformeNumeracion(int Total, string? Serie, IReadOnlyList<Anomalia> Anomalias)
{
    public bool Correcta => Anomalias.Count == 0;
    public int Huecos => Anomalias.Count(a => a.Tipo == TipoAnomalia.Hueco);
    public int Duplicados => Anomalias.Count(a => a.Tipo == TipoAnomalia.Duplicado);
    public int Desordenadas => Anomalias.Count(a => a.Tipo == TipoAnomalia.FechaDesordenada);
}

/// <summary>
/// Comprobacion de la numeracion de las facturas que emite el propio cliente (perfil B2 y sociedades):
/// huecos en la serie, numeros repetidos y fechas que no siguen el orden de los numeros.
/// Entiende "AF-2026-35", "2026/035", "F-12" y numeros a secas: la parte numerica final es la secuencia.
/// </summary>
public static partial class ComprobadorNumeracion
{
    [GeneratedRegex(@"^(?<serie>.*?)(?<num>\d+)$")]
    private static partial Regex Patron();

    public static InformeNumeracion Analizar(IEnumerable<FacturaNumerada> facturas)
    {
        var lista = facturas.ToList();
        var anomalias = new List<Anomalia>();
        var parseadas = new List<(FacturaNumerada F, string Serie, long Num)>();
        foreach (var f in lista)
        {
            var m = Patron().Match(f.Numero.Trim());
            if (!m.Success) { anomalias.Add(new(TipoAnomalia.FormatoDistinto, f.Numero, "No termina en un número: no se puede comprobar la secuencia.")); continue; }
            parseadas.Add((f, m.Groups["serie"].Value, long.Parse(m.Groups["num"].Value)));
        }
        var series = parseadas.GroupBy(p => p.Serie).ToList();
        if (series.Count > 1)
            foreach (var s in series.OrderByDescending(g => g.Count()).Skip(1))
                foreach (var p in s) anomalias.Add(new(TipoAnomalia.FormatoDistinto, p.F.Numero, $"Serie «{p.Serie}» distinta de la principal «{series.OrderByDescending(g => g.Count()).First().Key}»."));

        var principal = series.OrderByDescending(g => g.Count()).FirstOrDefault();
        if (principal is not null)
        {
            var orden = principal.OrderBy(p => p.Num).ThenBy(p => p.F.Fecha).ToList();
            foreach (var dup in orden.GroupBy(p => p.Num).Where(g => g.Count() > 1))
                anomalias.Add(new(TipoAnomalia.Duplicado, dup.First().F.Numero, $"El número aparece {dup.Count()} veces ({string.Join(", ", dup.Select(d => d.F.Fecha.ToString("dd/MM")))})."));
            for (int i = 1; i < orden.Count; i++)
            {
                var salto = orden[i].Num - orden[i - 1].Num;
                if (salto > 1)
                    anomalias.Add(new(TipoAnomalia.Hueco, orden[i].F.Numero, salto == 2 ? $"Falta el número {Formatear(orden[i].Serie, orden[i - 1].Num + 1, orden[i].F.Numero)}." : $"Faltan {salto - 1} números entre {orden[i - 1].F.Numero} y {orden[i].F.Numero}."));
                if (orden[i].Num != orden[i - 1].Num && orden[i].F.Fecha < orden[i - 1].F.Fecha)
                    anomalias.Add(new(TipoAnomalia.FechaDesordenada, orden[i].F.Numero, $"Tiene fecha {orden[i].F.Fecha:dd/MM/yyyy}, anterior a la de {orden[i - 1].F.Numero} ({orden[i - 1].F.Fecha:dd/MM/yyyy})."));
            }
        }
        return new InformeNumeracion(lista.Count, principal?.Key, anomalias);
    }

    private static string Formatear(string serie, long numero, string ejemplo)
    {
        var m = Patron().Match(ejemplo);
        var ancho = m.Success ? m.Groups["num"].Value.Length : 1;
        return serie + numero.ToString().PadLeft(ancho, '0');
    }
}
