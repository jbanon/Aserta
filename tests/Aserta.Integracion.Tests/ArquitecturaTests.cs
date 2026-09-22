using System.Reflection;

namespace Aserta.Integracion.Tests;

/// <summary>ADR-001 §2.2 y §5: la regla de dependencias entre proyectos se comprueba inspeccionando los ensamblados.</summary>
public class ArquitecturaTests
{
    private static IReadOnlyList<string> Referencias(Assembly a) => a.GetReferencedAssemblies().Select(r => r.Name ?? "").ToList();

    [Fact]
    public void Dominio_no_referencia_nada_fuera_de_la_BCL()
    {
        var refs = Referencias(typeof(Aserta.Dominio.Obligaciones.MotorObligaciones).Assembly);
        Assert.All(refs, r => Assert.True(r.StartsWith("System") || r == "netstandard" || r == "mscorlib", $"Dominio referencia {r}"));
    }

    [Fact]
    public void Aplicacion_no_referencia_Infraestructura_ni_AspNet()
    {
        var refs = Referencias(typeof(Aserta.Aplicacion.Clientes.ServicioClientes).Assembly);
        Assert.DoesNotContain(refs, r => r.StartsWith("Aserta.Infraestructura") || r.StartsWith("Aserta.Web") || r.StartsWith("Microsoft.AspNetCore"));
    }

    [Fact]
    public void Verifactu_no_referencia_EF_ni_AspNet_ni_Infraestructura()
    {
        var asm = Assembly.Load("Aserta.Verifactu");
        var refs = Referencias(asm);
        Assert.DoesNotContain(refs, r => r.StartsWith("Microsoft.EntityFrameworkCore") || r.StartsWith("Microsoft.AspNetCore") || r.StartsWith("Aserta.Infraestructura"));
    }

    [Fact]
    public void Dominio_csproj_no_tiene_PackageReference()
    {
        var csproj = File.ReadAllText(Path.Combine(FabricaAplicacion.RaizRepo(), "src", "Aserta.Dominio", "Aserta.Dominio.csproj"));
        Assert.DoesNotContain("PackageReference", csproj);
    }

    [Fact]
    public void wwwroot_lib_solo_contiene_htmx_y_sortable_y_no_hay_package_json()
    {
        var lib = Path.Combine(FabricaAplicacion.RaizWeb(), "wwwroot", "lib");
        var ficheros = Directory.GetFiles(lib).Select(Path.GetFileName).ToList();
        Assert.All(ficheros, f => Assert.True(f!.StartsWith("htmx-") || f.StartsWith("sortable-"), f));
        Assert.False(File.Exists(Path.Combine(FabricaAplicacion.RaizRepo(), "package.json")));
    }
}
