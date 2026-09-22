using Aserta.Dominio.Catalogo;

namespace Aserta.Dominio.Documental;

/// <summary>Regla de catalogo: que documento se espera, con que periodicidad y cuantos, segun el perfil fiscal.</summary>
public class ReglaRequisito
{
    public int Id { get; set; }
    public string Clave { get; set; } = string.Empty;
    public TipoDocumento TipoDocumento { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public Periodicidad Periodicidad { get; set; }
    public int? CantidadPorMes { get; set; }
    public bool Obligatorio { get; set; } = true;
    public int Prioridad { get; set; } = 100;
    public bool Activa { get; set; } = true;
    public List<ReglaRequisitoCondicion> Condiciones { get; set; } = [];

    public bool SeCumple(IReadOnlyDictionary<string, string> atributos) => Activa && Condiciones.All(c => c.SeCumple(atributos));
}

/// <summary>Misma semantica que cat.ReglaCondicion, en su propia tabla (cat.ReglaRequisitoCondicion).</summary>
public class ReglaRequisitoCondicion
{
    public int Id { get; set; }
    public int ReglaId { get; set; }
    public string Atributo { get; set; } = string.Empty;
    public string Operador { get; set; } = "=";
    public string Valor { get; set; } = string.Empty;

    public bool SeCumple(IReadOnlyDictionary<string, string> atributos) =>
        new ReglaCondicion { Atributo = Atributo, Operador = Operador, Valor = Valor }.SeCumple(atributos);
}
