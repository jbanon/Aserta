using Aserta.Aplicacion.Clientes;
using Aserta.Aplicacion.Documental;
using Aserta.Aplicacion.Obligaciones;
using Aserta.Aplicacion.PortalCliente;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Documental;
using Aserta.Dominio.Nucleo;
using Aserta.Dominio.Obligaciones;
using Aserta.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aserta.Integracion.Tests;

/// <summary>
/// Criterios de exito 3 y 4 a nivel de servicios sobre la base real: subida con
/// hash y almacen cifrado, deteccion de duplicados, validacion que completa el
/// periodo y mueve la obligacion, y aprobacion del borrador por el cliente (RD-09).
/// </summary>
[Collection("aplicacion")]
public class FlujoDocumentalTests
{
    private readonly FabricaAplicacion _app;
    public FlujoDocumentalTests(FabricaAplicacion app) => _app = app;

    private static readonly byte[] Pdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\ntrailer << /Root 1 0 R >>\n%%EOF");

    private async Task<(Guid ClienteId, Guid AsesorId)> PrepararClienteAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AsertaDbContext>();
        var cliente = await db.Clientes.Include(c => c.Perfiles).FirstAsync(c => c.GestoriaId == FabricaAplicacion.TenantA);
        sp.GetRequiredService<ContextoEjecucion>().EstablecerUsuario(cliente.AsesorResponsableId, FabricaAplicacion.TenantA, null, "test", [Roles.SocioDirector], "127.0.0.1", "tests");
        if (cliente.Perfiles.Count == 0)
            await sp.GetRequiredService<ServicioClientes>().NuevaVersionPerfilAsync(cliente.Id, new DatosPerfilFiscal { VigenteDesde = cliente.FechaAlta, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral, TieneEmpleados = true });
        await sp.GetRequiredService<ServicioGeneracionObligaciones>().GenerarParaClienteAsync(cliente.Id, [2026]);
        return (cliente.Id, cliente.AsesorResponsableId);
    }

    [Fact]
    public async Task Subida_guarda_cifrado_calcula_hash_y_rechaza_duplicados()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var (clienteId, _) = await PrepararClienteAsync(scope.ServiceProvider);
        var documentos = scope.ServiceProvider.GetRequiredService<ServicioDocumentos>();
        var contenido = Pdf.Concat(Guid.NewGuid().ToByteArray()).ToArray(); // contenido unico por ejecucion

        var doc = await documentos.SubirAsync(Subida(clienteId, TipoDocumento.FacturaRecibida, "01", contenido));
        Assert.Equal(EstadoDocumento.Recibido, doc.Estado);
        Assert.Equal(64, doc.HashSha256.Length);
        Assert.NotNull(doc.DatosExtraidos); // extractor simulado para facturas

        var (stream, _) = await documentos.AbrirAsync(doc.Id);
        using var ms = new MemoryStream(); await stream.CopyToAsync(ms);
        Assert.Equal(contenido, ms.ToArray()); // se descifra al abrir

        var ex = await Assert.ThrowsAsync<Aserta.Aplicacion.Comun.ExcepcionValidacion>(() => documentos.SubirAsync(Subida(clienteId, TipoDocumento.Ticket, "02", contenido)));
        Assert.Contains("ya se subió", ex.Message);
    }

    [Fact]
    public async Task Validar_los_requisitos_de_un_trimestre_mueve_la_obligacion_a_DocumentacionCompleta()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var sp = scope.ServiceProvider;
        var (clienteId, _) = await PrepararClienteAsync(sp);
        var db = sp.GetRequiredService<AsertaDbContext>();
        var documentos = sp.GetRequiredService<ServicioDocumentos>();
        var requisitos = sp.GetRequiredService<ServicioRequisitos>();

        // Ejercicio y trimestre de prueba lejano para no chocar con otras ejecuciones: usamos el 1T y limpiamos su estado
        var o = await db.Obligaciones.FirstAsync(x => x.ClienteId == clienteId && x.ModeloCodigo == "303" && x.Ejercicio == 2026 && x.Periodo == "1T");
        if (o.Estado != EstadoObligacion.PendienteDocumentacion) { o.Estado = EstadoObligacion.PendienteDocumentacion; await db.SaveChangesAsync(); }

        // Todos los requisitos obligatorios de enero-marzo con un documento validado cada uno
        var estados = await requisitos.EstadoAsync(clienteId, 2026);
        foreach (var e in estados.Where(e => e.Requisito.Obligatorio && e.Requisito.Periodo is "01" or "02" or "03" && !e.Completo))
        {
            for (int i = e.Recibidos; i < e.Esperados; i++)
            {
                var d = await documentos.SubirAsync(Subida(clienteId, e.Requisito.TipoDocumento, e.Requisito.Periodo, Pdf.Concat(Guid.NewGuid().ToByteArray()).ToArray()));
                await documentos.ValidarAsync(d.Id);
            }
        }
        // Un documento mas (aunque el periodo ya estuviera cubierto de ejecuciones anteriores) para disparar el evento DocumentoValidado
        var extra = await documentos.SubirAsync(Subida(clienteId, TipoDocumento.Ticket, "03", Pdf.Concat(Guid.NewGuid().ToByteArray()).ToArray()));
        await documentos.ValidarAsync(extra.Id);

        var final = await db.Obligaciones.AsNoTracking().FirstAsync(x => x.Id == o.Id);
        Assert.Equal(EstadoObligacion.DocumentacionCompleta, final.Estado);
        Assert.True(await db.DocumentosObligacion.AnyAsync(x => x.ObligacionId == o.Id)); // vinculo automatico por periodo
    }

    [Fact]
    public async Task El_cliente_aprueba_el_borrador_y_la_gestoria_ve_la_conformidad()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var sp = scope.ServiceProvider;
        var (clienteId, asesorId) = await PrepararClienteAsync(sp);
        var db = sp.GetRequiredService<AsertaDbContext>();

        var o = await db.Obligaciones.Include(x => x.Historial).FirstAsync(x => x.ClienteId == clienteId && x.ModeloCodigo == "303" && x.Ejercicio == 2026 && x.Periodo == "2T");
        o.Estado = EstadoObligacion.PendienteAprobacionCliente; o.FechaAprobacionClienteUtc = null; o.UsuarioAprobacionId = null; o.ImporteResultado = 500m; o.SignoResultado = SignoResultado.Ingresar;
        await db.SaveChangesAsync();

        // Un usuario del lado cliente (ClienteAdmin)
        var um = sp.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Infraestructura.Identidad.UsuarioIdentity>>();
        var email = $"cliente-{clienteId.ToString()[..8]}@pruebas.aserta.local";
        var identidad = await um.FindByEmailAsync(email);
        if (identidad is null)
        {
            identidad = new Infraestructura.Identidad.UsuarioIdentity { Id = Guid.NewGuid(), UserName = email, Email = email, EmailConfirmed = true };
            Assert.True((await um.CreateAsync(identidad, FabricaAplicacion.ContrasenaTecnico)).Succeeded);
            await um.AddToRoleAsync(identidad, Roles.ClienteAdmin);
            db.Usuarios.Add(new Usuario { Id = identidad.Id, GestoriaId = FabricaAplicacion.TenantA, ClienteId = clienteId, NombreCompleto = "Cliente de pruebas" });
            await db.SaveChangesAsync();
        }
        sp.GetRequiredService<ContextoEjecucion>().EstablecerUsuario(identidad.Id, FabricaAplicacion.TenantA, clienteId, "Cliente de pruebas", [Roles.ClienteAdmin], "127.0.0.1", "tests");

        var portal = sp.GetRequiredService<ServicioPortal>();
        var resumen = await portal.ResumenAsync();
        Assert.Contains(resumen.PendientesAprobacion, x => x.Id == o.Id);
        await portal.AprobarBorradorAsync(o.Id);

        var final = await db.Obligaciones.AsNoTracking().Include(x => x.Historial).FirstAsync(x => x.Id == o.Id);
        Assert.NotNull(final.FechaAprobacionClienteUtc);
        Assert.Equal(identidad.Id, final.UsuarioAprobacionId);
        Assert.Contains(final.Historial, h => h.TipoEvento == TiposEventoHistorial.AprobacionCliente);
        Assert.True(await db.Avisos.AnyAsync(a => a.UsuarioDestinoId == asesorId && a.Tipo == TiposAviso.AprobacionCliente && a.EntidadId == o.Id.ToString()));
        // Segunda aprobacion => error de dominio
        await Assert.ThrowsAsync<Aserta.Dominio.Comun.ExcepcionDominio>(() => portal.AprobarBorradorAsync(o.Id));
    }

    [Fact]
    public async Task Un_usuario_de_cliente_no_puede_ver_documentos_de_otro_cliente()
    {
        await _app.AsegurarTenantsDePruebaAsync();
        using var scope = _app.AmbitoComo(FabricaAplicacion.TenantA);
        var sp = scope.ServiceProvider;
        var (clienteId, _) = await PrepararClienteAsync(sp);
        var documentos = sp.GetRequiredService<ServicioDocumentos>();
        var doc = await documentos.SubirAsync(Subida(clienteId, TipoDocumento.Otro, "05", Pdf.Concat(Guid.NewGuid().ToByteArray()).ToArray()));

        sp.GetRequiredService<ContextoEjecucion>().EstablecerUsuario(Guid.NewGuid(), FabricaAplicacion.TenantA, Guid.NewGuid(), "otro cliente", [Roles.ClienteUsuario], "127.0.0.1", "tests");
        await Assert.ThrowsAsync<Aserta.Aplicacion.Comun.ExcepcionNoAutorizado>(() => documentos.ObtenerAsync(doc.Id));
    }

    private static SubidaDocumento Subida(Guid clienteId, TipoDocumento tipo, string periodo, byte[] contenido) => new()
    {
        ClienteId = clienteId, Tipo = tipo, Ejercicio = 2026, Periodo = periodo, NombreOriginal = $"prueba-{Guid.NewGuid():N}.pdf", TipoMime = "application/pdf", TamanoBytes = contenido.Length, Contenido = new MemoryStream(contenido)
    };
}
