namespace Aserta.Dominio.Catalogo;

public class ReglaCondicion
{
    public int Id { get; set; }
    public int ReglaId { get; set; }
    public string Atributo { get; set; } = string.Empty;
    /// <summary>"=", "&lt;&gt;" o "IN".</summary>
    public string Operador { get; set; } = "=";
    /// <summary>Para IN: valores separados por coma.</summary>
    public string Valor { get; set; } = string.Empty;

    public bool SeCumple(IReadOnlyDictionary<string, string> atributos)
    {
        if (!atributos.TryGetValue(Atributo, out var actual))
            throw new InvalidOperationException($"La condicion hace referencia a un atributo desconocido: '{Atributo}'.");

        var valores = Valor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        bool contenido = valores.Any(v => string.Equals(v, actual, StringComparison.OrdinalIgnoreCase));
        return Operador switch
        {
            "=" => contenido,
            "<>" => !contenido,
            "IN" => contenido,
            _ => throw new InvalidOperationException($"Operador de condicion no soportado: '{Operador}'.")
        };
    }

    public override string ToString() => $"{Atributo} {Operador} {Valor}";
}
