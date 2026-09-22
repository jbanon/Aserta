using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Nucleo;
using Aserta.Aplicacion.Obligaciones;
using Aserta.Aplicacion.Puertos;
using Aserta.Infraestructura.Identidad;
using Aserta.Infraestructura.Migraciones;
using Aserta.Infraestructura.Notificaciones;
using Aserta.Infraestructura.Trabajos;
using Aserta.Infraestructura.Persistencia;
using Aserta.Infraestructura.Persistencia.Interceptores;
using Aserta.Infraestructura.Seed;
using Aserta.Infraestructura.Tiempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aserta.Infraestructura;

public static class ExtensionesServicios
{
    public const string NombreCadenaConexion = "Aserta";

    public static string CadenaConexion(IConfiguration config) =>
        config.GetConnectionString(NombreCadenaConexion)
        ?? throw new InvalidOperationException(
            "Falta ConnectionStrings:Aserta. En desarrollo: dotnet user-secrets set \"ConnectionStrings:Aserta\" \"...\" --project src/Aserta.Web. " +
            "En el servicio: variable de entorno ConnectionStrings__Aserta (ver docs/12-infraestructura-despliegue.md §2.1).");

    /// <summary>Registra persistencia, contexto de ejecucion, reloj, auditoria y servicios de aplicacion.</summary>
    public static IServiceCollection AddInfraestructuraAserta(this IServiceCollection servicios, IConfiguration config)
    {
        var cadena = CadenaConexion(config);
        int desplazamiento = config.GetValue<int>("Demo:DesplazamientoDias");

        servicios.AddScoped<ContextoEjecucion>();
        servicios.AddScoped<IContextoTenant>(sp => sp.GetRequiredService<ContextoEjecucion>());
        servicios.AddScoped<IContextoUsuarioActual>(sp => sp.GetRequiredService<ContextoEjecucion>());
        servicios.AddSingleton<IRelojSistema>(new RelojSistema(desplazamiento));

        servicios.AddScoped<InterceptorSesionTenant>();
        servicios.AddScoped<InterceptorAuditoria>();

        servicios.AddDbContext<AsertaDbContext>((sp, opciones) =>
        {
            opciones.UseSqlServer(cadena, sql =>
            {
                sql.EnableRetryOnFailure(3);
                sql.CommandTimeout(60);
            });
            opciones.AddInterceptors(sp.GetRequiredService<InterceptorSesionTenant>(), sp.GetRequiredService<InterceptorAuditoria>());
        });
        servicios.AddScoped<IAsertaDb>(sp => sp.GetRequiredService<AsertaDbContext>());

        servicios.AddScoped<IRegistroAuditoria, RegistroAuditoria>();
        servicios.AddScoped<IGestorIdentidad, GestorIdentidad>();

        servicios.AddScoped<ServicioClientes>();
        servicios.AddScoped<ServicioGeneracionObligaciones>();
        servicios.AddScoped<ServicioObligaciones>();
        servicios.AddScoped<ServicioTablero>();
        servicios.AddScoped<ServicioAvisosVencimiento>();
        servicios.AddScoped<ITareaDiaria>(sp => sp.GetRequiredService<ServicioAvisosVencimiento>());
        servicios.AddScoped<INotificador, NotificadorEnPantalla>();
        servicios.AddHostedService<PlanificadorDiario>();
        servicios.AddScoped<ServicioUsuarios>();
        servicios.AddScoped<ServicioGestoria>();
        servicios.AddScoped<SembradorDemo>();

        servicios.AddSingleton(sp => new RunnerMigraciones(
            cadena,
            config["Migraciones:Carpeta"] ?? Path.Combine(AppContext.BaseDirectory, "Scripts", "Migrations"),
            sp.GetRequiredService<ILogger<RunnerMigraciones>>()));

        return servicios;
    }
}
