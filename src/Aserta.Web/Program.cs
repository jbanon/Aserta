using System.Globalization;
using Aserta.Dominio.Nucleo;
using Aserta.Infraestructura;
using Aserta.Infraestructura.Identidad;
using Aserta.Infraestructura.Migraciones;
using Aserta.Infraestructura.Persistencia;
using Aserta.Infraestructura.Seed;
using Aserta.Infraestructura.Facturacion;
using Aserta.Verifactu.Servicios;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// --- Persistencia, contexto de ejecucion, servicios de aplicacion (Aserta.Infraestructura) ---
builder.Services.AddInfraestructuraAserta(config);

// --- Exportadores contables (Aserta.Exportacion): CSV generico disponible; A3 modelado sin especificacion (DA-07) ---
foreach (var exportador in Aserta.Exportacion.RegistroExportadores.Todos())
    builder.Services.AddSingleton<Aserta.Dominio.Exportacion.IExportadorContable>(exportador);

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
    o.Conventions.AuthorizeFolder("/Avisos", Politicas.Gestoria);
    o.Conventions.AuthorizeFolder("/Documentos", Politicas.Gestoria);
    o.Conventions.AllowAnonymousToPage("/Documentos/Contenido"); // se vuelve a autorizar abajo: el servicio exige sesion y comprueba cliente/tenant
    o.Conventions.AuthorizePage("/Documentos/Contenido");
    o.Conventions.AuthorizeFolder("/Portal", Politicas.Cliente);
    o.Conventions.AuthorizeFolder("/Facturacion", Politicas.Gestoria);
    o.Conventions.AuthorizeFolder("/Exportacion", Politicas.Gestoria);
    o.Conventions.AuthorizeFolder("/CuadroMando", Politicas.AsesorOSocio);
    o.Conventions.AllowAnonymousToPage("/Facturacion/Pdf");     // gestoria y cliente emisor: el modelo exige sesion y comprueba el cliente (la carpeta exige rol de gestoria)
    o.Conventions.AllowAnonymousToPage("/DeclaracionResponsable/Index");
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

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = 25 * 1024 * 1024);
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

// --- Comprobaciones de arranque del modulo Veri*Factu (la aplicacion NO arranca si fallan) ----------------------
{
    var vf = app.Services.GetRequiredService<OpcionesVerifactu>();
    if (app.Environment.IsProduction() && vf.UsarSimulador)
        throw new InvalidOperationException("Configuración vetada: entorno Production con Verifactu:UsarSimulador = true (envio-y-cola-reintentos.md §6).");
    if (app.Environment.IsProduction() && vf.DeclaracionResponsable.DatosDeDemostracion)
        throw new InvalidOperationException("Configuración vetada: entorno Production con datos de demostración en la declaración responsable (DA-02).");
    if (!string.Equals(vf.Sistema.Version, vf.DeclaracionResponsable.Version, StringComparison.Ordinal) ||
        !string.Equals(vf.Sistema.IdSistemaInformatico, vf.DeclaracionResponsable.IdSistemaInformatico, StringComparison.Ordinal) ||
        !string.Equals(vf.Sistema.NifProductor, vf.DeclaracionResponsable.Nif, StringComparison.Ordinal))
        throw new InvalidOperationException("La declaración responsable y el bloque SistemaInformatico no coinciden (versión, identificador o NIF del productor). declaracion-responsable.md §3.1.");
    vf.SistemaInformatico.Validar();
}

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

    // Historico de la declaracion responsable: una fila por version desplegada
    await RegistrarDeclaracionResponsableAsync(ambito.ServiceProvider);

    // Chromium para PDF: se comprueba al arrancar; si falta, respaldo basico (Pdf:ExigirChromium = true lo convierte en fallo de arranque)
    var chromium = ambito.ServiceProvider.GetRequiredService<GeneradorPdfPlaywright>();
    if (string.Equals(config["Pdf:Motor"], "Basico", StringComparison.OrdinalIgnoreCase))
        log.LogWarning("PDF: motor básico configurado (sin Chromium)");
    else if (!await chromium.ComprobarAsync())
    {
        if (config.GetValue<bool>("Pdf:ExigirChromium")) throw new InvalidOperationException("Chromium no disponible para generar PDF: " + chromium.UltimoError);
        log.LogWarning("PDF: Chromium no disponible ({Error}); se usará el generador básico sin navegador", chromium.UltimoError);
    }
    else log.LogInformation("PDF: Chromium disponible (Playwright)");
}

static async Task RegistrarDeclaracionResponsableAsync(IServiceProvider sp)
{
    var vf = sp.GetRequiredService<OpcionesVerifactu>();
    var db = sp.GetRequiredService<AsertaDbContext>();
    using var mantenimiento = sp.GetRequiredService<ContextoEjecucion>().AbrirAmbitoMantenimiento();
    var d = vf.DeclaracionResponsable;
    var contenido = System.Text.Json.JsonSerializer.Serialize(new { d.RazonSocial, d.Nif, d.Domicilio, d.NombreSistema, d.IdSistemaInformatico, d.Version, vf.Sistema.NumeroInstalacion, d.Componentes, Modalidad = "Solo VERI*FACTU", d.FechaSuscripcion, d.LugarSuscripcion, d.Firmante, d.DatosDeDemostracion });
    var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(contenido)));
    var existente = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.DeclaracionesResponsables, h => h.Version == d.Version);
    if (existente is null)
    {
        db.DeclaracionesResponsables.Add(new Aserta.Dominio.Facturacion.DeclaracionResponsableHistorico
        {
            Version = d.Version, FechaSuscripcion = DateOnly.TryParseExact(d.FechaSuscripcion, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var f) ? f : DateOnly.FromDateTime(DateTime.Today),
            Contenido = contenido, HashSha256 = hash, FechaRegistroUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }
    else if (existente.HashSha256 != hash)
        sp.GetRequiredService<ILogger<Program>>().LogWarning("La declaración responsable de la versión {Version} ha cambiado sin cambiar de versión: revise la configuración", d.Version);
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
