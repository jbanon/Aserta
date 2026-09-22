using Aserta.Infraestructura;
using Aserta.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aserta.Integracion.Tests;

/// <summary>
/// Levanta la aplicacion real (con runner de migraciones) contra la base Aserta
/// de desarrollo. La cadena de conexion sale de los user-secrets del proyecto
/// web (mismo UserSecretsId) o de la variable ConnectionStrings__Aserta.
/// El seed de demo se desactiva: los tests crean sus propios tenants de prueba.
/// </summary>
public sealed class FabricaAplicacion : WebApplicationFactory<Program>
{
    public static readonly Guid TenantA = Guid.Parse("7e57a000-0000-0000-0000-00000000000a");
    public static readonly Guid TenantB = Guid.Parse("7e57b000-0000-0000-0000-00000000000b");
    public const string ContrasenaTecnico = "Pruebas-Aserta-2026!";
    public static string EmailTecnico(Guid tenant) => $"tecnico-{tenant.ToString()[..8]}@pruebas.aserta.local";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseContentRoot(RaizWeb());
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddUserSecrets(typeof(Program).Assembly, optional: true);
            cfg.AddEnvironmentVariables();
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Demo:Sembrar"] = "false",
                ["Migraciones:Carpeta"] = Path.Combine(RaizRepo(), "Scripts", "Migrations"),
            });
        });
    }

    public static string RaizRepo()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Aserta.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("No se encuentra la raíz del repositorio (Aserta.slnx).");
    }

    public static string RaizWeb() => Path.Combine(RaizRepo(), "src", "Aserta.Web");

    /// <summary>Ambito de DI con el contexto de ejecucion ya posicionado en un tenant (o en mantenimiento).</summary>
    public IServiceScope AmbitoComo(Guid? gestoriaId, bool mantenimiento = false)
    {
        var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ContextoEjecucion>();
        if (mantenimiento) ctx.AbrirAmbitoMantenimiento();
        else if (gestoriaId is Guid g) ctx.AbrirAmbitoTenant(g);
        return scope;
    }

    /// <summary>Crea (si no existen) dos gestorias de prueba, cada una con un cliente. Idempotente.</summary>
    public async Task AsegurarTenantsDePruebaAsync()
    {
        using var scope = AmbitoComo(null, mantenimiento: true);
        var db = scope.ServiceProvider.GetRequiredService<AsertaDbContext>();
        foreach (var (id, nombre, nif) in new[] { (TenantA, "Gestoría de prueba A", "B00000000"), (TenantB, "Gestoría de prueba B", "B11111111") })
        {
            if (!await db.Gestorias.AnyAsync(g => g.Id == id))
                db.Gestorias.Add(new Dominio.Nucleo.Gestoria { Id = id, Nombre = nombre, Nif = nif, FechaAlta = new DateOnly(2026, 1, 1) });
        }
        await db.SaveChangesAsync();

        foreach (var (tenant, nif) in new[] { (TenantA, "A0000000J"), (TenantB, "A1111111H") })
        {
            // Un usuario "tecnico" por tenant (identidad + dbo.Usuario + rol SocioDirector): asesor de la FK y sesion para los tests HTTP.
            var asesorId = await AsegurarUsuarioTecnicoAsync(scope.ServiceProvider, tenant);
            if (await db.Clientes.AnyAsync(c => c.GestoriaId == tenant && c.Nif == nif)) continue;
            db.Clientes.Add(new Dominio.Clientes.Cliente
            {
                Id = Guid.NewGuid(), GestoriaId = tenant, Nif = nif, RazonSocial = $"Cliente de {tenant.ToString()[..8]}",
                FormaJuridica = Dominio.Clientes.FormaJuridica.SL, AsesorResponsableId = asesorId, FechaAlta = new DateOnly(2026, 1, 1)
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> AsegurarUsuarioTecnicoAsync(IServiceProvider sp, Guid tenant)
    {
        var db = sp.GetRequiredService<AsertaDbContext>();
        var um = sp.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Infraestructura.Identidad.UsuarioIdentity>>();
        var rm = sp.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Infraestructura.Identidad.RolIdentity>>();
        foreach (var rol in Dominio.Nucleo.Roles.Todos)
            if (!await rm.RoleExistsAsync(rol)) await rm.CreateAsync(new Infraestructura.Identidad.RolIdentity(rol));

        var id = Guid.NewGuid();
        var email = EmailTecnico(tenant);
        var identidad = await um.FindByEmailAsync(email);
        if (identidad is null)
        {
            identidad = new Infraestructura.Identidad.UsuarioIdentity { Id = id, UserName = email, Email = email, EmailConfirmed = true };
            var r = await um.CreateAsync(identidad, ContrasenaTecnico);
            if (!r.Succeeded) throw new InvalidOperationException(string.Join("; ", r.Errors.Select(x => x.Description)));
        }
        else id = identidad.Id;
        if (!await um.IsInRoleAsync(identidad, Dominio.Nucleo.Roles.SocioDirector)) await um.AddToRoleAsync(identidad, Dominio.Nucleo.Roles.SocioDirector);
        if (!await db.Usuarios.AnyAsync(u => u.Id == id))
            db.Usuarios.Add(new Dominio.Nucleo.Usuario { Id = id, GestoriaId = tenant, NombreCompleto = "Usuario técnico de pruebas" });
        await db.SaveChangesAsync();
        return id;
    }
}

[CollectionDefinition("aplicacion")]
public sealed class ColeccionAplicacion : ICollectionFixture<FabricaAplicacion> { }
