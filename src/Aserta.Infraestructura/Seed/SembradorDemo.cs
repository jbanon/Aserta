using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Obligaciones;
using Aserta.Aplicacion.Puertos;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Comun;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Aserta.Infraestructura.Identidad;
using Aserta.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aserta.Infraestructura.Seed;

/// <summary>
/// Datos de demostracion (SOLO entornos no productivos). Idempotente: si la
/// gestoria demo ya existe no se vuelve a crear; el motor de obligaciones si se
/// reejecuta siempre (es reentrante) para que el calendario este al dia.
/// Todos los datos son FICTICIOS: NIF/CIF sintacticamente validos pero inventados.
/// Se escribe en C# y no en SQL porque las contrasenas de Identity se hashean
/// con UserManager y el alta de clientes pasa por los mismos servicios que la UI.
/// </summary>
public sealed class SembradorDemo
{
    public static readonly Guid GestoriaDemoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string DominioCorreo = "demo.aserta.local";

    private static readonly (Guid Id, string Email, string Nombre, string Rol)[] UsuariosGestoria =
    [
        (Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), $"socio@{DominioCorreo}",   "Marta Montalbán Ríos",   Roles.SocioDirector),
        (Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"), $"carlos@{DominioCorreo}",  "Carlos Ruiz Peña",        Roles.Asesor),
        (Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"), $"lucia@{DominioCorreo}",   "Lucía Fernández Soto",    Roles.Asesor),
        (Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004"), $"pedro@{DominioCorreo}",   "Pedro Salas Ibáñez",      Roles.Administrativo),
    ];

    private readonly ContextoEjecucion _contexto;
    private readonly AsertaDbContext _db;
    private readonly RoleManager<RolIdentity> _roles;
    private readonly UserManager<UsuarioIdentity> _usuarios;
    private readonly ServicioClientes _clientes;
    private readonly ServicioGeneracionObligaciones _motor;
    private readonly IConfiguration _config;
    private readonly IRelojSistema _reloj;
    private readonly ILogger<SembradorDemo> _log;

    public SembradorDemo(ContextoEjecucion contexto, AsertaDbContext db, RoleManager<RolIdentity> roles, UserManager<UsuarioIdentity> usuarios,
        ServicioClientes clientes, ServicioGeneracionObligaciones motor, IConfiguration config, IRelojSistema reloj, ILogger<SembradorDemo> log)
    {
        _reloj = reloj;
        _contexto = contexto;
        _db = db;
        _roles = roles;
        _usuarios = usuarios;
        _clientes = clientes;
        _motor = motor;
        _config = config;
        _log = log;
    }

    public string ContrasenaDemo => _config["Demo:Contrasena"] ?? "Aserta-Demo-2026!";

    public async Task SembrarAsync(CancellationToken ct = default)
    {
        await AsegurarRolesAsync();

        using (_contexto.AbrirAmbitoMantenimiento())
        {
            if (!await _db.Gestorias.AnyAsync(g => g.Id == GestoriaDemoId, ct))
            {
                _db.Gestorias.Add(new Gestoria
                {
                    Id = GestoriaDemoId,
                    Nombre = "Asesoría Montalbán & Ruiz",
                    Nif = ValidadorNif.ConstruirCif('B', 8123456),
                    Estado = EstadoGestoria.Activa,
                    FechaAlta = new DateOnly(2026, 1, 1),
                });
                await _db.SaveChangesAsync(ct);
                _log.LogInformation("Seed: gestoría demo creada");
            }
        }

        // A partir de aqui actuamos COMO la gestoria demo, con el socio como usuario (para que la auditoria tenga autor).
        using var ambito = _contexto.AbrirAmbitoTenant(GestoriaDemoId);
        _contexto.EstablecerUsuario(UsuariosGestoria[0].Id, GestoriaDemoId, null, UsuariosGestoria[0].Nombre, [Roles.SocioDirector], "127.0.0.1", "SembradorDemo");

        foreach (var (id, email, nombre, rol) in UsuariosGestoria)
            await AsegurarUsuarioAsync(id, email, nombre, rol, null, ct);

        var carlos = UsuariosGestoria[1].Id;
        var lucia = UsuariosGestoria[2].Id;

        // Clientes ficticios con perfiles variados para que el motor luzca.
        var panaderia = await AsegurarClienteAsync(
            new DatosCliente { Nif = ValidadorNif.ConstruirCif('B', 4567890), RazonSocial = "Panadería López SL", NombreComercial = "Panadería López", FormaJuridica = FormaJuridica.SL,
                Email = "panaderia@ejemplo.invalid", Telefono = "600 000 001", DireccionCalle = "Calle del Horno, 12", DireccionCodigoPostal = "28001", DireccionMunicipio = "Madrid", DireccionProvincia = "Madrid",
                AsesorResponsableId = carlos, FechaAlta = new DateOnly(2024, 3, 1), Notas = "Obrador con dos empleados. Local alquilado." },
            new DatosPerfilFiscal { VigenteDesde = new DateOnly(2024, 3, 1), RegimenIrpf = RegimenIrpf.NoAplica, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral,
                TieneEmpleados = true, AlquilaLocal = true, SuperaUmbral347 = true }, ct);

        var fontanero = await AsegurarClienteAsync(
            new DatosCliente { Nif = ValidadorNif.ConstruirNif(23456789), RazonSocial = "Javier Ortega Campos", NombreComercial = "Fontanería Ortega", FormaJuridica = FormaJuridica.Autonomo,
                Email = "jortega@ejemplo.invalid", Telefono = "600 000 002", DireccionMunicipio = "Alcalá de Henares", DireccionProvincia = "Madrid",
                AsesorResponsableId = carlos, FechaAlta = new DateOnly(2023, 1, 1), Notas = "Contrata a su primer empleado el 1 de julio de 2026 (cambio de perfil a mitad de ejercicio)." },
            new DatosPerfilFiscal { VigenteDesde = new DateOnly(2023, 1, 1), RegimenIrpf = RegimenIrpf.DirectaSimplificada, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral }, ct);

        await AsegurarClienteAsync(
            new DatosCliente { Nif = ValidadorNif.ConstruirCif('E', 7654321), RazonSocial = "Bar La Esquina CB", FormaJuridica = FormaJuridica.CB,
                Email = "laesquina@ejemplo.invalid", DireccionMunicipio = "Getafe", DireccionProvincia = "Madrid",
                AsesorResponsableId = lucia, FechaAlta = new DateOnly(2022, 6, 1) },
            new DatosPerfilFiscal { VigenteDesde = new DateOnly(2022, 6, 1), RegimenIrpf = RegimenIrpf.NoAplica, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral,
                TieneEmpleados = true, AlquilaLocal = true }, ct);

        await AsegurarClienteAsync(
            new DatosCliente { Nif = ValidadorNif.ConstruirNif(34567890), RazonSocial = "Ana Belén Castro Vidal", NombreComercial = "Peluquería Ana Belén", FormaJuridica = FormaJuridica.Autonomo,
                DireccionMunicipio = "Leganés", DireccionProvincia = "Madrid",
                AsesorResponsableId = lucia, FechaAlta = new DateOnly(2021, 1, 1), Notas = "Estimación objetiva (módulos)." },
            new DatosPerfilFiscal { VigenteDesde = new DateOnly(2021, 1, 1), RegimenIrpf = RegimenIrpf.Objetiva, RegimenIva = RegimenIva.Simplificado, PeriodicidadIva = PeriodicidadIva.Trimestral }, ct);

        await AsegurarClienteAsync(
            new DatosCliente { Nif = ValidadorNif.ConstruirCif('A', 2345678), RazonSocial = "Transportes Vega SA", FormaJuridica = FormaJuridica.SA,
                Email = "administracion@ejemplo.invalid", DireccionMunicipio = "Coslada", DireccionProvincia = "Madrid",
                AsesorResponsableId = carlos, FechaAlta = new DateOnly(2020, 1, 1), Notas = "Inscrita en REDEME: IVA mensual. Operaciones intracomunitarias." },
            new DatosPerfilFiscal { VigenteDesde = new DateOnly(2020, 1, 1), RegimenIrpf = RegimenIrpf.NoAplica, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Mensual,
                TieneEmpleados = true, PagaProfesionalesConRetencion = true, OperacionesIntracomunitarias = true, SuperaUmbral347 = true }, ct);

        await AsegurarClienteAsync(
            new DatosCliente { Nif = ValidadorNif.ConstruirCif('B', 9876543), RazonSocial = "Consultoría Delta SL", FormaJuridica = FormaJuridica.SL,
                Email = "delta@ejemplo.invalid", DireccionMunicipio = "Madrid", DireccionProvincia = "Madrid",
                AsesorResponsableId = lucia, FechaAlta = new DateOnly(2025, 1, 1), Notas = "Paga a profesionales con retención y reparte dividendos." },
            new DatosPerfilFiscal { VigenteDesde = new DateOnly(2025, 1, 1), RegimenIrpf = RegimenIrpf.NoAplica, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral,
                PagaProfesionalesConRetencion = true, RepartePagosCapitalMobiliario = true, OperacionesIntracomunitarias = true }, ct);

        await AsegurarClienteAsync(
            new DatosCliente { Nif = ValidadorNif.ConstruirNif(45678901), RazonSocial = "Pilar Domínguez Lara", NombreComercial = "Mercería Pilar", FormaJuridica = FormaJuridica.Autonomo,
                DireccionMunicipio = "Móstoles", DireccionProvincia = "Madrid",
                AsesorResponsableId = carlos, FechaAlta = new DateOnly(2019, 1, 1), Notas = "Comercio minorista en recargo de equivalencia: no presenta 303." },
            new DatosPerfilFiscal { VigenteDesde = new DateOnly(2019, 1, 1), RegimenIrpf = RegimenIrpf.DirectaSimplificada, RegimenIva = RegimenIva.RecargoEquivalencia, PeriodicidadIva = PeriodicidadIva.Trimestral }, ct);

        await AsegurarClienteAsync(
            new DatosCliente { Nif = ValidadorNif.ConstruirNif(56789012), RazonSocial = "Rosa Martín Esteban", FormaJuridica = FormaJuridica.Particular,
                DireccionMunicipio = "Madrid", DireccionProvincia = "Madrid",
                AsesorResponsableId = lucia, FechaAlta = new DateOnly(2024, 1, 1), Notas = "Particular: solo Renta." },
            new DatosPerfilFiscal { VigenteDesde = new DateOnly(2024, 1, 1), RegimenIrpf = RegimenIrpf.NoAplica, RegimenIva = RegimenIva.NoAplica, PeriodicidadIva = PeriodicidadIva.NoAplica }, ct);

        await AsegurarClienteAsync(
            new DatosCliente { Nif = ValidadorNif.ConstruirCif('B', 1122334), RazonSocial = "Estudio de Arquitectura Norte SL", FormaJuridica = FormaJuridica.SL,
                DireccionMunicipio = "Tres Cantos", DireccionProvincia = "Madrid",
                AsesorResponsableId = carlos, FechaAlta = new DateOnly(2026, 7, 1), Notas = "Alta a mitad de ejercicio: solo obligaciones desde el 3T de 2026 (RD-10)." },
            new DatosPerfilFiscal { VigenteDesde = new DateOnly(2026, 7, 1), RegimenIrpf = RegimenIrpf.NoAplica, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral,
                PagaProfesionalesConRetencion = true }, ct);

        // Cambio de perfil a mitad de ejercicio (riesgo D1): el fontanero contrata en julio de 2026
        var clienteFontanero = await _clientes.ObtenerAsync(fontanero, ct);
        if (clienteFontanero.Perfiles.Count == 1)
        {
            await _clientes.NuevaVersionPerfilAsync(fontanero, new DatosPerfilFiscal
            {
                VigenteDesde = new DateOnly(2026, 7, 1), RegimenIrpf = RegimenIrpf.DirectaSimplificada, RegimenIva = RegimenIva.General,
                PeriodicidadIva = PeriodicidadIva.Trimestral, TieneEmpleados = true
            }, ct);
        }

        // Usuario del lado cliente (portal, fase 3) para la panaderia
        await AsegurarUsuarioAsync(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000101"), $"panaderia@{DominioCorreo}", "Luis López Herrero", Roles.ClienteAdmin, panaderia, ct);

        // El motor es reentrante: se ejecuta siempre para 2026 y 2027
        var resultados = await _motor.GenerarParaTodosAsync([2026, 2027], ct);
        _log.LogInformation("Seed: motor de obligaciones ejecutado para {N} clientes; {Nuevas} obligaciones nuevas",
            resultados.Select(r => r.ClienteId).Distinct().Count(), resultados.Sum(r => r.Nuevas.Count));

        await SimularHistoricoAsync(ct);
    }

    /// <summary>
    /// Los datos de demo nacen "hoy": sin esto, todo lo anterior a la fecha actual
    /// apareceria vencido. Se recorre la maquina de estados con fechas anteriores
    /// al plazo para las obligaciones cuyo limite paso hace mas de 10 dias, dejando
    /// dos vencidas a proposito (el semaforo rojo tambien hay que ensenarlo).
    /// Solo la primera vez (si no hay ninguna cerrada todavia).
    /// </summary>
    private async Task SimularHistoricoAsync(CancellationToken ct)
    {
        const string marcador = "SeedHistoricoDemo";
        if (await _db.EjecucionesProgramadas.AnyAsync(e => e.Tarea == marcador, ct)) return;
        _db.EjecucionesProgramadas.Add(new EjecucionProgramada { Tarea = marcador, UltimaEjecucionUtc = _reloj.AhoraUtc, Estado = "Correcto" });

        var hoy = _reloj.Hoy;
        var pasadas = await _db.Obligaciones
            .Where(o => o.Estado == EstadoObligacion.PendienteDocumentacion)
            .ToListAsync(ct);
        pasadas = pasadas.Where(o => Semaforo.FechaDeReferencia(o) < hoy.AddDays(-10)).OrderBy(Semaforo.FechaDeReferencia).ToList();
        if (pasadas.Count == 0) { await _db.SaveChangesAsync(ct); return; }

        var asesores = await _db.Usuarios.Where(u => u.ClienteId == null).Select(u => u.Id).ToListAsync(ct);
        var clienteAdmin = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000101");
        int i = 0, cerradas = 0;
        foreach (var o in pasadas)
        {
            i++;
            if (i % 23 == 0) continue; // unas pocas se quedan vencidas a proposito

            var limite = Semaforo.FechaDeReferencia(o);
            var t = limite.AddDays(-18).ToDateTime(new TimeOnly(9, 30), DateTimeKind.Utc);
            var asesor = o.AsesorId ?? asesores[i % asesores.Count];
            var pasos = new[] { EstadoObligacion.DocumentacionCompleta, EstadoObligacion.EnPreparacion, EstadoObligacion.RevisionInterna, EstadoObligacion.PendienteAprobacionCliente };
            foreach (var paso in pasos)
            {
                t = t.AddDays(2).AddHours(i % 5);
                o.CambiarEstado(paso, gestoriaExigeAprobacionCliente: true, asesor, t, paso == EstadoObligacion.DocumentacionCompleta ? "Documentación recibida por el portal." : null);
            }
            t = t.AddDays(1);
            o.RegistrarAprobacionCliente(clienteAdmin, t);
            t = t.AddHours(6);
            o.CambiarEstado(EstadoObligacion.Presentado, true, asesor, t, "Presentado en la sede de la AEAT (justificante manual).");
            // Un tercio se queda en Presentado (sin justificante archivado), el resto se cierra
            if (i % 3 != 0)
            {
                t = t.AddDays(1);
                o.CambiarEstado(EstadoObligacion.Cerrado, true, asesor, t, "Justificante archivado.");
                cerradas++;
            }
            if (o.FechaLimiteDomiciliacion is not null)
            {
                o.ImporteResultado = Math.Round(150m + (i * 137 % 2400), 2);
                o.SignoResultado = i % 7 == 0 ? SignoResultado.Devolver : SignoResultado.Ingresar;
            }
        }
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Seed: histórico simulado en {N} obligaciones pasadas ({C} cerradas)", pasadas.Count, cerradas);
    }

    private async Task AsegurarRolesAsync()
    {
        foreach (var rol in Roles.Todos)
            if (!await _roles.RoleExistsAsync(rol))
                await _roles.CreateAsync(new RolIdentity(rol));
    }

    private async Task AsegurarUsuarioAsync(Guid id, string email, string nombre, string rol, Guid? clienteId, CancellationToken ct)
    {
        if (await _usuarios.FindByIdAsync(id.ToString()) is not null) return;

        var identidad = new UsuarioIdentity { Id = id, UserName = email, Email = email, EmailConfirmed = true, LockoutEnabled = true };
        var r = await _usuarios.CreateAsync(identidad, ContrasenaDemo);
        if (!r.Succeeded) throw new InvalidOperationException($"Seed: no se pudo crear {email}: {string.Join("; ", r.Errors.Select(e => e.Description))}");
        await _usuarios.AddToRoleAsync(identidad, rol);

        _db.Usuarios.Add(new Usuario { Id = id, GestoriaId = GestoriaDemoId, ClienteId = clienteId, NombreCompleto = nombre, Estado = EstadoUsuario.Activo });
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Seed: usuario {Email} ({Rol})", email, rol);
    }

    private async Task<Guid> AsegurarClienteAsync(DatosCliente datos, DatosPerfilFiscal perfil, CancellationToken ct)
    {
        var nif = ValidadorNif.Normalizar(datos.Nif);
        var existente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Nif == nif, ct);
        if (existente is not null) return existente.Id;
        var id = await _clientes.AltaAsync(datos, perfil, ct);
        _log.LogInformation("Seed: cliente {Nombre}", datos.RazonSocial);
        return id;
    }
}
