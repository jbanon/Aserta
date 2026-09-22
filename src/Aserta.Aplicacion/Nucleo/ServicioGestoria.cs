using Aserta.Aplicacion.Comun;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Nucleo;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Aplicacion.Nucleo;

/// <summary>Configuracion del tenant: RD-07 y RD-09 son interruptores de la gestoria.</summary>
public sealed class ServicioGestoria
{
    private readonly IAsertaDb _db;

    public ServicioGestoria(IAsertaDb db) => _db = db;

    public async Task<Gestoria> ActualAsync(CancellationToken ct = default) =>
        await _db.Gestorias.FirstOrDefaultAsync(ct) ?? throw new ExcepcionNoEncontrado("Gestoría", "(contexto)");

    public async Task ConfigurarAsync(bool exigeAprobacionCliente, bool asesorVeTodosLosClientes, string nombre, CancellationToken ct = default)
    {
        var g = await ActualAsync(ct);
        if (string.IsNullOrWhiteSpace(nombre)) throw new ExcepcionValidacion("Nombre", "El nombre es obligatorio.");
        g.Nombre = nombre.Trim();
        g.ExigeAprobacionCliente = exigeAprobacionCliente;
        g.AsesorVeTodosLosClientes = asesorVeTodosLosClientes;
        await _db.GuardarCambiosAsync(ct);
    }
}
