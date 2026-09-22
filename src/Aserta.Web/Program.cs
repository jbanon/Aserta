using System.Globalization;
using Aserta.Dominio.Nucleo;
using Aserta.Infraestructura;
using Aserta.Infraestructura.Identidad;
using Aserta.Infraestructura.Migraciones;
using Aserta.Infraestructura.Persistencia;
using Aserta.Infraestructura.Seed;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// --- Persistencia, contexto de ejecucion, servicios de aplicacion (Aserta.Infraestructura) ---
builder.Services.AddInfraestructuraAserta(config);

// --- Identity -------------------------------------------------------------------------------
builder.Services
    .AddIdentity<UsuarioIdentity, RolIdentity>(o =>
    {
        o.Password.RequiredLength = 10;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = true;
        o.Password.RequireDigit = true;
        o.Lockout.MaxFailedAccessAttempts = 6;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        o.User.RequireUniqueEmail = true;
        o.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<AsertaDbContext>()
    .AddClaimsPrincipalFactory<FabricaClaims>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "aserta.sesion";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.LoginPath = "/Cuenta/Entrar";
    o.LogoutPath = "/Cuenta/Salir";
    o.AccessDeniedPath = "/Cuenta/Denegado";
    o.ExpireTimeSpan = TimeSpan.FromHours(10);
    o.SlidingExpiration = true;
});
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(5));

// --- Antiforgery (ADR-003 §2.3): cabecera para htmx -------------------------------------------
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

// --- Autorizacion: politicas por rol --------------------------------------------------------------
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Politicas.Gestoria, p => p.RequireRole(Roles.SocioDirector, Roles.Asesor, Roles.Administrativo));
    o.AddPolicy(Politicas.SocioDirector, p => p.RequireRole(Roles.SocioDirector));
    o.AddPolicy(Politicas.AsesorOSocio, p => p.RequireRole(Roles.SocioDirector, Roles.Asesor));
    o.AddPolicy(Politicas.Cliente, p => p.RequireRole(Roles.ClienteAdmin, Roles.ClienteUsuario));
    o.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

builder.Services.AddRazorPages(o =>
{
    // Solo las paginas de entrada son anonimas; /Cuenta/Perfil y /Cuenta/Mfa/Configurar exigen sesion (FallbackPolicy).
    o.Conventions.AllowAnonymousToPage("/Cuenta/Entrar");
    o.Conventions.AllowAnonymousToPage("/Cuenta/Salir");
    o.Conventions.AllowAnonymousToPage("/Cuenta/Denegado");
    o.Conventions.AllowAnonymousToPage("/Cuenta/Mfa/Verificar");
    o.Conventions.AllowAnonymousToPage("/Error");
    o.Conventions.AuthorizeFolder("/Clientes", Politicas.Gestoria);
    o.Conventions.AuthorizeFolder("/Obligaciones", Politicas.Gestoria);
    o.Conventions.AuthorizeFolder("/Catalogo", Politicas.Gestoria);
    o.Conventions.AuthorizeFolder("/Auditoria", Politicas.Gestoria);
    o.Conventions.AuthorizeFolder("/Usuarios", Politicas.SocioDirector);
    o.Conventions.AuthorizeFolder("/Gestoria", Politicas.SocioDirector);
}).AddMvcOptions(o =>
{
    o.ModelBinderProviders.Insert(0, new ProveedorBinderDecimal());
});

// --- Data Protection fuera del directorio de despliegue (misma convencion que otros proyectos del servidor) ---
var rutaClaves = config["DataProtection:RutaClaves"];
if (!string.IsNullOrWhiteSpace(rutaClaves))
{
    Directory.CreateDirectory(rutaClaves);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(rutaClaves)).SetApplicationName("Aserta");
}

// --- IP real tras nginx (RD-08: la IP de auditoria viene de ForwardedHeaders) --------------------------
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
    o.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.Loopback, 8));
    o.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.IPv6Loopback, 128));
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// --- Cultura: es-ES para presentar; el binder de decimales admite coma y punto ---------------------------
var culturaEs = new CultureInfo("es-ES");
CultureInfo.DefaultThreadCurrentCulture = culturaEs;
CultureInfo.DefaultThreadCurrentUICulture = culturaEs;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culturaEs),
    SupportedCultures = [culturaEs],
    SupportedUICultures = [culturaEs],
});

// --- Arranque: migraciones y, si procede, datos de demo. Si fallan, la aplicacion NO arranca. ------------
using (var ambito = app.Services.CreateScope())
{
    var log = ambito.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var runner = ambito.ServiceProvider.GetRequiredService<RunnerMigraciones>();
    var resumen = await runner.AplicarAsync();
    log.LogInformation("Migraciones: {A} aplicadas, {O} ya estaban", resumen.Aplicados, resumen.Omitidos);

    if (config.GetValue<bool>("Demo:Sembrar"))
    {
        log.LogInformation("Sembrando datos de demostración…");
        await ambito.ServiceProvider.GetRequiredService<SembradorDemo>().SembrarAsync();
    }
}

app.UseForwardedHeaders();
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/Error");
app.UseStatusCodePagesWithReExecute("/Error", "?codigo={0}");

app.UseMiddleware<MiddlewareCabecerasSeguridad>();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<MiddlewareContextoEjecucion>();
app.UseMiddleware<MiddlewareMfaObligatorio>();
app.UseAuthorization();

app.MapRazorPages();
app.MapHealthChecks("/salud").AllowAnonymous();

app.Run();

public partial class Program { }
