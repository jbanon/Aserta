using Aserta.Aplicacion.Nucleo;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;

namespace Aserta.Web.Pages.Usuarios;

public class IndexModel : PaginaBase
{
    private readonly ServicioUsuarios _usuarios;
    private readonly IGestorIdentidad _identidad;

    public IndexModel(ServicioUsuarios usuarios, IGestorIdentidad identidad)
    {
        _usuarios = usuarios;
        _identidad = identidad;
    }

    public sealed record Fila(Usuario Usuario, string Email, IReadOnlyList<string> Roles);
    public List<Fila> Filas { get; private set; } = [];

    public async Task OnGetAsync()
    {
        foreach (var u in await _usuarios.ListarDeGestoriaAsync())
            Filas.Add(new Fila(u, await _identidad.EmailDeAsync(u.Id) ?? "", await _identidad.RolesDeAsync(u.Id)));
    }

    public async Task<IActionResult> OnPostEstadoAsync(Guid usuarioId, EstadoUsuario estado)
    {
        var ok = await IntentarAsync(() => _usuarios.CambiarEstadoAsync(usuarioId, estado));
        if (ok) AvisoOk = estado == EstadoUsuario.Activo ? "Usuario reactivado." : "Usuario bloqueado.";
        else await OnGetAsync();
        return ok ? RedirectToPage() : Page();
    }
}
