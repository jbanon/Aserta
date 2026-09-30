using Aserta.Dominio.Comun;
using Microsoft.EntityFrameworkCore;
using Sga.Nucleo.Calculo;
using Sga.Nucleo.Calendario;
using Sga.Nucleo.Modelo;

namespace Sga.Web.Datos;

/// <summary>
/// Datos de demostracion: 3 gestores y 14 clientes ficticios (3 arrendadores, 6 profesionales, 5 sociedades)
/// con una linea de tiempo creible de 2026: 1T y 2T presentados, 3T en campana con pendientes,
/// un hueco de numeracion, un fraccionamiento solicitado, un extracto sin conciliar, documentos que faltan.
/// Ningun dato es real: nombres, NIF, IBAN e importes son inventados; solo se conservan las mecanicas.
/// </summary>
public static class Sembrador
{
    private static readonly Random Azar = new(2026);
    private static DateTime Utc(int y, int m, int d, int h = 10, int min = 0) => new(y, m, Math.Min(d, DateTime.DaysInMonth(y, m)), h, min, 0, DateTimeKind.Utc);
    private static DateOnly F(int m, int d) => new(2026, m, Math.Min(d, DateTime.DaysInMonth(2026, m)));

    /// <summary>IBAN espanol ficticio con digitos de control validos (para que se vea real en pantalla).</summary>
    private static string Iban(string entidad, string oficina, string cuenta)
    {
        var dc = DigitoControlCcc(entidad, oficina, cuenta);
        var bban = entidad + oficina + dc + cuenta;
        var numerico = bban + "142800"; // E=14, S=28, 00
        int resto = 0;
        foreach (var ch in numerico) resto = (resto * 10 + (ch - '0')) % 97;
        int control = 98 - resto;
        return $"ES{control:00}{bban}";
    }
    private static string DigitoControlCcc(string entidad, string oficina, string cuenta)
    {
        int[] pesos = [1, 2, 4, 8, 5, 10, 9, 7, 3, 6];
        int D(string s)
        {
            int suma = 0; var digitos = s.PadLeft(10, '0');
            for (int i = 0; i < 10; i++) suma += (digitos[i] - '0') * pesos[i];
            int d = 11 - suma % 11; return d == 11 ? 0 : d == 10 ? 1 : d;
        }
        return $"{D(entidad + oficina)}{D(cuenta)}";
    }

    public static async Task SembrarSiVaciaAsync(SgaDb db, IReloj reloj, ILogger log)
    {
        if (await db.Gestores.AnyAsync()) return;
        log.LogInformation("Sembrando datos de demostración de SGA…");

        // --- Gestores (ficticios) ---------------------------------------------------------------
        var marta = new Gestor { Nombre = "Marta Serrano Gil", Iniciales = "MS", Rol = "Socia directora", Email = "marta@sgacontabilizado.example", Nif = ValidadorNif.ConstruirNif(31415926), Color = "morado" };
        var lucia = new Gestor { Nombre = "Lucía Ferrer Campos", Iniciales = "LF", Rol = "Gestora fiscal", Email = "lucia@sgacontabilizado.example", Nif = ValidadorNif.ConstruirNif(27182818), Color = "verde" };
        var raul = new Gestor { Nombre = "Raúl Ibáñez Mora", Iniciales = "RI", Rol = "Contabilidad y laboral", Email = "raul@sgacontabilizado.example", Nif = ValidadorNif.ConstruirNif(16180339), Color = "ambar" };
        db.Gestores.AddRange(marta, lucia, raul);
        await db.SaveChangesAsync();

        // --- Clientes -----------------------------------------------------------------------------
        Cliente Arr(string nombre, string corto, int nif, string dir, string cp, string loc, Gestor g, string iban) => new()
        {
            Tipo = TipoCliente.Arrendador, Nombre = nombre, NombreCorto = corto, Nif = ValidadorNif.ConstruirNif(nif), Email = corto.Split(' ')[0].ToLowerInvariant() + "@correo.example", Telefono = "6" + Azar.Next(10000000, 99999999),
            Direccion = dir, CodigoPostal = cp, Localidad = loc, Iban = iban, Emisor = QuienEmite.Sga, FormaPago = FormaPago.Domiciliacion, GestorId = g.Id, FechaAlta = new DateOnly(2019 + Azar.Next(0, 5), Azar.Next(1, 12), 1),
            PeriodicidadHonorarios = PeriodicidadHonorarios.Trimestral, Honorarios = 45m, Actividades = [new Actividad { Iae = "861.2", Descripcion = "Alquiler de locales industriales y otros alquileres", Principal = true }],
        };
        Cliente Pro(string nombre, string corto, int nif, string dir, string cp, string loc, Gestor g, string iban, QuienEmite emisor, bool p130, bool empleados, bool alquila, decimal honorarios, params Actividad[] acts) => new()
        {
            Tipo = TipoCliente.Profesional, Nombre = nombre, NombreCorto = corto, Nif = ValidadorNif.ConstruirNif(nif), Email = corto.Split(' ')[0].ToLowerInvariant() + "@correo.example", Telefono = "6" + Azar.Next(10000000, 99999999),
            Direccion = dir, CodigoPostal = cp, Localidad = loc, Iban = iban, Emisor = emisor, FormaPago = FormaPago.Domiciliacion, TieneEmpleados = empleados, Presenta130 = p130, AlquilaLocal = alquila, GestorId = g.Id,
            FechaAlta = new DateOnly(2020 + Azar.Next(0, 5), Azar.Next(1, 12), 1), PeriodicidadHonorarios = PeriodicidadHonorarios.Mensual, Honorarios = honorarios, Actividades = [.. acts],
        };
        Cliente Soc(string nombre, string corto, int cif, string dir, string cp, string loc, Gestor g, string iban, bool intracom, bool empleados, bool alquila, bool claveBanco, decimal honorarios, string contacto, params Actividad[] acts) => new()
        {
            Tipo = TipoCliente.Sociedad, Nombre = nombre, NombreCorto = corto, Nif = ValidadorNif.ConstruirCif('B', cif), Email = "administracion@" + corto.Split(' ')[0].ToLowerInvariant() + ".example", Telefono = "91" + Azar.Next(1000000, 9999999),
            Direccion = dir, CodigoPostal = cp, Localidad = loc, Iban = iban, Emisor = QuienEmite.Cliente, FormaPago = FormaPago.Domiciliacion, TieneEmpleados = empleados, OperacionesIntracomunitarias = intracom, AlquilaLocal = alquila,
            ClaveConsultaBanco = claveBanco, UltimaDescargaBancoUtc = claveBanco ? Utc(2026, 10, 3, 7, 5) : null, GestorId = g.Id, FechaAlta = new DateOnly(2017 + Azar.Next(0, 7), Azar.Next(1, 12), 1),
            PeriodicidadHonorarios = PeriodicidadHonorarios.Mensual, Honorarios = honorarios, NombreContacto = contacto, Actividades = [.. acts],
        };

        var ernesto = Arr("Ernesto Valdés Pardo", "Ernesto Valdés", 4021873, "Calle de la Espada, 9, 3.º A", "28012", "Madrid", lucia, Iban("2100", "0418", "0200051332"));
        ernesto.Contratos.Add(new ContratoAlquiler { Inmueble = "local comercial, calle del Fresno 12", InquilinoNombre = "Talleres Mendieta SL", InquilinoNif = ValidadorNif.ConstruirCif('B', 8811234), InquilinoEmail = "admin@talleresmendieta.example", InquilinoDireccion = "Calle del Fresno, 12, 28901 Getafe", RentaMensual = 1850m });
        var carmen = Arr("Carmen Ruiz Alonso", "Carmen Ruiz", 5117642, "Avenida de la Albufera, 88, 5.º B", "28038", "Madrid", marta, Iban("0049", "1500", "0512345678"));
        carmen.Contratos.Add(new ContratoAlquiler { Inmueble = "local, avenida de la Albufera 88, bajo", InquilinoNombre = "Peluquería Nova SL", InquilinoNif = ValidadorNif.ConstruirCif('B', 8422101), InquilinoEmail = "nova@peluquerianova.example", InquilinoDireccion = "Avenida de la Albufera, 88, 28038 Madrid", RentaMensual = 1200m });
        carmen.Contratos.Add(new ContratoAlquiler { Inmueble = "nave 7, polígono Las Mercedes", InquilinoNombre = "Reparaciones Ortiz y Bravo SL", InquilinoNif = ValidadorNif.ConstruirCif('B', 8756320), InquilinoEmail = "taller@ortizybravo.example", InquilinoDireccion = "Polígono Las Mercedes, nave 7, 28022 Madrid", RentaMensual = 950m });
        var joaquin = Arr("Joaquín Peña Lasa", "Joaquín Peña", 2934871, "Calle de Alcalá, 300, 4.º C", "28027", "Madrid", raul, Iban("0182", "2370", "0201234567"));
        joaquin.Contratos.Add(new ContratoAlquiler { Inmueble = "oficina, calle de Alcalá 300, 2.º", InquilinoNombre = "Estudio Rivera Arquitectos SLP", InquilinoNif = ValidadorNif.ConstruirCif('B', 8399012), InquilinoEmail = "estudio@riveraarq.example", InquilinoDireccion = "Calle de Alcalá, 300, 2.º, 28027 Madrid", RentaMensual = 720m });

        var nuria = Pro("Nuria Campos Ibarra", "Nuria Campos", 7132908, "Calle de Embajadores, 44, 2.º D", "28012", "Madrid", lucia, Iban("1465", "0100", "0417123456"), QuienEmite.Sga, true, false, false, 60m,
            new Actividad { Iae = "774", Descripcion = "Traductores e intérpretes", Principal = true }, new Actividad { Iae = "899", Descripcion = "Otros servicios profesionales (corrección y maquetación)", Principal = false });
        var alvaro = Pro("Álvaro Sanz Morales", "Álvaro Sanz", 4876521, "Calle de Bravo Murillo, 210, 1.º", "28020", "Madrid", raul, Iban("0081", "0200", "0001234567"), QuienEmite.Sga, false, false, false, 55m,
            new Actividad { Iae = "973.1", Descripcion = "Servicios fotográficos", Principal = true });
        var pilar = Pro("Pilar Domínguez Roca", "Pilar Domínguez", 9021547, "Calle de Ferraz, 27, 3.º izq.", "28008", "Madrid", marta, Iban("2038", "1000", "0011223344"), QuienEmite.Sga, true, true, true, 95m,
            new Actividad { Iae = "411", Descripcion = "Arquitectos", Principal = true });
        var marcos = Pro("Marcos Leal Cruz", "Marcos Leal", 6345987, "Calle de Argumosa, 15, bajo", "28012", "Madrid", lucia, Iban("0073", "0100", "0500123456"), QuienEmite.Cliente, true, false, false, 50m,
            new Actividad { Iae = "751", Descripcion = "Profesionales de la publicidad, relaciones públicas y similares (diseño gráfico)", Principal = true });
        var beatriz = Pro("Beatriz Núñez Vela", "Beatriz Núñez", 8823411, "Calle de Serrano, 110, 6.º", "28006", "Madrid", marta, Iban("0128", "0700", "0100234567"), QuienEmite.Cliente, true, false, false, 70m,
            new Actividad { Iae = "731", Descripcion = "Abogados", Principal = true });
        var tomas = Pro("Tomás Iglesias Bravo", "Tomás Iglesias", 3398712, "Calle de Marcelo Usera, 61, 1.º", "28026", "Madrid", raul, Iban("2100", "5731", "0200987654"), QuienEmite.Cliente, true, false, false, 55m,
            new Actividad { Iae = "504.1", Descripcion = "Instalaciones eléctricas en general", Principal = true });

        var luzNorte = Soc("Luz Norte Producciones SL", "Luz Norte", 8612345, "Calle de Julián Camarillo, 26, nave 3", "28037", "Madrid", lucia, Iban("0049", "2020", "0400123456"), false, true, false, true, 210m, "Andrea Solís (administración)",
            new Actividad { Iae = "961.1", Descripcion = "Producción de películas cinematográficas y audiovisuales", Principal = true });
        var panaderia = Soc("Panadería Los Robles SL", "Panadería Los Robles", 8723456, "Calle de los Robles, 4", "28914", "Leganés", raul, Iban("2100", "1234", "0200112233"), false, true, false, false, 150m, "Jorge Robles",
            new Actividad { Iae = "419.1", Descripcion = "Industrias del pan y de la bollería", Principal = true }, new Actividad { Iae = "672.1", Descripcion = "Cafetería (degustación)", Principal = false });
        var ferreteria = Soc("Ferretería Aguilar SL", "Ferretería Aguilar", 8834567, "Calle de Toledo, 95", "28005", "Madrid", marta, Iban("0182", "0600", "0201122334"), true, true, false, false, 165m, "Inés Aguilar",
            new Actividad { Iae = "653.3", Descripcion = "Comercio al por menor de ferretería", Principal = true });
        var nexo = Soc("Nexo Consultores SL", "Nexo Consultores", 8945678, "Paseo de la Castellana, 141, 8.ª", "28046", "Madrid", lucia, Iban("0081", "5200", "0001998877"), false, true, true, false, 180m, "Diego Marín",
            new Actividad { Iae = "843", Descripcion = "Servicios técnicos y de consultoría", Principal = true });
        var vega = Soc("Distribuciones Vega y Soto SL", "Vega y Soto", 8056789, "Polígono Cobo Calleja, calle Lisboa, 12", "28947", "Fuenlabrada", raul, Iban("2038", "2200", "0022334455"), true, true, false, false, 190m, "Sara Vega",
            new Actividad { Iae = "612.9", Descripcion = "Comercio al por mayor de otros productos", Principal = true });

        var clientes = new[] { ernesto, carmen, joaquin, nuria, alvaro, pilar, marcos, beatriz, tomas, luzNorte, panaderia, ferreteria, nexo, vega };
        db.Clientes.AddRange(clientes);
        await db.SaveChangesAsync();

        // --- Facturas emitidas --------------------------------------------------------------------
        var emitidas = new List<FacturaEmitida>();
        FacturaEmitida Alq(Cliente c, ContratoAlquiler k, int mes, EstadoFacturaEmitida estado)
        {
            var i = CalculoFactura.Alquiler(k.RentaMensual, k.TipoIva, k.TipoRetencion);
            var fecha = F(mes, 1);
            return new FacturaEmitida { ClienteId = c.Id, ContratoId = k.Id, Numero = CalculoFactura.NumeroAlquiler(fecha, c.Contratos.IndexOf(k) + 1), Fecha = fecha, DestinatarioNombre = k.InquilinoNombre, DestinatarioNif = k.InquilinoNif,
                Concepto = $"Cuota alquiler {k.Inmueble} · {Infraestructura.Formato.NombreMes(mes)} 2026", Base = i.Base, TipoIva = i.TipoIva, CuotaIva = i.CuotaIva, TipoRetencion = i.TipoRetencion, CuotaRetencion = i.CuotaRetencion, Total = i.Total,
                Origen = OrigenFactura.Sga, Estado = estado, EnviadaUtc = estado >= EstadoFacturaEmitida.EnviadaInquilino ? Utc(2026, mes, 2, 9, 15) : null, RegistradaMonitor = true };
        }
        foreach (var k in ernesto.Contratos) for (int m = 1; m <= 9; m++) emitidas.Add(Alq(ernesto, k, m, m <= 6 ? EstadoFacturaEmitida.CopiaRecibida : EstadoFacturaEmitida.EnviadaInquilino));
        foreach (var k in carmen.Contratos) for (int m = 1; m <= 6; m++) emitidas.Add(Alq(carmen, k, m, EstadoFacturaEmitida.CopiaRecibida)); // 3T sin generar: lo hara el gestor en la demo
        foreach (var k in joaquin.Contratos) for (int m = 1; m <= 9; m++) emitidas.Add(Alq(joaquin, k, m, m <= 8 ? EstadoFacturaEmitida.CopiaRecibida : EstadoFacturaEmitida.Generada)); // la de septiembre, generada y sin enviar

        FacturaEmitida Prof(Cliente c, string numero, DateOnly fecha, string dest, string nif, string concepto, decimal baseImp, decimal iva, decimal ret, OrigenFactura origen)
        {
            var i = CalculoFactura.Calcular(baseImp, iva, ret);
            return new FacturaEmitida { ClienteId = c.Id, Numero = numero, Fecha = fecha, DestinatarioNombre = dest, DestinatarioNif = nif, Concepto = concepto, Base = i.Base, TipoIva = iva, CuotaIva = i.CuotaIva, TipoRetencion = ret, CuotaRetencion = i.CuotaRetencion, Total = i.Total,
                Origen = origen, Estado = origen == OrigenFactura.Sga ? (fecha.Month <= 8 ? EstadoFacturaEmitida.CopiaRecibida : EstadoFacturaEmitida.EnviadaInquilino) : EstadoFacturaEmitida.CopiaRecibida, EnviadaUtc = Utc(2026, fecha.Month, Math.Min(fecha.Day + 1, 28)), RegistradaMonitor = origen == OrigenFactura.Sga };
        }
        // Nuria (SGA emite): traducciones a editoriales y agencias, retencion del 15 % [VERIFICAR tipo profesional]
        string[] clientesNuria = ["Editorial Almendro SL", "Agencia Trazo SL", "Congresos Ibéricos SA"]; string[] nifsNuria = [ValidadorNif.ConstruirCif('B', 8100001), ValidadorNif.ConstruirCif('B', 8100002), ValidadorNif.ConstruirCif('A', 8100003)];
        int n = 0;
        for (int m = 1; m <= 9; m++) for (int k = 0; k < 2 + (m % 2); k++) { n++; int ci = (m + k) % 3; emitidas.Add(Prof(nuria, $"26/{n:000}", F(m, 5 + k * 9), clientesNuria[ci], nifsNuria[ci], ci == 0 ? "Traducción y corrección de textos" : ci == 1 ? "Traducción de campaña y adaptación" : "Interpretación de conferencia", 380m + Azar.Next(0, 9) * 120m, 21m, 15m, OrigenFactura.Sga)); }
        n = 0;
        for (int m = 1; m <= 9; m++) for (int k = 0; k < 1 + (m % 3); k++) { n++; emitidas.Add(Prof(alvaro, $"26/{n:000}", F(m, 8 + k * 7), k == 0 ? "Restaurante La Higuera SL" : "Inmobiliaria Cardenal SL", k == 0 ? ValidadorNif.ConstruirCif('B', 8200001) : ValidadorNif.ConstruirCif('B', 8200002), k == 0 ? "Sesión fotográfica de carta y local" : "Reportaje fotográfico de inmuebles", 250m + Azar.Next(0, 7) * 100m, 21m, 15m, OrigenFactura.Sga)); }
        n = 0;
        for (int m = 1; m <= 9; m++) for (int k = 0; k < 1 + (m % 2); k++) { n++; emitidas.Add(Prof(pilar, $"26/{n:000}", F(m, 10 + k * 12), k == 0 ? "Promociones Aldea Verde SL" : "Comunidad de propietarios Ferraz 31", k == 0 ? ValidadorNif.ConstruirCif('B', 8300001) : ValidadorNif.ConstruirCif('H', 8300002), k == 0 ? "Proyecto básico y de ejecución, fase " + m : "Certificado de eficiencia energética", 1500m + Azar.Next(0, 10) * 270m, 21m, k == 0 ? 15m : 0m, OrigenFactura.Sga)); }
        // Marcos (emite el mismo): hueco (falta la 2026-017) y una duplicada (2026-021 dos veces)
        string[] numsMarcos = ["2026-011", "2026-012", "2026-013", "2026-014", "2026-015", "2026-016", "2026-018", "2026-019", "2026-020", "2026-021", "2026-021", "2026-022"];
        int mesM = 4; int diaM = 3;
        for (int i = 0; i < numsMarcos.Length; i++) { emitidas.Add(Prof(marcos, numsMarcos[i], F(mesM, diaM), i % 2 == 0 ? "Bodega Altos del Jarama SL" : "Clínica Dental Sonrisa SLP", i % 2 == 0 ? ValidadorNif.ConstruirCif('B', 8400001) : ValidadorNif.ConstruirCif('B', 8400002), i % 2 == 0 ? "Diseño de etiquetas y material promocional" : "Identidad visual y redes sociales", 450m + Azar.Next(0, 8) * 110m, 21m, 15m, OrigenFactura.Cliente)); diaM += 11; if (diaM > 27) { diaM -= 25; mesM++; } }
        for (int m = 1; m <= 3; m++) for (int k = 0; k < 3; k++) emitidas.Add(Prof(marcos, $"2026-{(m - 1) * 3 + k + 2:000}", F(m, 4 + k * 9), "Bodega Altos del Jarama SL", ValidadorNif.ConstruirCif('B', 8400001), "Diseño gráfico", 400m + k * 150m, 21m, 15m, OrigenFactura.Cliente));
        n = 0;
        for (int m = 1; m <= 9; m++) for (int k = 0; k < 2; k++) { n++; emitidas.Add(Prof(beatriz, $"B-2026-{n:00}", F(m, 6 + k * 14), k == 0 ? "Grupo Hostelero Mirador SL" : "Rosa Mª Ledesma Prieto", k == 0 ? ValidadorNif.ConstruirCif('B', 8500001) : ValidadorNif.ConstruirNif(5500002), k == 0 ? "Asesoramiento jurídico mensual" : "Redacción de contrato y consulta", k == 0 ? 900m : 350m + Azar.Next(0, 4) * 100m, 21m, k == 0 ? 15m : 0m, OrigenFactura.Cliente)); }
        n = 0;
        for (int m = 1; m <= 9; m++) for (int k = 0; k < 3 + (m % 3); k++) { n++; emitidas.Add(Prof(tomas, $"26/{n:000}", F(m, 2 + k * 6), k % 3 == 0 ? "Comunidad de propietarios Usera 12" : k % 3 == 1 ? "Reformas Delgado SL" : "María Ochoa Pinto", k % 3 == 0 ? ValidadorNif.ConstruirCif('H', 8600001) : k % 3 == 1 ? ValidadorNif.ConstruirCif('B', 8600002) : ValidadorNif.ConstruirNif(5600003), k % 3 == 0 ? "Revisión de instalación eléctrica y boletín" : k % 3 == 1 ? "Instalación eléctrica en obra" : "Reparación y cuadro eléctrico", 180m + Azar.Next(0, 12) * 160m + (m >= 7 ? 900m : 0m), 21m, 0m, OrigenFactura.Cliente)); }
        // Sociedades (emite la empresa; SGA solo las registra)
        n = 0;
        for (int m = 1; m <= 9; m++) for (int k = 0; k < 4 + (m % 2); k++) { n++; bool export = k % 3 == 2; emitidas.Add(Prof(luzNorte, $"LN-2026-{n:00}", F(m, 3 + k * 5), export ? "Cordillera Media LLC" : k % 2 == 0 ? "Cadena Meridiano TV SA" : "Agencia Vértice Comunicación SL", export ? "US-000" : k % 2 == 0 ? ValidadorNif.ConstruirCif('A', 8700001) : ValidadorNif.ConstruirCif('B', 8700002), export ? "Producción de contenidos para exportación (no sujeta)" : "Producción y postproducción de spot", export ? 9000m + Azar.Next(0, 20) * 950m : 2500m + Azar.Next(0, 15) * 620m, export ? 0m : 21m, 0m, OrigenFactura.Cliente)); }
        n = 0;
        for (int m = 1; m <= 9; m++) for (int k = 0; k < 3; k++) { n++; emitidas.Add(Prof(panaderia, $"PR-26-{n:000}", F(m, 10 * (k + 1) - 2), k == 0 ? "Ventas mostrador (resumen quincenal)" : k == 1 ? "Hotel Jardines de Leganés SL" : "Colegio Los Álamos", k == 0 ? "" : k == 1 ? ValidadorNif.ConstruirCif('B', 8800001) : ValidadorNif.ConstruirCif('G', 8800002), k == 0 ? "Ventas de pan y bollería" : "Suministro diario de pan y bollería", 900m + Azar.Next(0, 10) * 210m, 10m, 0m, OrigenFactura.Cliente)); }
        n = 0;
        for (int m = 1; m <= 9; m++) for (int k = 0; k < 4; k++) { n++; emitidas.Add(Prof(ferreteria, $"FA-26-{n:000}", F(m, 2 + k * 7), k == 0 ? "Ventas mostrador (resumen semanal)" : "Construcciones Peláez SL", k == 0 ? "" : ValidadorNif.ConstruirCif('B', 8900001), k == 0 ? "Ventas de ferretería" : "Suministro de material a obra", 1200m + Azar.Next(0, 12) * 260m, 21m, 0m, OrigenFactura.Cliente)); }
        n = 0;
        for (int m = 1; m <= 9; m++) for (int k = 0; k < 2; k++) { n++; emitidas.Add(Prof(nexo, $"NX-2026-{n:00}", F(m, 15 + k * 12), k == 0 ? "Banco Regional del Sur SA" : "Ayuntamiento de Villaverde Alto", k == 0 ? ValidadorNif.ConstruirCif('A', 8910001) : "P2800000A", k == 0 ? "Consultoría de procesos, mensualidad" : "Asistencia técnica proyecto digitalización", k == 0 ? 6500m : 2800m + Azar.Next(0, 5) * 400m, 21m, 0m, OrigenFactura.Cliente)); }
        n = 0;
        for (int m = 1; m <= 9; m++) for (int k = 0; k < 5; k++) { n++; emitidas.Add(Prof(vega, $"VS-26-{n:000}", F(m, 1 + k * 5), k % 2 == 0 ? "Supermercados Encina SL" : "Hostelería Puente de Toledo SL", k % 2 == 0 ? ValidadorNif.ConstruirCif('B', 8920001) : ValidadorNif.ConstruirCif('B', 8920002), "Suministro de productos de limpieza y celulosa", 1800m + Azar.Next(0, 14) * 310m, 21m, 0m, OrigenFactura.Cliente)); }
        db.FacturasEmitidas.AddRange(emitidas);

        // --- Facturas recibidas y gastos ------------------------------------------------------------
        var recibidas = new List<FacturaRecibida>();
        var honorarios = new List<FacturaHonorarios>();
        int numHon = 0;
        FacturaRecibida Rec(Cliente c, DateOnly fecha, string prov, string nif, string numero, string concepto, decimal baseImp, decimal iva, CategoriaGasto cat, bool deducible = true, bool comun = false, int? actividadId = null, bool sga = false) =>
            new() { ClienteId = c.Id, Fecha = fecha, ProveedorNombre = prov, ProveedorNif = nif, Numero = numero, Concepto = concepto, Base = baseImp, TipoIva = iva, CuotaIva = CalculoFactura.Redondear(baseImp * iva / 100m), Total = CalculoFactura.Redondear(baseImp * (1 + iva / 100m)), Categoria = cat, Deducible = deducible, Comun = comun, ActividadId = actividadId, EsHonorariosSga = sga };
        string sgaNif = "B00000000";
        foreach (var c in clientes)
        {
            var meses = c.PeriodicidadHonorarios == PeriodicidadHonorarios.Mensual ? Enumerable.Range(1, 9).ToArray() : [2, 5, 8];
            foreach (var m in meses)
            {
                numHon++;
                var num = $"26/{numHon:000}";
                recibidas.Add(Rec(c, F(m, 20), "SGA Contabilizado SL", sgaNif, num, c.PeriodicidadHonorarios == PeriodicidadHonorarios.Mensual ? "Asesoramiento fiscal y contable, " + Infraestructura.Formato.NombreMes(m) : "Asesoramiento fiscal, trimestre", c.Honorarios, 21m, CategoriaGasto.ServiciosExteriores, sga: true));
                honorarios.Add(new FacturaHonorarios { ClienteId = c.Id, Numero = num, Fecha = F(m, 20), Concepto = c.PeriodicidadHonorarios == PeriodicidadHonorarios.Mensual ? "Honorarios de " + Infraestructura.Formato.NombreMes(m) : $"Honorarios del {(m - 1) / 3 + 1}.º trimestre", Base = c.Honorarios, CuotaIva = CalculoFactura.Redondear(c.Honorarios * 0.21m), Total = CalculoFactura.Redondear(c.Honorarios * 1.21m), Pagada = m < 9 });
            }
            if (c.Tipo == TipoCliente.Arrendador) continue;
            var secundaria = c.Actividades.FirstOrDefault(a => !a.Principal);
            for (int m = 1; m <= 9; m++)
            {
                if (c.Tipo == TipoCliente.Profesional)
                {
                    recibidas.Add(Rec(c, F(m, 28), "Tesorería General de la Seguridad Social", "Q2827003A", $"RETA-{m:00}", "Cuota de autónomos", 294m, 0m, CategoriaGasto.SeguridadSocial));
                    recibidas.Add(Rec(c, F(m, 12), "Telefonía Ibérica SA", ValidadorNif.ConstruirCif('A', 8000001), $"TI-{m:00}-{c.Id}", "Fibra y móvil profesional", 42.15m, 21m, CategoriaGasto.ServiciosExteriores, comun: secundaria is not null));
                    if (m % 3 == 1) recibidas.Add(Rec(c, F(m, 5), "Seguros Mutua Profesional", ValidadorNif.ConstruirCif('G', 8000002), $"POL-{m:00}", "Seguro de responsabilidad civil (trimestre)", 96m, 0m, CategoriaGasto.ServiciosExteriores, comun: secundaria is not null));
                    recibidas.Add(Rec(c, F(m, 9 + m % 5), "Papelería Ronda SL", ValidadorNif.ConstruirCif('B', 8000003), $"T{m:00}{c.Id}1", "Material de oficina (ticket)", 18m + Azar.Next(0, 6) * 7m, 21m, CategoriaGasto.ConsumosExplotacion));
                    if (secundaria is not null && m % 2 == 0) recibidas.Add(Rec(c, F(m, 16), "Plataforma Cursos Online SL", ValidadorNif.ConstruirCif('B', 8000004), $"PC-{m:00}", "Licencia de software de maquetación", 29m, 21m, CategoriaGasto.ServiciosExteriores, actividadId: secundaria.Id));
                    if (c.TieneEmpleados) recibidas.Add(Rec(c, F(m, 30), "Nómina empleada", "", $"NOM-{m:00}", "Sueldo y Seguridad Social de la empleada", 1650m, 0m, CategoriaGasto.SueldosSalarios));
                    if (c.AlquilaLocal) recibidas.Add(Rec(c, F(m, 1), "Inmuebles Ferraz SL", ValidadorNif.ConstruirCif('B', 8000005), $"ALQ-{m:00}", "Alquiler del despacho", 850m, 21m, CategoriaGasto.Arrendamientos));
                    if (m == 7 && c.Id == tomas.Id) recibidas.Add(Rec(c, F(7, 22), "Suministros Eléctricos Ruano SL", ValidadorNif.ConstruirCif('B', 8000006), "SR-7741", "Material eléctrico para obra", 2150m, 21m, CategoriaGasto.ConsumosExplotacion));
                    if (m % 2 == 1) recibidas.Add(Rec(c, F(m, 20), "Estación de servicio Vallecas", ValidadorNif.ConstruirCif('B', 8000007), $"G{m:00}{c.Id}", "Combustible (ticket)", 48.5m, 21m, CategoriaGasto.ConsumosExplotacion, deducible: c.Id == tomas.Id));
                }
                else
                {
                    int cuantos = 5 + (m % 3);
                    for (int k = 0; k < cuantos; k++)
                    {
                        (string prov, string concepto, decimal baseImp, decimal iva, CategoriaGasto cat) = (k % 5) switch
                        {
                            0 => ("Distribuidora Peninsular SA", "Compra de mercancía", 800m + Azar.Next(0, 20) * 95m, 21m, CategoriaGasto.ConsumosExplotacion),
                            1 => ("Energía del Centro SA", "Electricidad", 180m + Azar.Next(0, 10) * 30m, 21m, CategoriaGasto.ServiciosExteriores),
                            2 => ("Restaurante El Parterre SL", "Comidas de trabajo", 45m + Azar.Next(0, 6) * 20m, 10m, CategoriaGasto.ServiciosExteriores),
                            3 => ("Transportes Rápidos del Sur SL", "Portes y mensajería", 60m + Azar.Next(0, 8) * 25m, 21m, CategoriaGasto.ServiciosExteriores),
                            _ => ("Alquiler de equipos Meridian SL", "Alquiler de equipos y material", 300m + Azar.Next(0, 9) * 85m, 21m, CategoriaGasto.Arrendamientos),
                        };
                        recibidas.Add(Rec(c, F(m, 1 + (k * 5) % 27), prov, ValidadorNif.ConstruirCif(k == 0 ? 'A' : 'B', 8500100 + k), $"{m:00}{k}{c.Id:00}", concepto, baseImp, iva, cat));
                    }
                    if (c.TieneEmpleados) recibidas.Add(Rec(c, F(m, 30), "Nóminas", "", $"NOM-{m:00}", "Nóminas y Seguridad Social de la plantilla", c.Id == luzNorte.Id ? 9800m : 3900m, 0m, CategoriaGasto.SueldosSalarios));
                    if (c.AlquilaLocal) recibidas.Add(Rec(c, F(m, 1), "Castellana Oficinas SA", ValidadorNif.ConstruirCif('A', 8000008), $"CO-{m:00}", "Alquiler de oficina", 2400m, 21m, CategoriaGasto.Arrendamientos));
                    if (c.OperacionesIntracomunitarias && m % 2 == 0) recibidas.Add(Rec(c, F(m, 14), c.Id == ferreteria.Id ? "Werkzeug Nord GmbH" : "Lisboa Higiene Lda", c.Id == ferreteria.Id ? "DE811234567" : "PT501234567", $"EU-{m:00}", "Compra intracomunitaria (autorrepercutida)", 1500m + Azar.Next(0, 8) * 400m, 0m, CategoriaGasto.ConsumosExplotacion));
                }
            }
        }
        db.FacturasRecibidas.AddRange(recibidas);
        db.Honorarios.AddRange(honorarios);
        await db.SaveChangesAsync();

        // --- Obligaciones (matriz de control) -------------------------------------------------------
        var obligaciones = new List<Obligacion>();
        static string Tri(int m) => $"{(m - 1) / 3 + 1}T";
        bool Aplica(Cliente c, string modelo) => modelo switch
        {
            "303" => true,
            "349" => c.OperacionesIntracomunitarias,
            "130" => c.Tipo == TipoCliente.Profesional && c.Presenta130,
            "111" => c.TieneEmpleados,
            "115" => c.AlquilaLocal,
            "123" => false,
            "202" => c.Tipo == TipoCliente.Sociedad,
            "LIBROS" or "CCAA" or "200" => c.Tipo == TipoCliente.Sociedad,
            "390" => true,
            "347" => c.Tipo == TipoCliente.Sociedad || c.Id == pilar.Id || c.Id == beatriz.Id,
            "190" => c.TieneEmpleados,
            "180" => c.AlquilaLocal,
            "100" => c.Tipo != TipoCliente.Sociedad,
            _ => false,
        };
        Obligacion Ob(Cliente c, short ej, string periodo, string modelo, EstadoObligacion estado, Gestor? g = null, DateOnly? fecha = null, decimal? resultado = null) =>
            new() { ClienteId = c.Id, Ejercicio = ej, Periodo = periodo, Modelo = modelo, Estado = estado, GestorId = g?.Id ?? (estado is EstadoObligacion.EnCurso or EstadoObligacion.Presentado ? c.GestorId : null), FechaPresentacion = fecha, Resultado = resultado };

        // Estado del 3T por cliente (la historia de la demo). Clave: modelo -> (estado, gestor, dia de octubre)
        var guion3T = new Dictionary<int, Dictionary<string, (EstadoObligacion E, Gestor? G, int Dia)>>
        {
            [ernesto.Id] = new() { ["303"] = (EstadoObligacion.Presentado, lucia, 5) },
            [carmen.Id] = new() { ["303"] = (EstadoObligacion.Pendiente, null, 0) },
            [joaquin.Id] = new() { ["303"] = (EstadoObligacion.EnCurso, raul, 0) },
            [nuria.Id] = new() { ["303"] = (EstadoObligacion.Pendiente, null, 0), ["130"] = (EstadoObligacion.Pendiente, null, 0) },
            [alvaro.Id] = new() { ["303"] = (EstadoObligacion.EnCurso, raul, 0) },
            [pilar.Id] = new() { ["303"] = (EstadoObligacion.Presentado, marta, 2), ["130"] = (EstadoObligacion.Presentado, marta, 2), ["111"] = (EstadoObligacion.Presentado, marta, 2), ["115"] = (EstadoObligacion.EnCurso, marta, 0) },
            [marcos.Id] = new() { ["303"] = (EstadoObligacion.Pendiente, null, 0), ["130"] = (EstadoObligacion.Pendiente, null, 0) },
            [beatriz.Id] = new() { ["303"] = (EstadoObligacion.Presentado, marta, 3), ["130"] = (EstadoObligacion.Presentado, marta, 3) },
            [tomas.Id] = new() { ["303"] = (EstadoObligacion.EnCurso, raul, 0), ["130"] = (EstadoObligacion.Pendiente, null, 0) },
            [luzNorte.Id] = new() { ["303"] = (EstadoObligacion.EnCurso, lucia, 0), ["111"] = (EstadoObligacion.Presentado, lucia, 5), ["202"] = (EstadoObligacion.EnCurso, lucia, 0) },
            [panaderia.Id] = new() { ["303"] = (EstadoObligacion.Pendiente, null, 0), ["111"] = (EstadoObligacion.Pendiente, null, 0), ["202"] = (EstadoObligacion.Pendiente, null, 0) },
            [ferreteria.Id] = new() { ["303"] = (EstadoObligacion.Presentado, marta, 2), ["349"] = (EstadoObligacion.Presentado, marta, 2), ["111"] = (EstadoObligacion.Presentado, marta, 2), ["202"] = (EstadoObligacion.Presentado, marta, 2) },
            [nexo.Id] = new() { ["303"] = (EstadoObligacion.EnCurso, lucia, 0), ["111"] = (EstadoObligacion.Presentado, lucia, 5), ["115"] = (EstadoObligacion.Pendiente, null, 0), ["202"] = (EstadoObligacion.Pendiente, null, 0) },
            [vega.Id] = new() { ["303"] = (EstadoObligacion.Pendiente, null, 0), ["349"] = (EstadoObligacion.Pendiente, null, 0), ["111"] = (EstadoObligacion.EnCurso, raul, 0), ["202"] = (EstadoObligacion.Pendiente, null, 0) },
        };
        var gestores = new Dictionary<int, Gestor> { [marta.Id] = marta, [lucia.Id] = lucia, [raul.Id] = raul };
        foreach (var c in clientes)
        {
            var g = gestores[c.GestorId];
            foreach (var modelo in new[] { "303", "349", "130", "111", "115", "123" })
            {
                if (!Aplica(c, modelo)) { foreach (var t in new[] { "1T", "2T", "3T", "4T" }) obligaciones.Add(Ob(c, 2026, t, modelo, EstadoObligacion.NoProcede)); continue; }
                obligaciones.Add(Ob(c, 2026, "1T", modelo, EstadoObligacion.Presentado, g, F(4, 8 + c.Id % 10)));
                obligaciones.Add(Ob(c, 2026, "2T", modelo, EstadoObligacion.Presentado, g, F(7, 6 + c.Id % 12)));
                var (e, gg, dia) = guion3T[c.Id].TryGetValue(modelo, out var v) ? v : (EstadoObligacion.Pendiente, null, 0);
                obligaciones.Add(Ob(c, 2026, "3T", modelo, e, gg, e == EstadoObligacion.Presentado ? F(10, dia) : null));
                obligaciones.Add(Ob(c, 2026, "4T", modelo, EstadoObligacion.Pendiente));
            }
            if (c.Tipo == TipoCliente.Sociedad)
            {
                obligaciones.Add(Ob(c, 2026, "1P", "202", EstadoObligacion.Presentado, g, F(4, 14), 1200m + c.Id * 90m));
                var (e, gg, dia) = guion3T[c.Id].TryGetValue("202", out var v2) ? v2 : (EstadoObligacion.Pendiente, null, 0);
                obligaciones.Add(Ob(c, 2026, "2P", "202", e, gg, e == EstadoObligacion.Presentado ? F(10, dia) : null, e == EstadoObligacion.Presentado ? 1200m + c.Id * 90m : null));
                obligaciones.Add(Ob(c, 2026, "3P", "202", EstadoObligacion.Pendiente));
                // Anuales del ejercicio 2025, presentados durante 2026 (asi los lleva el Excel de control)
                obligaciones.Add(Ob(c, 2025, "AN", "LIBROS", EstadoObligacion.Presentado, raul, F(4, 22)));
                obligaciones.Add(Ob(c, 2025, "AN", "CCAA", c.Id == vega.Id ? EstadoObligacion.EnCurso : EstadoObligacion.Presentado, raul, c.Id == vega.Id ? null : F(7, 24)));
                obligaciones.Add(Ob(c, 2025, "AN", "200", EstadoObligacion.Presentado, marta, F(7, 20), 3200m + c.Id * 410m));
            }
            else
            {
                foreach (var t in new[] { "1P", "2P", "3P" }) obligaciones.Add(Ob(c, 2026, t, "202", EstadoObligacion.NoProcede));
                obligaciones.Add(Ob(c, 2025, "AN", "LIBROS", EstadoObligacion.NoProcede));
                obligaciones.Add(Ob(c, 2025, "AN", "CCAA", EstadoObligacion.NoProcede));
                if (Aplica(c, "100")) obligaciones.Add(Ob(c, 2025, "AN", "100", EstadoObligacion.Presentado, g, F(6, 10 + c.Id), -320m + c.Id * 55m));
            }
            obligaciones.Add(Ob(c, 2025, "AN", "390", EstadoObligacion.Presentado, g, F(1, 20 + c.Id % 8)));
            if (Aplica(c, "347")) obligaciones.Add(Ob(c, 2025, "AN", "347", EstadoObligacion.Presentado, g, F(2, 12 + c.Id % 10)));
            if (Aplica(c, "190")) obligaciones.Add(Ob(c, 2025, "AN", "190", EstadoObligacion.Presentado, g, F(1, 18 + c.Id % 9)));
            if (Aplica(c, "180")) obligaciones.Add(Ob(c, 2025, "AN", "180", EstadoObligacion.Presentado, g, F(1, 18 + c.Id % 9)));
        }
        db.Obligaciones.AddRange(obligaciones);
        await db.SaveChangesAsync();

        // --- Resultados del 303 (calculados con el mismo motor que la mesa de IVA) y justificantes ------
        foreach (var c in clientes)
        {
            var anteriores = new List<LiquidacionAnterior>();
            var em = emitidas.Where(f => f.ClienteId == c.Id).Select(f => new LineaIva(f.Fecha, f.Base, f.TipoIva, f.CuotaIva)).ToList();
            var re = recibidas.Where(f => f.ClienteId == c.Id).Select(f => new LineaIva(f.Fecha, f.Base, f.TipoIva, f.CuotaIva, f.Deducible)).ToList();
            foreach (var t in new[] { "1T", "2T", "3T" })
            {
                var o = obligaciones.First(x => x.ClienteId == c.Id && x.Ejercicio == 2026 && x.Periodo == t && x.Modelo == "303");
                if (o.Estado != EstadoObligacion.Presentado) break;
                var r = CalculoIva.Liquidar(2026, t, em, re, anteriores);
                o.Resultado = r.Resultado; o.RepercutidoLiquidado = r.RepercutidoALiquidar; o.SoportadoLiquidado = r.SoportadoALiquidar;
                anteriores.Add(new LiquidacionAnterior(t, r.RepercutidoALiquidar, r.SoportadoALiquidar, r.Resultado));
            }
        }
        foreach (var o in obligaciones.Where(o => o.Estado == EstadoObligacion.Presentado))
        {
            var c = clientes.First(x => x.Id == o.ClienteId);
            var g = gestores[o.GestorId ?? c.GestorId];
            var fecha = o.FechaPresentacion ?? F(7, 15);
            if (o.Modelo is "111" or "115" && o.Resultado is null) o.Resultado = o.Modelo == "111" ? 420m + c.Id * 37m : 178.5m;
            if (o.Modelo == "130" && o.Resultado is null) o.Resultado = 260m + c.Id * 41m;
            o.Presentacion = new Presentacion
            {
                FechaHora = new DateTime(fecha.Year, fecha.Month, fecha.Day, 9 + c.Id % 8, 5 + c.Id * 3 % 50, 12, DateTimeKind.Utc), Expediente = Justificantes.Expediente(o.Ejercicio, o.Modelo), Csv = Justificantes.Csv(),
                NumeroJustificante = Justificantes.NumeroJustificante(o.Modelo), PresentadorNombre = g.Nombre, PresentadorNif = g.Nif, Importe = o.Resultado ?? 0m, Iban = (o.Resultado ?? 0m) > 0 ? c.Iban : null,
            };
        }
        // Fraccionamiento solicitado por Tomas (3T alto por la obra de julio)
        var o303Tomas = obligaciones.First(o => o.ClienteId == tomas.Id && o.Periodo == "3T" && o.Modelo == "303");
        o303Tomas.Fraccionamiento = EstadoFraccionamiento.Solicitado; o303Tomas.FraccionamientoSolicitadoUtc = Utc(2026, 10, 4, 18, 40);
        await db.SaveChangesAsync();

        // --- Documentos subidos ------------------------------------------------------------------------
        var docs = new List<Documento>();
        Documento Doc(Cliente c, TipoDocumento tipo, string nombre, int m, int d, string por, EstadoDocumento estado = EstadoDocumento.Revisado, decimal? importe = null) =>
            new() { ClienteId = c.Id, Tipo = tipo, Nombre = nombre, TipoMime = nombre.EndsWith(".pdf") ? "application/pdf" : "image/jpeg", Tamano = 180_000 + Azar.Next(0, 900) * 1000, SubidoUtc = Utc(2026, m, d, 8 + Azar.Next(0, 12), Azar.Next(0, 59)), SubidoPor = por, Ejercicio = 2026, Periodo = Tri(m), Estado = estado, ImporteDetectado = importe };
        foreach (var c in clientes.Where(x => x.Tipo != TipoCliente.Arrendador))
        {
            string por = c.Tipo == TipoCliente.Sociedad ? (c.NombreContacto ?? c.NombreCorto) : c.NombreCorto;
            for (int m = 1; m <= 9; m++)
            {
                bool tercero = m >= 7;
                var estadoDoc = tercero ? EstadoDocumento.Recibido : EstadoDocumento.Revisado;
                // Historia del 3T: Nuria y Panaderia incompletos; Vega y Soto sin extracto; el resto completo
                if (c.Emisor == QuienEmite.Cliente && !(tercero && c.Id == vega.Id && m == 9)) docs.Add(Doc(c, TipoDocumento.FacturaEmitida, $"emitidas-{m:00}-2026.pdf", m, 26, por, estadoDoc));
                if (!(tercero && c.Id == panaderia.Id && m == 9)) docs.Add(Doc(c, TipoDocumento.FacturaRecibida, $"factura-{(m % 2 == 0 ? "energia" : "proveedor")}-{m:00}.pdf", m, 15, por, estadoDoc, 180m + m * 12m));
                if (c.Tipo == TipoCliente.Profesional)
                {
                    if (!(tercero && c.Id == nuria.Id && m == 9)) docs.Add(Doc(c, TipoDocumento.Ticket, $"ticket-{(m % 2 == 0 ? "gasolina" : "papeleria")}-2026-{m:00}-{9 + m % 5:00}.jpg", m, 9 + m % 5, c.NombreCorto, estadoDoc, 18m + m * 3m));
                    if (!(tercero && c.Id == nuria.Id && m == 9)) docs.Add(Doc(c, TipoDocumento.SeguroOCuota, $"recibo-autonomos-{m:00}.pdf", m, 28, c.NombreCorto, EstadoDocumento.Revisado, 294m));
                    if (c.TieneEmpleados) docs.Add(Doc(c, TipoDocumento.Nomina, $"nomina-{m:00}.pdf", m, 30, c.NombreCorto));
                    if (m % 3 == 0 && !(tercero && c.Id == nuria.Id)) docs.Add(Doc(c, TipoDocumento.ExtractoBancario, $"extracto-{Tri(m)}-2026.pdf", m, 30, c.NombreCorto, estadoDoc));
                }
                else
                {
                    if (c.TieneEmpleados && !(tercero && c.Id == panaderia.Id && m >= 8)) docs.Add(Doc(c, TipoDocumento.Nomina, $"nominas-{m:00}.pdf", m, 30, por));
                    if (!c.ClaveConsultaBanco && m % 3 == 0 && !(tercero && c.Id == vega.Id)) docs.Add(Doc(c, TipoDocumento.ExtractoBancario, $"extracto-{Tri(m)}-2026.pdf", m, 30, por, estadoDoc));
                }
            }
        }
        // Una foto de ticket subida "esta manana" por Tomas, sin revisar
        docs.Add(new Documento { ClienteId = tomas.Id, Tipo = TipoDocumento.Ticket, Nombre = "IMG_20261006_0812.jpg", TipoMime = "image/jpeg", Tamano = 2_140_000, SubidoUtc = reloj.AhoraUtc.AddHours(-2), SubidoPor = tomas.NombreCorto, Ejercicio = 2026, Periodo = "4T", Estado = EstadoDocumento.Recibido, ImporteDetectado = 63.4m });
        db.Documentos.AddRange(docs);

        // --- Extracto bancario de Luz Norte (3T) con movimientos sin conciliar ---------------------------
        var movs = new List<MovimientoBancario>();
        decimal saldo = 41_250.33m;
        void Mov(Cliente c, DateOnly f, string concepto, decimal importe, int? emitida = null, int? recibida = null, EstadoConciliacion estado = EstadoConciliacion.Conciliado)
        { saldo += importe; movs.Add(new MovimientoBancario { ClienteId = c.Id, Fecha = f, Concepto = concepto, Importe = importe, Saldo = saldo, FacturaEmitidaId = emitida, FacturaRecibidaId = recibida, Estado = estado }); }
        var emLN = emitidas.Where(f => f.ClienteId == luzNorte.Id && f.Fecha.Month >= 7).OrderBy(f => f.Fecha).ToList();
        var reLN = recibidas.Where(f => f.ClienteId == luzNorte.Id && f.Fecha.Month >= 7 && !f.EsHonorariosSga).OrderBy(f => f.Fecha).ToList();
        int mi = 0;
        foreach (var f in emLN) { mi++; bool sin = mi % 5 == 0; Mov(luzNorte, f.Fecha.AddDays(12 + mi % 7), $"TRANSF. {f.DestinatarioNombre.ToUpperInvariant()} REF {f.Numero}", f.Total, sin ? null : f.Id, null, sin ? EstadoConciliacion.Pendiente : EstadoConciliacion.Conciliado); }
        foreach (var f in reLN.Take(14)) { mi++; bool sin = mi % 7 == 0; Mov(luzNorte, f.Fecha.AddDays(3), $"RECIBO {f.ProveedorNombre.ToUpperInvariant()}", -f.Total, null, sin ? null : f.Id, sin ? EstadoConciliacion.Pendiente : EstadoConciliacion.Conciliado); }
        Mov(luzNorte, F(8, 3), "COMISIÓN MANTENIMIENTO CUENTA", -18.5m, estado: EstadoConciliacion.Pendiente);
        Mov(luzNorte, F(9, 12), "TRANSF. RECIBIDA CORDILLERA MEDIA LLC (SIN REFERENCIA)", 9450m, estado: EstadoConciliacion.Pendiente);
        Mov(luzNorte, F(9, 27), "ADEUDO SGA CONTABILIZADO SL", -254.1m, estado: EstadoConciliacion.Pendiente);
        // Nexo: extracto 3T subido, conciliado en su mayoria
        saldo = 12_800m;
        foreach (var f in emitidas.Where(f => f.ClienteId == nexo.Id && f.Fecha.Month >= 7)) Mov(nexo, f.Fecha.AddDays(30), $"TRANSF. {f.DestinatarioNombre.ToUpperInvariant()}", f.Total, f.Id);
        movs.Sort((a, b) => a.Fecha.CompareTo(b.Fecha));
        db.Movimientos.AddRange(movs);

        // --- Incidencias y mensajes ----------------------------------------------------------------------
        var incidencias = new List<Incidencia>();
        Incidencia Inc(Cliente c, TipoIncidencia tipo, string titulo, EstadoIncidencia estado, DateTime creada, int? obligacionId, params (string autor, bool gestor, string texto, int minutos)[] mensajes)
        {
            var i = new Incidencia { ClienteId = c.Id, Tipo = tipo, Titulo = titulo, Estado = estado, CreadaUtc = creada, GestorId = c.GestorId, ObligacionId = obligacionId };
            foreach (var (autor, gestor, texto, minutos) in mensajes) i.Mensajes.Add(new Mensaje { Autor = autor, EsGestor = gestor, Texto = texto, FechaUtc = creada.AddMinutes(minutos), LeidoPorCliente = !gestor || estado == EstadoIncidencia.Resuelta, LeidoPorGestor = gestor || estado != EstadoIncidencia.Abierta });
            return i;
        }
        incidencias.Add(Inc(tomas, TipoIncidencia.Fraccionamiento, "Fraccionar el pago del 303 3T", EstadoIncidencia.Abierta, Utc(2026, 10, 4, 18, 40), o303Tomas.Id,
            ("Tomás Iglesias", false, "Este trimestre el IVA sale muy alto por la obra de julio. ¿Se puede fraccionar el pago?", 0)));
        incidencias.Add(Inc(marcos, TipoIncidencia.Numeracion, "Falta la factura 2026-017 y la 2026-021 está repetida", EstadoIncidencia.Abierta, Utc(2026, 10, 5, 11, 20), null,
            ("Lucía Ferrer", true, "Marcos, al revisar tus facturas del trimestre veo que falta la 2026-017 (entre la 016 y la 018) y que la 2026-021 aparece dos veces con fechas distintas. ¿Puedes mirarlo? Si la 017 se anuló, necesito que me lo confirmes.", 0)));
        incidencias.Add(Inc(nuria, TipoIncidencia.Consulta, "¿La cuota de autónomos va en el IVA?", EstadoIncidencia.Resuelta, Utc(2026, 9, 18, 16, 2), null,
            ("Nuria Campos", false, "Hola Lucía, ¿los recibos de la cuota de autónomos los tengo que subir para el IVA?", 0),
            ("Lucía Ferrer", true, "Hola Nuria. La cuota no lleva IVA, así que no cambia lo que pagas de IVA, pero sí es un gasto de tu actividad: baja tu IRPF (el modelo 130). Súbelos cada mes como «cuota o seguro» y yo los llevo al libro de gastos.", 95)));
        incidencias.Add(Inc(luzNorte, TipoIncidencia.Conciliacion, "Cuatro movimientos del extracto sin cuadrar", EstadoIncidencia.EnCurso, Utc(2026, 10, 3, 9, 30), null,
            ("Lucía Ferrer", true, "Andrea, del extracto de 3T me quedan cuatro movimientos sin factura: un ingreso de Cordillera Media de 9.450 € sin referencia y tres cobros que no casan con ninguna factura. ¿Me confirmáis a qué facturas corresponden?", 0),
            ("Andrea Solís", false, "El de Cordillera es un anticipo del proyecto de noviembre; la factura la hacemos esta semana. Los otros los miro con Jorge.", 240)));
        incidencias.Add(Inc(panaderia, TipoIncidencia.Documento, "Faltan las nóminas de agosto y septiembre", EstadoIncidencia.Abierta, Utc(2026, 10, 2, 12, 0), null,
            ("Raúl Ibáñez", true, "Jorge, para presentar el 111 del trimestre necesito las nóminas de agosto y septiembre. Con eso lo cierro el mismo día.", 0)));
        db.Incidencias.AddRange(incidencias);

        // --- Avisos a clientes ------------------------------------------------------------------------------
        var avisos = new List<Aviso>();
        void Av(Cliente c, DateTime f, string texto, string? enlace, bool leido = false) => avisos.Add(new Aviso { ClienteId = c.Id, FechaUtc = f, Texto = texto, Enlace = enlace, Leido = leido });
        var r303Ernesto = obligaciones.First(o => o.ClienteId == ernesto.Id && o.Periodo == "3T" && o.Modelo == "303").Resultado ?? 0m;
        Av(ernesto, Utc(2026, 10, 5, 13, 25), $"Hemos presentado tu IVA del tercer trimestre (modelo 303). Se cargarán {Infraestructura.Formato.Euros(r303Ernesto)} en tu cuenta el 20 de octubre.", "/cliente/presentaciones");
        Av(ernesto, Utc(2026, 10, 2, 9, 15), "Hemos enviado la factura 26/9 a Talleres Mendieta SL. Tienes tu copia en «Mis facturas».", "/cliente/facturas", true);
        Av(joaquin, Utc(2026, 10, 1, 9, 0), "Ya tienes generada la factura de septiembre (26/9). La enviaremos al inquilino en los próximos días.", "/cliente/facturas");
        Av(nuria, Utc(2026, 10, 1, 8, 0), "Empieza la campaña del tercer trimestre: te faltan un ticket, el recibo de autónomos de septiembre y el extracto bancario.", "/cliente/documentos");
        Av(tomas, Utc(2026, 10, 4, 18, 41), "Hemos recibido tu solicitud de fraccionamiento. Raúl la revisa y te contesta en menos de 48 horas.", "/cliente/mensajes");
        Av(marcos, Utc(2026, 10, 5, 11, 21), "Lucía te ha escrito sobre la numeración de tus facturas del trimestre.", "/cliente/mensajes");
        Av(luzNorte, Utc(2026, 10, 3, 9, 31), "Hemos descargado el extracto del tercer trimestre con la clave de consulta. Cuatro movimientos necesitan vuestra confirmación.", "/cliente/mensajes");
        Av(panaderia, Utc(2026, 10, 2, 12, 1), "Para presentar las retenciones (modelo 111) nos faltan las nóminas de agosto y septiembre.", "/cliente/documentos");
        Av(ferreteria, Utc(2026, 10, 2, 17, 45), "Trimestre cerrado: IVA, retenciones, intracomunitarias y pago fraccionado presentados. Ya puedes descargar los justificantes.", "/cliente/presentaciones");
        db.Avisos.AddRange(avisos);
        await db.SaveChangesAsync();
        log.LogInformation("Datos de demostración sembrados: {Clientes} clientes, {Emitidas} facturas emitidas, {Recibidas} recibidas, {Obligaciones} obligaciones.", clientes.Length, emitidas.Count, recibidas.Count, obligaciones.Count);
    }
}
