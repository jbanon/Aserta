using Aserta.Aplicacion.Comun;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Comun;
using Aserta.Dominio.Nucleo;
using Microsoft.EntityFrameworkCore;

namespace Aserta.Aplicacion.Clientes;

/// <summary>Casos de uso de la ficha de cliente (M1). Todo lo que guarda pasa por aqui.</summary>
public sealed class ServicioClientes
{
    private readonly IAsertaDb _db;
    private readonly IContextoUsuarioActual _usuario;
    private readonly IRelojSistema _reloj;

    public ServicioClientes(IAsertaDb db, IContextoUsuarioActual usuario, IRelojSistema reloj)
    {
        _db = db;
        _usuario = usuario;
        _reloj = reloj;
    }

    public async Task<Guid> AltaAsync(DatosCliente datos, DatosPerfilFiscal perfil, CancellationToken ct = default)
    {
        var gestoriaId = _usuario.GestoriaId ?? throw new ExcepcionNoAutorizado("No hay gestoría en el contexto.");
        await ValidarAsync(datos, clienteId: null, ct);
        ValidarPerfil(perfil, datos.FechaAlta);

        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            GestoriaId = gestoriaId,
            FechaAlta = datos.FechaAlta,
            Estado = EstadoCliente.Activo,
        };
        Aplicar(cliente, datos);

        var entidadPerfil = perfil.AEntidad();
        cliente.RegistrarPerfil(entidadPerfil); // lanza ExcepcionDominio si Territorio = Foral (DA-14)

        _db.Clientes.Add(cliente);
        await _db.GuardarCambiosAsync(ct);
        return cliente.Id;
    }

    public async Task ActualizarDatosAsync(Guid clienteId, DatosCliente datos, CancellationToken ct = default)
    {
        var cliente = await ObtenerAsync(clienteId, ct);
        await ValidarAsync(datos, clienteId, ct);
        Aplicar(cliente, datos);
        cliente.FechaAlta = datos.FechaAlta;
        await _db.GuardarCambiosAsync(ct);
    }

    /// <summary>Registra una nueva version del perfil fiscal (cierra la anterior el dia antes). El motor se reejecuta aparte.</summary>
    public async Task NuevaVersionPerfilAsync(Guid clienteId, DatosPerfilFiscal perfil, CancellationToken ct = default)
    {
        var cliente = await ObtenerAsync(clienteId, ct);
        ValidarPerfil(perfil, cliente.FechaAlta);
        cliente.RegistrarPerfil(perfil.AEntidad());
        await _db.GuardarCambiosAsync(ct);
    }

    public async Task DarDeBajaAsync(Guid clienteId, DateOnly fechaBaja, CancellationToken ct = default)
    {
        var cliente = await ObtenerAsync(clienteId, ct);
        cliente.DarDeBaja(fechaBaja);
        await _db.GuardarCambiosAsync(ct);
    }

    public async Task ReactivarAsync(Guid clienteId, CancellationToken ct = default)
    {
        var cliente = await ObtenerAsync(clienteId, ct);
        cliente.FechaBaja = null;
        cliente.Estado = EstadoCliente.Activo;
        await _db.GuardarCambiosAsync(ct);
    }

    public async Task<Cliente> ObtenerAsync(Guid clienteId, CancellationToken ct = default)
    {
        var cliente = await _db.Clientes.Include(c => c.Perfiles).FirstOrDefaultAsync(c => c.Id == clienteId, ct)
                      ?? throw new ExcepcionNoEncontrado("Cliente", clienteId);
        ComprobarVisibilidad(cliente);
        return cliente;
    }

    /// <summary>RD-07: si la gestoria restringe, un asesor solo ve sus clientes. Socio y administrativo ven todos.</summary>
    public async Task<IQueryable<Cliente>> ConsultaVisiblesAsync(CancellationToken ct = default)
    {
        IQueryable<Cliente> q = _db.Clientes.AsNoTracking();
        if (await DebeRestringirAMisClientesAsync(ct))
            q = q.Where(c => c.AsesorResponsableId == _usuario.UsuarioId);
        return q;
    }

    private async Task<bool> DebeRestringirAMisClientesAsync(CancellationToken ct)
    {
        if (!_usuario.TieneRol(Roles.Asesor) || _usuario.TieneRol(Roles.SocioDirector)) return false;
        var gestoria = await _db.Gestorias.AsNoTracking().FirstOrDefaultAsync(ct);
        return gestoria is { AsesorVeTodosLosClientes: false };
    }

    private void ComprobarVisibilidad(Cliente cliente)
    {
        // La visibilidad restringida se evalua en ConsultaVisiblesAsync; aqui solo la barrera por cliente del lado cliente.
        if (_usuario.ClienteId is Guid propio && propio != cliente.Id)
            throw new ExcepcionNoAutorizado("Este cliente no es el suyo.");
    }

    private static void Aplicar(Cliente cliente, DatosCliente datos)
    {
        cliente.Nif = ValidadorNif.Normalizar(datos.Nif);
        cliente.RazonSocial = datos.RazonSocial.Trim();
        cliente.NombreComercial = Limpiar(datos.NombreComercial);
        cliente.FormaJuridica = datos.FormaJuridica;
        cliente.Email = Limpiar(datos.Email);
        cliente.Telefono = Limpiar(datos.Telefono);
        cliente.DireccionCalle = Limpiar(datos.DireccionCalle);
        cliente.DireccionCodigoPostal = Limpiar(datos.DireccionCodigoPostal);
        cliente.DireccionMunicipio = Limpiar(datos.DireccionMunicipio);
        cliente.DireccionProvincia = Limpiar(datos.DireccionProvincia);
        cliente.AsesorResponsableId = datos.AsesorResponsableId;
        cliente.Notas = Limpiar(datos.Notas);
    }

    private static string? Limpiar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private async Task ValidarAsync(DatosCliente datos, Guid? clienteId, CancellationToken ct)
    {
        var errores = new Dictionary<string, List<string>>();
        void Error(string campo, string msg) { if (!errores.TryGetValue(campo, out var l)) errores[campo] = l = []; l.Add(msg); }

        var nif = ValidadorNif.Validar(datos.Nif);
        if (!nif.EsValido) Error(nameof(datos.Nif), nif.Error!);
        else
        {
            // Coherencia forma juridica / tipo de identificador
            bool esCif = nif.Tipo == ValidadorNif.TipoIdentificador.Cif;
            bool esPersonaJuridica = datos.FormaJuridica is FormaJuridica.SL or FormaJuridica.SA or FormaJuridica.CB;
            if (esPersonaJuridica && !esCif) Error(nameof(datos.Nif), "Una sociedad o comunidad de bienes se identifica con un CIF (letra + 7 dígitos + control).");
            if (!esPersonaJuridica && esCif) Error(nameof(datos.Nif), "Un autónomo o particular se identifica con un NIF o NIE de persona física, no con un CIF.");

            bool duplicado = await _db.Clientes.AnyAsync(c => c.Nif == nif.Normalizado && c.Id != clienteId, ct);
            if (duplicado) Error(nameof(datos.Nif), "Ya existe un cliente con este NIF en la gestoría.");
        }

        if (string.IsNullOrWhiteSpace(datos.RazonSocial)) Error(nameof(datos.RazonSocial), "La razón social es obligatoria.");
        if (datos.RazonSocial?.Length > 200) Error(nameof(datos.RazonSocial), "Máximo 200 caracteres.");
        if (datos.FechaAlta == default) Error(nameof(datos.FechaAlta), "La fecha de alta es obligatoria.");
        if (!string.IsNullOrWhiteSpace(datos.Email) && !datos.Email.Contains('@')) Error(nameof(datos.Email), "El correo no es válido.");

        var asesorValido = await _db.Usuarios.AnyAsync(u => u.Id == datos.AsesorResponsableId && u.ClienteId == null && u.Estado == EstadoUsuario.Activo, ct);
        if (!asesorValido) Error(nameof(datos.AsesorResponsableId), "Hay que asignar un asesor responsable activo de la gestoría.");

        if (errores.Count > 0)
            throw new ExcepcionValidacion(errores.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }

    private static void ValidarPerfil(DatosPerfilFiscal perfil, DateOnly fechaAltaCliente)
    {
        var errores = new Dictionary<string, string[]>();
        if (perfil.Territorio == Territorio.Foral)
            errores["Territorio"] = ["El territorio foral (País Vasco y Navarra) queda fuera del alcance: otra hacienda, otro calendario y otro sistema de facturación (TicketBAI)."];
        if (perfil.VigenteDesde == default)
            errores["VigenteDesde"] = ["Indique desde qué fecha está vigente este perfil."];
        else if (perfil.VigenteDesde < fechaAltaCliente)
            errores["VigenteDesde"] = [$"El perfil no puede estar vigente antes del alta del cliente ({fechaAltaCliente:dd/MM/yyyy})."];
        if (perfil.RegimenIva != RegimenIva.NoAplica && perfil.RegimenIva != RegimenIva.Exento && perfil.PeriodicidadIva == PeriodicidadIva.NoAplica)
            errores["PeriodicidadIva"] = ["Si el cliente está en un régimen de IVA hay que indicar la periodicidad."];
        if (errores.Count > 0) throw new ExcepcionValidacion(errores);
    }
}
