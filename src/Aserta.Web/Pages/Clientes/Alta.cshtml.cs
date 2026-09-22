using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Nucleo;
using Aserta.Aplicacion.Obligaciones;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Aserta.Web.Infraestructura;
using Microsoft.AspNetCore.Mvc;

namespace Aserta.Web.Pages.Clientes;

public class AltaModel : PaginaBase
{
    private readonly ServicioClientes _clientes;
    private readonly ServicioUsuarios _usuarios;
    private readonly ServicioGeneracionObligaciones _motor;
    private readonly IRelojSistema _reloj;

    public AltaModel(ServicioClientes clientes, ServicioUsuarios usuarios, ServicioGeneracionObligaciones motor, IRelojSistema reloj)
    {
        _clientes = clientes;
        _usuarios = usuarios;
        _motor = motor;
        _reloj = reloj;
    }

    [BindProperty] public DatosCliente Datos { get; set; } = new();
    [BindProperty] public DatosPerfilFiscal Perfil { get; set; } = new();
    public List<Usuario> Asesores { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Asesores = await _usuarios.AsesoresActivosAsync();
        Datos.FechaAlta = _reloj.Hoy;
        Perfil.VigenteDesde = _reloj.Hoy;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Asesores = await _usuarios.AsesoresActivosAsync();
        if (Perfil.VigenteDesde == default) Perfil.VigenteDesde = Datos.FechaAlta;

        Guid id = Guid.Empty;
        var ok = await IntentarAsync(async () => id = await _clientes.AltaAsync(Datos, Perfil), prefijoParaPerfil: "Perfil", prefijoParaDatos: "Datos");
        if (!ok) return Page();

        var resultados = await _motor.GenerarParaClienteAsync(id);
        int nuevas = resultados.Sum(r => r.Nuevas.Count);
        var avisos = resultados.SelectMany(r => r.Avisos).ToList();
        AvisoOk = $"Cliente dado de alta. El motor ha generado {nuevas} obligaciones para {string.Join(" y ", resultados.Select(r => r.Ejercicio))}.";
        if (avisos.Count > 0) AvisoInfo = string.Join(" ", avisos.Take(3));
        return RedirectToPage("/Clientes/Ficha", new { id, pestana = "obligaciones" });
    }

    // Los errores de ServicioClientes vienen con nombres de campo sin prefijo; aqui se reparten entre los dos modelos enlazados.
    private async Task<bool> IntentarAsync(Func<Task> accion, string prefijoParaPerfil, string prefijoParaDatos)
    {
        var camposPerfil = new HashSet<string>(typeof(DatosPerfilFiscal).GetProperties().Select(p => p.Name));
        try
        {
            await accion();
            return true;
        }
        catch (Aserta.Aplicacion.Comun.ExcepcionValidacion ex)
        {
            foreach (var (campo, mensajes) in ex.Errores)
                foreach (var m in mensajes)
                    ModelState.AddModelError($"{(camposPerfil.Contains(campo) ? prefijoParaPerfil : prefijoParaDatos)}.{campo}", m);
            return false;
        }
        catch (Aserta.Dominio.Comun.ExcepcionDominio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return false;
        }
    }
}
