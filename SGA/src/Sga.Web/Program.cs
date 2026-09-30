using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calendario;
using Sga.Web.Datos;
using Sga.Web.Infraestructura;
using Sga.Web.Servicios;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

// Proveedor de datos: SQL Server (base "SGA") por defecto; SQLite en fichero si Datos:Proveedor = "Sqlite" (demo portatil, sin administrador de base de datos)
var proveedor = builder.Configuration["Datos:Proveedor"] ?? "SqlServer";
var usaSqlite = proveedor.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);
var rutaSqlite = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Datos:Sqlite"] ?? "../../sga-demo.db"));
if (usaSqlite) builder.Services.AddDbContext<SgaDb>(o => o.UseSqlite($"Data Source={rutaSqlite}"));
else
{
    var cadena = builder.Configuration.GetConnectionString("SGA")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:SGA (dotnet user-secrets en desarrollo; variable de entorno ConnectionStrings__SGA en el servidor) o Datos:Proveedor = Sqlite.");
    builder.Services.AddDbContext<SgaDb>(o => o.UseSqlServer(cadena, sql => sql.CommandTimeout(60)));
}

// Reloj de la demo: fijo salvo que se pida el real
var demo = builder.Configuration.GetSection("Demo");
if (demo.GetValue<bool>("RelojReal")) builder.Services.AddSingleton<IReloj, RelojReal>();
else builder.Services.AddSingleton<IReloj>(new RelojFijo(DateOnly.Parse(demo["Hoy"] ?? "2026-10-06", CultureInfo.InvariantCulture)));

builder.Services.Configure<OpcionesSga>(builder.Configuration.GetSection("Sga"));
builder.Services.AddScoped<ServicioIva>();
builder.Services.AddScoped<ServicioCliente>();
builder.Services.AddScoped<ServicioGestor>();
builder.Services.AddScoped<ServicioFacturacion>();
builder.Services.AddScoped<ServicioPresentacion>();
builder.Services.AddScoped<AlmacenDocumentos>();
builder.Services.AddScoped<GeneradorPdf>();

builder.Services.AddAuthentication(Sesion.Esquema).AddCookie(o =>
{
    o.Cookie.Name = "sga.sesion";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.LoginPath = "/acceso";
    o.AccessDeniedPath = "/acceso";
    o.SlidingExpiration = true;
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Cliente", p => p.RequireClaim(Sesion.ClaimPerfil, "cliente"));
    o.AddPolicy("Gestor", p => p.RequireClaim(Sesion.ClaimPerfil, "gestor"));
});
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/Cliente", "Cliente");
    o.Conventions.AuthorizeFolder("/Gestor", "Gestor");
});
builder.Services.Configure<ForwardedHeadersOptions>(o => { o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto; o.KnownNetworks.Clear(); o.KnownProxies.Clear(); });

var app = builder.Build();

// Esquema y datos de demostracion
using (var ambito = app.Services.CreateScope())
{
    var db = ambito.ServiceProvider.GetRequiredService<SgaDb>();
    var reloj = ambito.ServiceProvider.GetRequiredService<IReloj>();
    if (demo.GetValue<bool>("Reiniciar"))
    {
        // Reinicio de la demo: vacia el esquema SIN borrar la base (agente_ro no puede crear bases). En SQLite se borra el fichero.
        if (usaSqlite) { if (File.Exists(rutaSqlite)) File.Delete(rutaSqlite); }
        else if (await db.Database.CanConnectAsync()) await db.Database.ExecuteSqlRawAsync(@"
            DECLARE @sql nvarchar(max) = N'';
            SELECT @sql += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' FROM sys.foreign_keys fk JOIN sys.tables t ON t.object_id = fk.parent_object_id JOIN sys.schemas s ON s.schema_id = t.schema_id;
            SELECT @sql += N'DROP TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';' FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id;
            EXEC sp_executesql @sql;");
    }
    await db.Database.EnsureCreatedAsync();
    app.Logger.LogInformation("Base de datos: {Proveedor}{Detalle}", usaSqlite ? "SQLite" : "SQL Server", usaSqlite ? " · " + rutaSqlite : "");
    await Sembrador.SembrarSiVaciaAsync(db, reloj, app.Logger);
}

app.UseForwardedHeaders();
var cultura = new CultureInfo("es-ES");
app.UseRequestLocalization(new RequestLocalizationOptions { DefaultRequestCulture = new RequestCulture(cultura), SupportedCultures = [cultura], SupportedUICultures = [cultura] });
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseStatusCodePagesWithReExecute("/error", "?codigo={0}");
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: blob:; style-src 'self' 'unsafe-inline'; script-src 'self'; font-src 'self'; frame-ancestors 'none'; form-action 'self'; base-uri 'self'";
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"] = "camera=(self), geolocation=()";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapGet("/salud", () => Results.Ok(new { estado = "ok", app = "sga-demo" }));
app.Run();

public sealed class OpcionesSga
{
    public string Nombre { get; set; } = "SGA Contabilizado";
    public string Direccion { get; set; } = "";
    public string CodigoPostal { get; set; } = "";
    public string Localidad { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Email { get; set; } = "";
    public string Nif { get; set; } = "";
    public string Horario { get; set; } = "";
}
