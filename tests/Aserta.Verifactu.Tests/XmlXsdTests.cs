using Aserta.Verifactu.Xml;

namespace Aserta.Verifactu.Tests;

/// <summary>El XML generado valida contra los XSD oficiales de la AEAT versionados en Esquemas/ (regla V1 de especificacion.md).</summary>
public class XmlXsdTests
{
    private static readonly SistemaInformatico Sistema = new("Productor Demo SL", "B00000000", "Aserta", "AS", "1.0.0", "1");
    private static readonly ValidadorXsd Validador = new(RutaEsquemas());

    private static string RutaEsquemas()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Aserta.slnx"))) dir = dir.Parent;
        return Path.Combine(dir!.FullName, "src", "Aserta.Verifactu", "Esquemas");
    }

    private static DatosRegistroAlta Alta(bool primero, string numSerie = "A-2026-0001", string tipo = "F1", string? destinatario = "12345678Z") => new(
        new IdFactura("89890001K", numSerie, new DateOnly(2026, 3, 15)), "Emisor Ficticio SL", tipo, null, null, "Servicios profesionales marzo",
        destinatario, destinatario is null ? null : "Cliente Ficticio", [new LineaDesglose("01", "S1", false, null, 21m, 100m, 21m, null, null), new LineaDesglose("01", "S1", true, "E1", 0m, 50m, 0m, null, null)],
        21m, 171m, primero, primero ? null : new IdFactura("89890001K", "A-2026-0000", new DateOnly(2026, 3, 14)), primero ? null : new string('A', 64),
        "2026-03-15T10:00:00+01:00", new string('B', 64), Sistema);

    [Fact]
    public void Los_esquemas_oficiales_estan_versionados_y_cargan()
    {
        Assert.True(Validador.Disponible, Validador.ErrorCarga);
    }

    [Fact]
    public void Un_lote_con_alta_primer_registro_valida_contra_el_xsd()
    {
        var doc = ConstructorXmlRegistro.EnvioLote("89890001K", "Emisor Ficticio SL", [ConstructorXmlRegistro.RegistroAlta(Alta(primero: true))]);
        var errores = Validador.Validar(doc);
        Assert.True(errores.Count == 0, string.Join("\n", errores) + "\n" + doc);
    }

    [Fact]
    public void Un_lote_con_alta_encadenada_y_anulacion_valida()
    {
        var alta = ConstructorXmlRegistro.RegistroAlta(Alta(primero: false, numSerie: "A-2026-0002"));
        var anulacion = ConstructorXmlRegistro.RegistroAnulacion(new DatosRegistroAnulacion(new IdFactura("89890001K", "A-2026-0002", new DateOnly(2026, 3, 15)), false,
            new IdFactura("89890001K", "A-2026-0002", new DateOnly(2026, 3, 15)), new string('B', 64), "2026-03-15T11:00:00+01:00", new string('C', 64), Sistema));
        var doc = ConstructorXmlRegistro.EnvioLote("89890001K", "Emisor Ficticio SL", [alta, anulacion]);
        var errores = Validador.Validar(doc);
        Assert.True(errores.Count == 0, string.Join("\n", errores));
    }

    [Fact]
    public void Rectificativa_y_simplificada_validan()
    {
        var r1 = Alta(primero: false, numSerie: "R-2026-0001", tipo: "R1") with { TipoRectificativa = "I", FacturaRectificada = new IdFactura("89890001K", "A-2026-0001", new DateOnly(2026, 3, 15)) };
        var f2 = Alta(primero: false, numSerie: "T-2026-0001", tipo: "F2", destinatario: null);
        var doc = ConstructorXmlRegistro.EnvioLote("89890001K", "Emisor Ficticio SL", [ConstructorXmlRegistro.RegistroAlta(r1), ConstructorXmlRegistro.RegistroAlta(f2)]);
        var errores = Validador.Validar(doc);
        Assert.True(errores.Count == 0, string.Join("\n", errores));
    }

    [Fact]
    public void Un_xml_invalido_se_detecta()
    {
        var alta = ConstructorXmlRegistro.RegistroAlta(Alta(primero: true));
        alta.Element(ConstructorXmlRegistro.NsSF + "TipoFactura")!.Value = "F9";
        var errores = Validador.Validar(ConstructorXmlRegistro.EnvioLote("89890001K", "Emisor", [alta]));
        Assert.NotEmpty(errores);
    }

    [Fact]
    public void El_sistema_informatico_respeta_las_longitudes_del_xsd()
    {
        Assert.Throws<ArgumentException>(() => new SistemaInformatico("x", "B00000000", "Aserta", "ASE", "1", "1").Validar());
        Assert.Throws<ArgumentException>(() => new SistemaInformatico("x", "B00000000", new string('n', 31), "AS", "1", "1").Validar());
    }
}
