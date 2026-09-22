using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Documental;

namespace Aserta.Dominio.Tests;

public class DocumentalTests
{
    private static readonly Guid Gestoria = Guid.NewGuid();

    private static Cliente ClienteSl(bool empleados = true, bool alquila = true)
    {
        var c = new Cliente { Id = Guid.NewGuid(), GestoriaId = Gestoria, FormaJuridica = FormaJuridica.SL, FechaAlta = new DateOnly(2024, 1, 1), AsesorResponsableId = Guid.NewGuid() };
        c.RegistrarPerfil(new PerfilFiscal { VigenteDesde = c.FechaAlta, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral, TieneEmpleados = empleados, AlquilaLocal = alquila });
        return c;
    }

    private static List<ReglaRequisito> Reglas() =>
    [
        new() { Id = 1, Clave = "FACTURAS-RECIBIDAS", TipoDocumento = TipoDocumento.FacturaRecibida, Descripcion = "Facturas de compras", Periodicidad = Periodicidad.Mensual, CantidadPorMes = null, Obligatorio = true,
                Condiciones = [new ReglaRequisitoCondicion { Atributo = "FormaJuridica", Operador = "IN", Valor = "Autonomo,SL,SA,CB" }] },
        new() { Id = 2, Clave = "EXTRACTO", TipoDocumento = TipoDocumento.ExtractoBancario, Descripcion = "Extracto bancario", Periodicidad = Periodicidad.Mensual, CantidadPorMes = 1, Obligatorio = true },
        new() { Id = 3, Clave = "NOMINAS", TipoDocumento = TipoDocumento.Nomina, Descripcion = "Nóminas", Periodicidad = Periodicidad.Mensual, CantidadPorMes = 1, Obligatorio = true,
                Condiciones = [new ReglaRequisitoCondicion { Atributo = "TieneEmpleados", Operador = "=", Valor = "1" }] },
        new() { Id = 4, Clave = "TICKETS", TipoDocumento = TipoDocumento.Ticket, Descripcion = "Tickets", Periodicidad = Periodicidad.Mensual, Obligatorio = false },
    ];

    [Fact]
    public void Motor_de_requisitos_genera_solo_periodos_pasados_o_en_curso_y_segun_perfil()
    {
        var c = ClienteSl(empleados: false);
        var r = new MotorRequisitos(Reglas()).Evaluar(c, 2026, [], new DateOnly(2026, 8, 15));
        Assert.Contains(r.Nuevos, x => x.TipoDocumento == TipoDocumento.FacturaRecibida && x.Periodo == "07");
        Assert.DoesNotContain(r.Nuevos, x => x.Periodo == "08");   // mes en curso: aun no se reclama
        Assert.DoesNotContain(r.Nuevos, x => x.TipoDocumento == TipoDocumento.Nomina);
        Assert.Equal(7, r.Nuevos.Count(x => x.TipoDocumento == TipoDocumento.ExtractoBancario));
        Assert.All(r.Nuevos.Where(x => x.TipoDocumento == TipoDocumento.ExtractoBancario), x => Assert.Equal(1, x.CantidadEsperada));
        Assert.All(r.Nuevos.Where(x => x.TipoDocumento == TipoDocumento.Ticket), x => Assert.False(x.Obligatorio));
    }

    [Fact]
    public void Motor_de_requisitos_es_idempotente_y_respeta_ajustes_manuales()
    {
        var c = ClienteSl();
        var motor = new MotorRequisitos(Reglas());
        var primera = motor.Evaluar(c, 2026, [], new DateOnly(2026, 8, 15));
        var ajustado = primera.Nuevos.First(x => x.TipoDocumento == TipoDocumento.FacturaRecibida && x.Periodo == "07");
        ajustado.CantidadEsperada = 8; ajustado.ReglaOrigenId = null;
        var segunda = motor.Evaluar(c, 2026, primera.Nuevos, new DateOnly(2026, 8, 15));
        Assert.Empty(segunda.Nuevos);
        Assert.Equal(8, ajustado.CantidadEsperada);
    }

    [Fact]
    public void Te_faltan_2_facturas_de_julio()
    {
        var clienteId = Guid.NewGuid();
        var req = new RequisitoPeriodo { ClienteId = clienteId, Ejercicio = 2026, Periodo = "07", TipoDocumento = TipoDocumento.FacturaRecibida, CantidadEsperada = 8, Obligatorio = true, Descripcion = "Facturas" };
        var docs = Enumerable.Range(1, 7).Select(i => new Documento { ClienteId = clienteId, Ejercicio = 2026, Periodo = "07", Tipo = TipoDocumento.FacturaRecibida, Estado = i == 7 ? EstadoDocumento.Rechazado : EstadoDocumento.Validado }).ToList();
        var estados = CompletitudDocumental.Evaluar([req], docs);
        var e = Assert.Single(estados);
        Assert.Equal(6, e.Recibidos);
        Assert.Equal(2, e.Faltan);
        Assert.Equal("Te faltan 2 facturas de compra de julio", e.Frase);
        Assert.False(e.Completo);
        Assert.Equal(2, CompletitudDocumental.TotalFaltantes(estados));
    }

    [Fact]
    public void Frase_en_singular_y_sin_cantidad_fija()
    {
        var req = new RequisitoPeriodo { Ejercicio = 2026, Periodo = "03", TipoDocumento = TipoDocumento.ExtractoBancario, CantidadEsperada = null, Obligatorio = true };
        var e = Assert.Single(CompletitudDocumental.Evaluar([req], []));
        Assert.Equal("Te falta 1 extracto bancario de marzo", e.Frase);
    }

    [Fact]
    public void Periodo_completo_solo_cuando_todos_los_obligatorios_del_trimestre_estan_cubiertos()
    {
        var clienteId = Guid.NewGuid();
        var reqs = new List<RequisitoPeriodo>();
        foreach (var m in new[] { "07", "08", "09" })
        {
            reqs.Add(new RequisitoPeriodo { ClienteId = clienteId, Ejercicio = 2026, Periodo = m, TipoDocumento = TipoDocumento.ExtractoBancario, CantidadEsperada = 1, Obligatorio = true });
            reqs.Add(new RequisitoPeriodo { ClienteId = clienteId, Ejercicio = 2026, Periodo = m, TipoDocumento = TipoDocumento.Ticket, CantidadEsperada = null, Obligatorio = false });
        }
        var docs = new[] { "07", "08" }.Select(m => new Documento { ClienteId = clienteId, Ejercicio = 2026, Periodo = m, Tipo = TipoDocumento.ExtractoBancario, Estado = EstadoDocumento.Validado }).ToList();
        var estados = CompletitudDocumental.Evaluar(reqs, docs);
        Assert.False(CompletitudDocumental.PeriodoCompleto(estados, 2026, "3T"));
        docs.Add(new Documento { ClienteId = clienteId, Ejercicio = 2026, Periodo = "09", Tipo = TipoDocumento.ExtractoBancario, Estado = EstadoDocumento.Recibido });
        estados = CompletitudDocumental.Evaluar(reqs, docs);
        Assert.True(CompletitudDocumental.PeriodoCompleto(estados, 2026, "3T"));
        Assert.False(CompletitudDocumental.PeriodoCompleto(estados, 2026, "4T"));
    }

    [Fact]
    public void Documento_rechazado_exige_motivo_y_hilo_exige_un_ancla()
    {
        var d = new Documento();
        Assert.Throws<Aserta.Dominio.Comun.ExcepcionDominio>(() => d.Rechazar(Guid.NewGuid(), DateTime.UtcNow, " "));
        d.Rechazar(Guid.NewGuid(), DateTime.UtcNow, "Ilegible");
        Assert.Equal(EstadoDocumento.Rechazado, d.Estado);
        Assert.False(d.CuentaParaRequisitos);
        Assert.Throws<Aserta.Dominio.Comun.ExcepcionDominio>(() => Aserta.Dominio.Mensajeria.Hilo.Nuevo(Gestoria, Guid.NewGuid(), null, null, "x", DateTime.UtcNow));
        Assert.Throws<Aserta.Dominio.Comun.ExcepcionDominio>(() => Aserta.Dominio.Mensajeria.Hilo.Nuevo(Gestoria, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x", DateTime.UtcNow));
        var h = Aserta.Dominio.Mensajeria.Hilo.Nuevo(Gestoria, Guid.NewGuid(), Guid.NewGuid(), null, "Asunto", DateTime.UtcNow);
        var m = h.Responder(Guid.NewGuid(), autorEsCliente: true, "Hola", DateTime.UtcNow);
        Assert.NotNull(m.LeidoPorClienteUtc);
        Assert.Null(m.LeidoPorGestoriaUtc);
    }
}
