namespace Aserta.Dominio.Nucleo;

public enum EstadoUsuario { Activo, Bloqueado, Baja }

/// <summary>
/// Extension de la identidad (AspNetUsers) con el tenant y, si es del lado
/// cliente, el cliente al que pertenece. Id = AspNetUsers.Id.
/// </summary>
public class Usuario
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public Guid? ClienteId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;
    public bool MfaObligatorio { get; set; }

    public bool EsDelLadoCliente => ClienteId.HasValue;
}
