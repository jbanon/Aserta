using Microsoft.AspNetCore.Identity;

namespace Aserta.Infraestructura.Identidad;

/// <summary>Fila de AspNetUsers. Los datos de negocio (tenant, cliente, nombre) viven en dbo.Usuario.</summary>
public sealed class UsuarioIdentity : IdentityUser<Guid> { }

public sealed class RolIdentity : IdentityRole<Guid>
{
    public RolIdentity() { }
    public RolIdentity(string nombre) : base(nombre) { }
}
