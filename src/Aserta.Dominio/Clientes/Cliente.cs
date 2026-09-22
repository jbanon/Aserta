using Aserta.Dominio.Comun;

namespace Aserta.Dominio.Clientes;

/// <summary>La empresa, autonomo o particular al que la gestoria presta servicio. No confundir con Usuario.</summary>
public class Cliente
{
    public Guid Id { get; set; }
    public Guid GestoriaId { get; set; }
    public string Nif { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public FormaJuridica FormaJuridica { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? DireccionCalle { get; set; }
    public string? DireccionCodigoPostal { get; set; }
    public string? DireccionMunicipio { get; set; }
    public string? DireccionProvincia { get; set; }
    public Guid AsesorResponsableId { get; set; }
    public DateOnly FechaAlta { get; set; }
    public DateOnly? FechaBaja { get; set; }
    public EstadoCliente Estado { get; set; } = EstadoCliente.Activo;
    public string? Notas { get; set; }

    public List<PerfilFiscal> Perfiles { get; set; } = [];

    public string NombreParaMostrar => string.IsNullOrWhiteSpace(NombreComercial) ? RazonSocial : NombreComercial!;

    /// <summary>RD-10: el cliente estaba de alta en algun dia del intervalo [desde, hasta].</summary>
    public bool EstabaDeAltaEn(DateOnly desde, DateOnly hasta)
    {
        if (FechaAlta > hasta) return false;
        if (FechaBaja.HasValue && FechaBaja.Value < desde) return false;
        return true;
    }

    /// <summary>Perfil fiscal vigente en una fecha concreta, o null si el cliente no tenia perfil entonces.</summary>
    public PerfilFiscal? PerfilVigenteEn(DateOnly fecha) =>
        Perfiles
            .Where(p => p.VigenteDesde <= fecha && (p.VigenteHasta is null || p.VigenteHasta.Value >= fecha))
            .OrderByDescending(p => p.VigenteDesde)
            .FirstOrDefault();

    public PerfilFiscal? PerfilActual => Perfiles.FirstOrDefault(p => p.VigenteHasta is null);

    /// <summary>
    /// Cierra el perfil vigente el dia anterior a <paramref name="nuevo"/>.VigenteDesde y anade la nueva version.
    /// Si el nuevo perfil empieza el mismo dia que el vigente, lo sustituye (correccion, no cambio).
    /// </summary>
    public void RegistrarPerfil(PerfilFiscal nuevo)
    {
        if (nuevo.Territorio == Territorio.Foral)
            throw new ExcepcionDominio("El territorio foral (Pais Vasco y Navarra) queda fuera del alcance de la plataforma: otra hacienda, otro calendario y otro sistema de facturacion (TicketBAI).");

        var vigente = PerfilActual;
        if (vigente is not null)
        {
            if (nuevo.VigenteDesde < vigente.VigenteDesde)
                throw new ExcepcionDominio($"La nueva version del perfil no puede empezar antes que la vigente ({vigente.VigenteDesde:dd/MM/yyyy}).");

            if (nuevo.VigenteDesde == vigente.VigenteDesde)
            {
                Perfiles.Remove(vigente);
            }
            else
            {
                vigente.VigenteHasta = nuevo.VigenteDesde.AddDays(-1);
            }
        }
        nuevo.ClienteId = Id;
        nuevo.GestoriaId = GestoriaId;
        nuevo.VigenteHasta = null;
        Perfiles.Add(nuevo);
    }

    public void DarDeBaja(DateOnly fechaBaja)
    {
        if (fechaBaja < FechaAlta)
            throw new ExcepcionDominio("La fecha de baja no puede ser anterior a la de alta.");
        FechaBaja = fechaBaja;
        Estado = EstadoCliente.Baja;
    }
}
