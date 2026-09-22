using Aserta.Dominio.Catalogo;
using Aserta.Dominio.Clientes;
using Aserta.Dominio.Obligaciones;

namespace Aserta.Dominio.Tests;

/// <summary>
/// El motor es el corazon del producto (riesgo R4). Estos tests reproducen en
/// memoria un catalogo equivalente al de 0004_catalogo_datos_2026_2027.sql.
/// </summary>
public class MotorObligacionesTests
{
    private static readonly Guid GestoriaId = Guid.NewGuid();
    private static readonly Guid AsesorId = Guid.NewGuid();
    private static readonly DateTime Ahora = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    // --- Catalogo de prueba -----------------------------------------------------------
    private static ReglaObligacion Regla(int id, string modelo, Periodicidad periodicidad, int prioridad = 100, params (string Atributo, string Op, string Valor)[] condiciones) => new()
    {
        Id = id, Clave = $"{modelo}-{id}", ModeloCodigo = modelo, Descripcion = $"Regla {modelo}", Periodicidad = periodicidad,
        VigenteDesdeEjercicio = 2026, Prioridad = prioridad, Activa = true,
        Condiciones = condiciones.Select(c => new ReglaCondicion { ReglaId = id, Atributo = c.Atributo, Operador = c.Op, Valor = c.Valor }).ToList()
    };

    private static readonly List<ReglaObligacion> Reglas =
    [
        Regla(1, "303", Periodicidad.Trimestral, 100, ("RegimenIva", "IN", "General,Simplificado,CriterioCaja"), ("PeriodicidadIva", "=", "Trimestral"), ("FormaJuridica", "IN", "Autonomo,SL,SA,CB")),
        Regla(2, "303", Periodicidad.Mensual, 100, ("RegimenIva", "IN", "General,Simplificado,CriterioCaja"), ("PeriodicidadIva", "=", "Mensual"), ("FormaJuridica", "IN", "Autonomo,SL,SA,CB")),
        Regla(3, "130", Periodicidad.Trimestral, 100, ("FormaJuridica", "=", "Autonomo"), ("RegimenIrpf", "IN", "DirectaNormal,DirectaSimplificada")),
        Regla(4, "131", Periodicidad.Trimestral, 100, ("FormaJuridica", "=", "Autonomo"), ("RegimenIrpf", "=", "Objetiva")),
        Regla(5, "111", Periodicidad.Trimestral, 10, ("TieneEmpleados", "=", "1")),
        Regla(6, "111", Periodicidad.Trimestral, 20, ("PagaProfesionalesConRetencion", "=", "1")),
        Regla(7, "115", Periodicidad.Trimestral, 100, ("AlquilaLocal", "=", "1")),
        Regla(8, "202", Periodicidad.PagoFraccionado, 100, ("FormaJuridica", "IN", "SL,SA")),
        Regla(9, "200", Periodicidad.Anual, 100, ("FormaJuridica", "IN", "SL,SA")),
        Regla(10, "100", Periodicidad.Anual, 100, ("FormaJuridica", "IN", "Autonomo,Particular")),
        Regla(11, "347", Periodicidad.Anual, 100, ("SuperaUmbral347", "=", "1"), ("FormaJuridica", "<>", "Particular")),
    ];

    private static IEnumerable<PlazoModelo> Plazos()
    {
        foreach (var ej in new[] { 2026, 2027 })
        foreach (var modelo in new[] { "303", "130", "131", "111", "115", "202", "200", "100", "347" })
        {
            var periodicidad = modelo switch { "202" => Periodicidad.PagoFraccionado, "200" or "100" or "347" => Periodicidad.Anual, _ => Periodicidad.Trimestral };
            foreach (var p in Periodo.DePeriodicidad(periodicidad))
                yield return Plazo(modelo, ej, p);
            if (modelo == "303")
                foreach (var m in Periodo.Meses) yield return Plazo(modelo, ej, m);
        }
    }

    private static PlazoModelo Plazo(string modelo, int ej, string periodo)
    {
        var fin = Periodo.Rango(ej, periodo).Fin.AddDays(20);
        return new PlazoModelo { ModeloCodigo = modelo, Ejercicio = (short)ej, Periodo = periodo, FechaInicioPresentacion = fin.AddDays(-19), FechaLimiteDomiciliacion = fin.AddDays(-5), FechaLimitePresentacion = fin, Fuente = "test", Confirmado = true };
    }

    private static MotorObligaciones Motor() => new(Reglas, Plazos());

    private static Cliente NuevoCliente(FormaJuridica forma, DateOnly alta, PerfilFiscal perfil)
    {
        var c = new Cliente { Id = Guid.NewGuid(), GestoriaId = GestoriaId, Nif = "X", RazonSocial = "Test", FormaJuridica = forma, AsesorResponsableId = AsesorId, FechaAlta = alta };
        perfil.VigenteDesde = alta;
        c.RegistrarPerfil(perfil);
        return c;
    }

    private static PerfilFiscal PerfilAutonomoDirecta() => new() { RegimenIrpf = RegimenIrpf.DirectaSimplificada, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral };

    private static HashSet<string> Claves(IEnumerable<Obligacion> o) => o.Select(x => $"{x.ModeloCodigo}/{x.Periodo}").ToHashSet();

    // --- Tests ------------------------------------------------------------------------------

    [Fact]
    public void Autonomo_en_directa_tiene_303_130_y_renta()
    {
        var c = NuevoCliente(FormaJuridica.Autonomo, new DateOnly(2025, 1, 1), PerfilAutonomoDirecta());
        var r = Motor().Evaluar(c, 2026, [], Ahora);
        Assert.Equal(9, r.Nuevas.Count);
        Assert.Equal(new HashSet<string> { "303/1T", "303/2T", "303/3T", "303/4T", "130/1T", "130/2T", "130/3T", "130/4T", "100/AN" }, Claves(r.Nuevas));
        Assert.All(r.Nuevas, o => Assert.Equal(EstadoObligacion.PendienteDocumentacion, o.Estado));
        Assert.All(r.Nuevas, o => Assert.Equal(AsesorId, o.AsesorId));
        Assert.All(r.Nuevas, o => Assert.Single(o.Historial));
    }

    [Fact]
    public void Autonomo_en_modulos_tiene_131_en_lugar_de_130()
    {
        var c = NuevoCliente(FormaJuridica.Autonomo, new DateOnly(2025, 1, 1), new PerfilFiscal { RegimenIrpf = RegimenIrpf.Objetiva, RegimenIva = RegimenIva.Simplificado, PeriodicidadIva = PeriodicidadIva.Trimestral });
        var claves = Claves(Motor().Evaluar(c, 2026, [], Ahora).Nuevas);
        Assert.Contains("131/1T", claves);
        Assert.DoesNotContain("130/1T", claves);
        Assert.Contains("303/1T", claves);
    }

    [Fact]
    public void Recargo_de_equivalencia_no_presenta_303()
    {
        var c = NuevoCliente(FormaJuridica.Autonomo, new DateOnly(2025, 1, 1), new PerfilFiscal { RegimenIrpf = RegimenIrpf.DirectaSimplificada, RegimenIva = RegimenIva.RecargoEquivalencia, PeriodicidadIva = PeriodicidadIva.Trimestral });
        var claves = Claves(Motor().Evaluar(c, 2026, [], Ahora).Nuevas);
        Assert.DoesNotContain("303/1T", claves);
        Assert.Contains("130/1T", claves);
    }

    [Fact]
    public void Sociedad_con_iva_mensual_tiene_12_periodos_de_303_y_pagos_fraccionados()
    {
        var c = NuevoCliente(FormaJuridica.SA, new DateOnly(2020, 1, 1), new PerfilFiscal { RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Mensual });
        var claves = Claves(Motor().Evaluar(c, 2026, [], Ahora).Nuevas);
        Assert.Equal(12, claves.Count(k => k.StartsWith("303/")));
        Assert.DoesNotContain("303/1T", claves);
        Assert.Contains("202/1P", claves);
        Assert.Contains("202/3P", claves);
        Assert.Contains("200/AN", claves);
        Assert.DoesNotContain("100/AN", claves);
    }

    [Fact]
    public void Particular_solo_tiene_renta()
    {
        var c = NuevoCliente(FormaJuridica.Particular, new DateOnly(2020, 1, 1), new PerfilFiscal { RegimenIva = RegimenIva.NoAplica, PeriodicidadIva = PeriodicidadIva.NoAplica, SuperaUmbral347 = true });
        var claves = Claves(Motor().Evaluar(c, 2026, [], Ahora).Nuevas);
        Assert.Equal(["100/AN"], claves);
    }

    [Fact]
    public void Dos_reglas_para_el_mismo_modelo_no_duplican_y_gana_la_de_menor_prioridad()
    {
        var c = NuevoCliente(FormaJuridica.SL, new DateOnly(2020, 1, 1), new PerfilFiscal { RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral, TieneEmpleados = true, PagaProfesionalesConRetencion = true });
        var r = Motor().Evaluar(c, 2026, [], Ahora);
        var de111 = r.Nuevas.Where(o => o.ModeloCodigo == "111").ToList();
        Assert.Equal(4, de111.Count);
        Assert.All(de111, o => Assert.Equal(5, o.ReglaOrigenId));
    }

    [Fact]
    public void RD10_alta_a_mitad_de_ejercicio_solo_genera_periodos_posteriores()
    {
        var c = NuevoCliente(FormaJuridica.SL, new DateOnly(2026, 7, 1), new PerfilFiscal { RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral });
        var claves = Claves(Motor().Evaluar(c, 2026, [], Ahora).Nuevas);
        Assert.DoesNotContain("303/1T", claves);
        Assert.DoesNotContain("303/2T", claves);
        Assert.Contains("303/3T", claves);
        Assert.Contains("303/4T", claves);
        Assert.DoesNotContain("202/1P", claves);
        Assert.Contains("202/2P", claves); // el periodo abr-sep contiene el alta: se usa el primer perfil que empieza dentro del periodo
        Assert.Contains("200/AN", claves);
    }

    [Fact]
    public void RD10_baja_del_cliente_no_genera_periodos_posteriores()
    {
        var c = NuevoCliente(FormaJuridica.Autonomo, new DateOnly(2020, 1, 1), PerfilAutonomoDirecta());
        c.DarDeBaja(new DateOnly(2026, 5, 15));
        var claves = Claves(Motor().Evaluar(c, 2026, [], Ahora).Nuevas);
        Assert.Contains("303/1T", claves);
        Assert.Contains("303/2T", claves); // estaba de alta parte del 2T
        Assert.DoesNotContain("303/3T", claves);
        Assert.Contains("100/AN", claves);
    }

    [Fact]
    public void Cambio_de_perfil_a_mitad_de_ejercicio_activa_el_111_desde_ese_trimestre()
    {
        var c = NuevoCliente(FormaJuridica.Autonomo, new DateOnly(2020, 1, 1), PerfilAutonomoDirecta());
        var nuevo = PerfilAutonomoDirecta();
        nuevo.VigenteDesde = new DateOnly(2026, 7, 1);
        nuevo.TieneEmpleados = true;
        c.RegistrarPerfil(nuevo);

        Assert.Equal(new DateOnly(2026, 6, 30), c.Perfiles[0].VigenteHasta);
        var claves = Claves(Motor().Evaluar(c, 2026, [], Ahora).Nuevas);
        Assert.DoesNotContain("111/1T", claves);
        Assert.DoesNotContain("111/2T", claves);
        Assert.Contains("111/3T", claves);
        Assert.Contains("111/4T", claves);
    }

    [Fact]
    public void Es_idempotente_reejecutar_no_duplica_ni_cambia_nada()
    {
        var c = NuevoCliente(FormaJuridica.Autonomo, new DateOnly(2020, 1, 1), PerfilAutonomoDirecta());
        var motor = Motor();
        var primera = motor.Evaluar(c, 2026, [], Ahora);
        var segunda = motor.Evaluar(c, 2026, primera.Nuevas, Ahora.AddDays(1));
        Assert.Empty(segunda.Nuevas);
        Assert.Empty(segunda.MarcadasNoAplica);
        Assert.Equal(primera.Nuevas.Count, segunda.SinCambios.Count);
        Assert.False(segunda.HuboCambios);
    }

    [Fact]
    public void D1_cambio_de_perfil_marca_NoAplica_lo_que_sobra_y_nunca_toca_presentadas()
    {
        var c = NuevoCliente(FormaJuridica.Autonomo, new DateOnly(2020, 1, 1), new PerfilFiscal { RegimenIrpf = RegimenIrpf.DirectaSimplificada, RegimenIva = RegimenIva.General, PeriodicidadIva = PeriodicidadIva.Trimestral, AlquilaLocal = true });
        var motor = Motor();
        var existentes = motor.Evaluar(c, 2026, [], Ahora).Nuevas;
        var presentada = existentes.First(o => o.ModeloCodigo == "115" && o.Periodo == "1T");
        presentada.Estado = EstadoObligacion.Presentado;

        // Corrige el perfil: en realidad nunca alquilo local (misma fecha de vigencia => sustitucion)
        var corregido = PerfilAutonomoDirecta();
        corregido.VigenteDesde = c.FechaAlta;
        c.RegistrarPerfil(corregido);
        Assert.Single(c.Perfiles);

        var r = motor.Evaluar(c, 2026, existentes, Ahora.AddDays(1));
        Assert.Empty(r.Nuevas);
        Assert.Equal(3, r.MarcadasNoAplica.Count); // 115 de 2T, 3T y 4T
        Assert.All(r.MarcadasNoAplica, o => Assert.Equal(EstadoObligacion.NoAplica, o.Estado));
        Assert.All(r.MarcadasNoAplica, o => Assert.Equal(TiposEventoHistorial.MarcadaNoAplica, o.Historial[^1].TipoEvento));
        Assert.Equal(EstadoObligacion.Presentado, presentada.Estado); // intocable
    }

    [Fact]
    public void Reactiva_una_NoAplica_que_vuelve_a_proceder()
    {
        var c = NuevoCliente(FormaJuridica.Autonomo, new DateOnly(2020, 1, 1), PerfilAutonomoDirecta());
        var motor = Motor();
        var existentes = motor.Evaluar(c, 2026, [], Ahora).Nuevas;
        var una = existentes.First(o => o.ModeloCodigo == "130" && o.Periodo == "3T");
        una.Estado = EstadoObligacion.NoAplica;

        var r = motor.Evaluar(c, 2026, existentes, Ahora);
        Assert.Single(r.Reactivadas);
        Assert.Equal(EstadoObligacion.PendienteDocumentacion, una.Estado);
        Assert.Equal(TiposEventoHistorial.Reactivada, una.Historial[^1].TipoEvento);
    }

    [Fact]
    public void Actualiza_plazos_si_el_catalogo_cambio_pero_no_en_presentadas()
    {
        var c = NuevoCliente(FormaJuridica.Autonomo, new DateOnly(2020, 1, 1), PerfilAutonomoDirecta());
        var existentes = Motor().Evaluar(c, 2026, [], Ahora).Nuevas;
        var abierta = existentes.First(o => o.ModeloCodigo == "303" && o.Periodo == "1T");
        var presentada = existentes.First(o => o.ModeloCodigo == "303" && o.Periodo == "2T");
        presentada.Estado = EstadoObligacion.Presentado;

        var plazos = Plazos().ToList();
        foreach (var p in plazos.Where(p => p.ModeloCodigo == "303" && p.Ejercicio == 2026 && p.Periodo is "1T" or "2T"))
            p.FechaLimitePresentacion = p.FechaLimitePresentacion.AddDays(2);
        var r = new MotorObligaciones(Reglas, plazos).Evaluar(c, 2026, existentes, Ahora);

        Assert.Single(r.PlazosActualizados);
        Assert.Equal(new DateOnly(2026, 4, 22), abierta.FechaLimitePresentacion);
        Assert.Equal(new DateOnly(2026, 7, 20), presentada.FechaLimitePresentacion);
    }

    [Fact]
    public void Sin_plazo_en_catalogo_no_genera_y_avisa()
    {
        var c = NuevoCliente(FormaJuridica.Autonomo, new DateOnly(2020, 1, 1), PerfilAutonomoDirecta());
        var r = new MotorObligaciones(Reglas, Plazos().Where(p => !(p.ModeloCodigo == "130" && p.Periodo == "4T"))).Evaluar(c, 2026, [], Ahora);
        Assert.DoesNotContain("130/4T", Claves(r.Nuevas));
        Assert.Single(r.Avisos);
        Assert.Contains("130", r.Avisos[0]);
    }

    [Fact]
    public void Territorio_foral_se_rechaza_en_el_dominio()
    {
        var c = new Cliente { Id = Guid.NewGuid(), GestoriaId = GestoriaId, FormaJuridica = FormaJuridica.Autonomo, FechaAlta = new DateOnly(2026, 1, 1) };
        Assert.Throws<Aserta.Dominio.Comun.ExcepcionDominio>(() => c.RegistrarPerfil(new PerfilFiscal { VigenteDesde = c.FechaAlta, Territorio = Territorio.Foral }));
    }

    [Fact]
    public void Reglas_inactivas_o_fuera_de_vigencia_no_se_aplican()
    {
        var reglas = Reglas.Select(r => new ReglaObligacion { Id = r.Id, Clave = r.Clave, ModeloCodigo = r.ModeloCodigo, Descripcion = r.Descripcion, Periodicidad = r.Periodicidad, VigenteDesdeEjercicio = r.VigenteDesdeEjercicio, Prioridad = r.Prioridad, Activa = r.Activa, Condiciones = r.Condiciones }).ToList();
        reglas.First(r => r.ModeloCodigo == "130").Activa = false;
        reglas.First(r => r.ModeloCodigo == "100").VigenteHastaEjercicio = 2025;
        var c = NuevoCliente(FormaJuridica.Autonomo, new DateOnly(2020, 1, 1), PerfilAutonomoDirecta());
        var claves = Claves(new MotorObligaciones(reglas, Plazos()).Evaluar(c, 2026, [], Ahora).Nuevas);
        Assert.DoesNotContain("130/1T", claves);
        Assert.DoesNotContain("100/AN", claves);
        Assert.Contains("303/1T", claves);
    }

    [Fact]
    public void Condicion_con_atributo_desconocido_falla_ruidosamente()
    {
        var regla = Regla(99, "303", Periodicidad.Trimestral, 100, ("AtributoInexistente", "=", "1"));
        var c = NuevoCliente(FormaJuridica.Autonomo, new DateOnly(2020, 1, 1), PerfilAutonomoDirecta());
        Assert.Throws<InvalidOperationException>(() => new MotorObligaciones([regla], Plazos()).Evaluar(c, 2026, [], Ahora));
    }
}
