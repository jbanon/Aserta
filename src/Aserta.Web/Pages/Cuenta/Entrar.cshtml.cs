using System.ComponentModel.DataAnnotations;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Infraestructura.Identidad;
using Aserta.Infraestructura.Persistencia;
using Aserta.Infraestructura.Seed;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Web.Pages.Cuenta;

[AllowAnonymous]
public class EntrarModel : PaginaBase
{
    private readonly SignInManager<UsuarioIdentity> _signIn;
    private readonly UserManager<UsuarioIdentity> _usuarios;
    private readonly IAsertaDb _db;
    private readonly ContextoEjecucion _contexto;
    private readonly IRegistroAuditoria _auditoria;
    private readonly IConfiguration _config;
    private readonly ILogger<EntrarModel> _log;

    public EntrarModel(SignInManager<UsuarioIdentity> signIn, UserManager<UsuarioIdentity> usuarios, IAsertaDb db, ContextoEjecucion contexto,
        IRegistroAuditoria auditoria, IConfiguration config, ILogger<EntrarModel> log)
    {
        _signIn = signIn;
        _usuarios = usuarios;
        _db = db;
        _contexto = contexto;
        _auditoria = auditoria;
        _config = config;
        _log = log;
    }

    public sealed class Formulario
    {
        [Required(ErrorMessage = "Indique su correo."), EmailAddress(ErrorMessage = "Correo no válido.")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Indique su contraseña.")]
        public string Contrasena { get; set; } = string.Empty;
        public bool Recordar { get; set; }
    }

    [BindProperty] public Formulario Datos { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? Volver { get; set; }

    public bool MostrarCredencialesDemo => _config.GetValue<bool>("Demo:Sembrar");
    public string ContrasenaDemo => _config["Demo:Contrasena"] ?? "Aserta-Demo-2026!";

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToPage("/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var identidad = await _usuarios.FindByEmailAsync(Datos.Email.Trim());
        if (identidad is null)
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return Page();
        }

        // Comprobar el estado de negocio del usuario antes de emitir la cookie (en ambito de mantenimiento: aun no hay tenant).
        Usuario? usuario;
        using (_contexto.AbrirAmbitoMantenimiento())
            usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == identidad.Id);

        if (usuario is null || usuario.Estado != EstadoUsuario.Activo)
        {
            ModelState.AddModelError(string.Empty, "Este usuario no está activo. Contacte con su gestoría.");
            return Page();
        }

        var resultado = await _signIn.PasswordSignInAsync(identidad, Datos.Contrasena, Datos.Recordar, lockoutOnFailure: true);
        if (resultado.Succeeded)
        {
            using (_contexto.AbrirAmbitoTenant(usuario.GestoriaId))
                await _auditoria.RegistrarYGuardarAsync(AccionesAuditoria.InicioSesion, nameof(Usuario), usuario.Id.ToString(), null, usuario.GestoriaId, usuario.Id);
            _log.LogInformation("Inicio de sesión de {Email}", identidad.Email);
            return LocalRedirect(string.IsNullOrEmpty(Volver) || !Url.IsLocalUrl(Volver) ? "/" : Volver);
        }
        if (resultado.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Demasiados intentos fallidos. La cuenta queda bloqueada unos minutos.");
            return Page();
        }
        if (resultado.RequiresTwoFactor)
        {
            ModelState.AddModelError(string.Empty, "Este usuario tiene activado el segundo factor, que se habilita en una tarea posterior.");
            return Page();
        }

        using (_contexto.AbrirAmbitoTenant(usuario.GestoriaId))
            await _auditoria.RegistrarYGuardarAsync(AccionesAuditoria.InicioSesionFallido, nameof(Usuario), usuario.Id.ToString(), null, usuario.GestoriaId, usuario.Id);
        ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
        return Page();
    }
}
