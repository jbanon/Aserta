using Aserta.Dominio.Clientes;

namespace Aserta.Aplicacion.Clientes;

/// <summary>Datos de la ficha (alta y edicion). La validacion de negocio la hace ServicioClientes.</summary>
public sealed class DatosCliente
{
    public string Nif { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public FormaJuridica FormaJuridica { get; set; } = FormaJuridica.Autonomo;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? DireccionCalle { get; set; }
    public string? DireccionCodigoPostal { get; set; }
    public string? DireccionMunicipio { get; set; }
    public string? DireccionProvincia { get; set; }
    public Guid AsesorResponsableId { get; set; }
    public DateOnly FechaAlta { get; set; }
    public string? Notas { get; set; }
}

/// <summary>Una version del perfil fiscal tal y como la introduce el gestor.</summary>
public sealed class DatosPerfilFiscal
{
    public DateOnly VigenteDesde { get; set; }
    public RegimenIrpf RegimenIrpf { get; set; } = RegimenIrpf.NoAplica;
    public RegimenIva RegimenIva { get; set; } = RegimenIva.General;
    public PeriodicidadIva PeriodicidadIva { get; set; } = PeriodicidadIva.Trimestral;
    public bool TieneEmpleados { get; set; }
    public bool PagaProfesionalesConRetencion { get; set; }
    public bool AlquilaLocal { get; set; }
    public bool RepartePagosCapitalMobiliario { get; set; }
    public bool OperacionesIntracomunitarias { get; set; }
    public bool SuperaUmbral347 { get; set; }
    public Territorio Territorio { get; set; } = Territorio.Comun;
    public byte CierreEjercicioMes { get; set; } = 12;
    public byte CierreEjercicioDia { get; set; } = 31;

    public PerfilFiscal AEntidad() => new()
    {
        Id = Guid.NewGuid(),
        VigenteDesde = VigenteDesde,
        RegimenIrpf = RegimenIrpf,
        RegimenIva = RegimenIva,
        PeriodicidadIva = PeriodicidadIva,
        TieneEmpleados = TieneEmpleados,
        PagaProfesionalesConRetencion = PagaProfesionalesConRetencion,
        AlquilaLocal = AlquilaLocal,
        RepartePagosCapitalMobiliario = RepartePagosCapitalMobiliario,
        OperacionesIntracomunitarias = OperacionesIntracomunitarias,
        SuperaUmbral347 = SuperaUmbral347,
        Territorio = Territorio,
        CierreEjercicioMes = CierreEjercicioMes,
        CierreEjercicioDia = CierreEjercicioDia,
    };

    public static DatosPerfilFiscal Desde(PerfilFiscal p) => new()
    {
        VigenteDesde = p.VigenteDesde,
        RegimenIrpf = p.RegimenIrpf,
        RegimenIva = p.RegimenIva,
        PeriodicidadIva = p.PeriodicidadIva,
        TieneEmpleados = p.TieneEmpleados,
        PagaProfesionalesConRetencion = p.PagaProfesionalesConRetencion,
        AlquilaLocal = p.AlquilaLocal,
        RepartePagosCapitalMobiliario = p.RepartePagosCapitalMobiliario,
        OperacionesIntracomunitarias = p.OperacionesIntracomunitarias,
        SuperaUmbral347 = p.SuperaUmbral347,
        Territorio = p.Territorio,
        CierreEjercicioMes = p.CierreEjercicioMes,
        CierreEjercicioDia = p.CierreEjercicioDia,
    };
}
