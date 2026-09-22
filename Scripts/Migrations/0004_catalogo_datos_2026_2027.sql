-- =============================================================================
-- 0004_catalogo_datos_2026_2027.sql  ·  Datos del catalogo normativo
-- -----------------------------------------------------------------------------
-- Idempotente (MERGE / WHEN NOT MATCHED). NO edita scripts anteriores.
-- Contenido: dias inhabiles 2026-2028, modelos tributarios, reglas de obligacion
-- con sus condiciones, y plazos de los ejercicios 2026 y 2027.
--
-- *** TODO DATO NORMATIVO DE ESTE SCRIPT ESTA SIN CONTRASTAR CONTRA LA AEAT ***
-- Los plazos siguen el PATRON GENERAL del calendario del contribuyente (dia 20
-- del mes siguiente al trimestre, dia 30 de enero para el 4T del 303, etc.) y
-- entran con Confirmado = 0. La interfaz los marca como "sin confirmar".
-- Resolver los [VERIFICAR] NO es tarea del programador (regla 3 del encargo):
-- cuando se contrasten, un script nuevo pondra Confirmado = 1 y corregira fechas.
-- Fuente a consultar: Calendario del contribuyente de la AEAT del ejercicio.
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- 1. Dias inhabiles nacionales [VERIFICAR contra el calendario laboral BOE] -----------
MERGE cat.DiaInhabil AS d
USING (VALUES
    ('2026-01-01', N'Año Nuevo'), ('2026-01-06', N'Epifanía del Señor'), ('2026-04-03', N'Viernes Santo'),
    ('2026-05-01', N'Fiesta del Trabajo'), ('2026-08-15', N'Asunción de la Virgen'), ('2026-10-12', N'Fiesta Nacional de España'),
    ('2026-11-01', N'Todos los Santos'), ('2026-12-08', N'Inmaculada Concepción'), ('2026-12-25', N'Natividad del Señor'),
    ('2027-01-01', N'Año Nuevo'), ('2027-01-06', N'Epifanía del Señor'), ('2027-03-26', N'Viernes Santo'),
    ('2027-05-01', N'Fiesta del Trabajo'), ('2027-08-15', N'Asunción de la Virgen'), ('2027-10-12', N'Fiesta Nacional de España'),
    ('2027-11-01', N'Todos los Santos'), ('2027-12-06', N'Día de la Constitución'), ('2027-12-08', N'Inmaculada Concepción'), ('2027-12-25', N'Natividad del Señor'),
    ('2028-01-01', N'Año Nuevo'), ('2028-01-06', N'Epifanía del Señor'), ('2028-04-14', N'Viernes Santo'),
    ('2028-05-01', N'Fiesta del Trabajo'), ('2028-08-15', N'Asunción de la Virgen'), ('2028-10-12', N'Fiesta Nacional de España'),
    ('2028-11-01', N'Todos los Santos'), ('2028-12-06', N'Día de la Constitución'), ('2028-12-08', N'Inmaculada Concepción'), ('2028-12-25', N'Natividad del Señor')
) AS v (Fecha, Descripcion)
ON d.Fecha = CAST(v.Fecha AS date) AND d.Ambito = N'Nacional'
WHEN NOT MATCHED THEN INSERT (Fecha, Ambito, Descripcion) VALUES (CAST(v.Fecha AS date), N'Nacional', v.Descripcion + N' [VERIFICAR]');
GO

-- 2. Modelos tributarios ----------------------------------------------------------------
MERGE cat.ModeloTributario AS m
USING (VALUES
    (N'303',    N'IVA. Autoliquidación',                              N'Declaración periódica del IVA repercutido y soportado.', N'Tu declaración de IVA: lo que has cobrado de IVA menos lo que has pagado.', N'Trimestral', 0, NULL,   N'AEAT'),
    (N'390',    N'IVA. Resumen anual',                                N'Declaración informativa anual de IVA.', N'Resumen del IVA de todo el año. No se paga nada: es informativo.', N'Anual', 1, N'303', N'AEAT'),
    (N'130',    N'IRPF. Pago fraccionado. Estimación directa',        N'Pago a cuenta trimestral del IRPF para actividades en estimación directa.', N'Anticipo trimestral de tu IRPF calculado sobre tu beneficio real.', N'Trimestral', 0, NULL, N'AEAT'),
    (N'131',    N'IRPF. Pago fraccionado. Estimación objetiva',       N'Pago a cuenta trimestral del IRPF para actividades en módulos.', N'Anticipo trimestral de tu IRPF por módulos.', N'Trimestral', 0, NULL, N'AEAT'),
    (N'111',    N'Retenciones. Rendimientos del trabajo y profesionales', N'Retenciones e ingresos a cuenta de nóminas y facturas de profesionales.', N'Lo que has retenido en nóminas y facturas de profesionales, para ingresarlo a Hacienda.', N'Trimestral', 0, NULL, N'AEAT'),
    (N'190',    N'Retenciones. Resumen anual del 111',                N'Declaración informativa anual de retenciones del trabajo y profesionales.', N'Resumen anual de las retenciones de nóminas y profesionales. Informativo.', N'Anual', 1, N'111', N'AEAT'),
    (N'115',    N'Retenciones. Arrendamientos de inmuebles urbanos',  N'Retenciones sobre alquileres de locales.', N'La retención del alquiler de tu local, para ingresarla a Hacienda.', N'Trimestral', 0, NULL, N'AEAT'),
    (N'180',    N'Retenciones. Resumen anual del 115',                N'Declaración informativa anual de retenciones por arrendamientos.', N'Resumen anual de las retenciones del alquiler. Informativo.', N'Anual', 1, N'115', N'AEAT'),
    (N'123',    N'Retenciones. Capital mobiliario',                   N'Retenciones sobre dividendos, intereses y otros rendimientos del capital mobiliario.', N'Retención de los dividendos o intereses que has repartido.', N'Trimestral', 0, NULL, N'AEAT'),
    (N'193',    N'Retenciones. Resumen anual del 123',                N'Declaración informativa anual de retenciones del capital mobiliario.', N'Resumen anual de las retenciones de dividendos e intereses. Informativo.', N'Anual', 1, N'123', N'AEAT'),
    (N'349',    N'Operaciones intracomunitarias',                     N'Declaración recapitulativa de entregas y adquisiciones intracomunitarias.', N'Listado de tus compras y ventas con empresas de otros países de la UE. Informativo.', N'Trimestral', 1, NULL, N'AEAT'),
    (N'347',    N'Operaciones con terceros',                          N'Declaración anual de operaciones con terceras personas superiores a 3.005,06 €.', N'Listado de clientes y proveedores con los que has superado 3.005,06 € en el año. Informativo.', N'Anual', 1, NULL, N'AEAT'),
    (N'184',    N'Entidades en régimen de atribución de rentas',      N'Declaración informativa anual de comunidades de bienes y otras entidades en atribución de rentas.', N'Reparto de los resultados de la comunidad de bienes entre sus miembros. Informativo.', N'Anual', 1, NULL, N'AEAT'),
    (N'202',    N'Impuesto sobre Sociedades. Pago fraccionado',       N'Pagos a cuenta del Impuesto sobre Sociedades (abril, octubre y diciembre).', N'Anticipo del Impuesto sobre Sociedades de tu empresa.', N'PagoFraccionado', 0, NULL, N'AEAT'),
    (N'200',    N'Impuesto sobre Sociedades. Declaración anual',      N'Autoliquidación anual del Impuesto sobre Sociedades.', N'El impuesto anual sobre el beneficio de tu empresa.', N'Anual', 0, NULL, N'AEAT'),
    (N'100',    N'IRPF. Declaración anual (Renta)',                   N'Declaración anual del Impuesto sobre la Renta de las Personas Físicas.', N'Tu declaración de la Renta.', N'Anual', 0, NULL, N'AEAT'),
    (N'714',    N'Impuesto sobre el Patrimonio',                      N'Declaración anual del Impuesto sobre el Patrimonio.', N'Declaración anual de tu patrimonio (solo si superas el mínimo).', N'Anual', 0, NULL, N'AEAT'),
    (N'CCAA',   N'Depósito de cuentas anuales',                       N'Depósito de las cuentas anuales en el Registro Mercantil (obligación mercantil, no tributaria).', N'Presentar las cuentas de tu empresa en el Registro Mercantil.', N'Anual', 1, NULL, N'RegistroMercantil'),
    (N'LIBROS', N'Legalización de libros',                            N'Legalización telemática de los libros contables y societarios en el Registro Mercantil.', N'Legalizar los libros contables de tu empresa en el Registro Mercantil.', N'Anual', 1, NULL, N'RegistroMercantil')
) AS v (Codigo, Nombre, Descripcion, DescripcionCliente, Periodicidad, EsInformativo, EsResumenAnualDe, Organismo)
ON m.Codigo = v.Codigo
WHEN NOT MATCHED THEN INSERT (Codigo, Nombre, Descripcion, DescripcionCliente, Periodicidad, EsInformativo, EsResumenAnualDe, Organismo)
    VALUES (v.Codigo, v.Nombre, v.Descripcion, v.DescripcionCliente, v.Periodicidad, v.EsInformativo, v.EsResumenAnualDe, v.Organismo);
GO

-- 3. Reglas de obligacion (RD-01) ----------------------------------------------------------
-- Condiciones AND. Para OR (111 por empleados O por profesionales) hay dos reglas;
-- el motor deduplica por modelo+periodo conservando la de menor Prioridad.
MERGE cat.ReglaObligacion AS r
USING (VALUES
    (N'303-TRIMESTRAL',    N'303',    N'IVA trimestral (régimen general, simplificado o criterio de caja)', N'Trimestral',      100),
    (N'303-MENSUAL',       N'303',    N'IVA mensual (REDEME / SII)',                                        N'Mensual',         100),
    (N'390-ANUAL',         N'390',    N'Resumen anual de IVA (exonerados los inscritos en SII) [VERIFICAR]', N'Anual',          100),
    (N'130-TRIMESTRAL',    N'130',    N'Pago fraccionado IRPF, autónomo en estimación directa',             N'Trimestral',      100),
    (N'131-TRIMESTRAL',    N'131',    N'Pago fraccionado IRPF, autónomo en estimación objetiva (módulos)',  N'Trimestral',      100),
    (N'111-EMPLEADOS',     N'111',    N'Retenciones por tener empleados',                                   N'Trimestral',       10),
    (N'111-PROFESIONALES', N'111',    N'Retenciones por pagar a profesionales con retención',               N'Trimestral',       20),
    (N'190-EMPLEADOS',     N'190',    N'Resumen anual de retenciones por tener empleados',                  N'Anual',            10),
    (N'190-PROFESIONALES', N'190',    N'Resumen anual de retenciones por pagar a profesionales',            N'Anual',            20),
    (N'115-ALQUILER',      N'115',    N'Retenciones por alquiler de local',                                 N'Trimestral',      100),
    (N'180-ALQUILER',      N'180',    N'Resumen anual de retenciones por alquiler de local',                N'Anual',           100),
    (N'123-CAPITAL',       N'123',    N'Retenciones por reparto de capital mobiliario',                     N'Trimestral',      100),
    (N'193-CAPITAL',       N'193',    N'Resumen anual de retenciones del capital mobiliario',               N'Anual',           100),
    (N'349-TRIMESTRAL',    N'349',    N'Operaciones intracomunitarias, declaración trimestral [VERIFICAR umbrales]', N'Trimestral', 100),
    (N'349-MENSUAL',       N'349',    N'Operaciones intracomunitarias, declaración mensual (IVA mensual)',  N'Mensual',         100),
    (N'347-ANUAL',         N'347',    N'Operaciones con terceros superiores a 3.005,06 €',                  N'Anual',           100),
    (N'184-ANUAL',         N'184',    N'Comunidad de bienes: atribución de rentas',                         N'Anual',           100),
    (N'202-PAGOS',         N'202',    N'Sociedad: pagos fraccionados del Impuesto sobre Sociedades',        N'PagoFraccionado', 100),
    (N'200-ANUAL',         N'200',    N'Sociedad: Impuesto sobre Sociedades',                               N'Anual',           100),
    (N'CCAA-ANUAL',        N'CCAA',   N'Sociedad: depósito de cuentas anuales',                             N'Anual',           100),
    (N'LIBROS-ANUAL',      N'LIBROS', N'Sociedad: legalización de libros',                                  N'Anual',           100),
    (N'100-RENTA',         N'100',    N'Persona física: declaración de la Renta',                           N'Anual',           100)
) AS v (Clave, ModeloCodigo, Descripcion, Periodicidad, Prioridad)
ON r.Clave = v.Clave
WHEN NOT MATCHED THEN INSERT (Clave, ModeloCodigo, Descripcion, Periodicidad, VigenteDesdeEjercicio, VigenteHastaEjercicio, Prioridad, Activa)
    VALUES (v.Clave, v.ModeloCodigo, v.Descripcion, v.Periodicidad, 2026, NULL, v.Prioridad, 1);
GO

-- Condiciones: se reescriben enteras por regla (idempotente por construccion).
DECLARE @c TABLE (Clave nvarchar(60), Atributo nvarchar(60), Operador nvarchar(4), Valor nvarchar(200));
INSERT INTO @c VALUES
    (N'303-TRIMESTRAL',    N'RegimenIva',      N'IN', N'General,Simplificado,CriterioCaja'),
    (N'303-TRIMESTRAL',    N'PeriodicidadIva', N'=',  N'Trimestral'),
    (N'303-TRIMESTRAL',    N'FormaJuridica',   N'IN', N'Autonomo,SL,SA,CB'),
    (N'303-MENSUAL',       N'RegimenIva',      N'IN', N'General,Simplificado,CriterioCaja'),
    (N'303-MENSUAL',       N'PeriodicidadIva', N'=',  N'Mensual'),
    (N'303-MENSUAL',       N'FormaJuridica',   N'IN', N'Autonomo,SL,SA,CB'),
    (N'390-ANUAL',         N'RegimenIva',      N'IN', N'General,Simplificado,CriterioCaja'),
    (N'390-ANUAL',         N'PeriodicidadIva', N'=',  N'Trimestral'),
    (N'390-ANUAL',         N'FormaJuridica',   N'IN', N'Autonomo,SL,SA,CB'),
    (N'130-TRIMESTRAL',    N'FormaJuridica',   N'=',  N'Autonomo'),
    (N'130-TRIMESTRAL',    N'RegimenIrpf',     N'IN', N'DirectaNormal,DirectaSimplificada'),
    (N'131-TRIMESTRAL',    N'FormaJuridica',   N'=',  N'Autonomo'),
    (N'131-TRIMESTRAL',    N'RegimenIrpf',     N'=',  N'Objetiva'),
    (N'111-EMPLEADOS',     N'TieneEmpleados',  N'=',  N'1'),
    (N'111-PROFESIONALES', N'PagaProfesionalesConRetencion', N'=', N'1'),
    (N'190-EMPLEADOS',     N'TieneEmpleados',  N'=',  N'1'),
    (N'190-PROFESIONALES', N'PagaProfesionalesConRetencion', N'=', N'1'),
    (N'115-ALQUILER',      N'AlquilaLocal',    N'=',  N'1'),
    (N'180-ALQUILER',      N'AlquilaLocal',    N'=',  N'1'),
    (N'123-CAPITAL',       N'RepartePagosCapitalMobiliario', N'=', N'1'),
    (N'193-CAPITAL',       N'RepartePagosCapitalMobiliario', N'=', N'1'),
    (N'349-TRIMESTRAL',    N'OperacionesIntracomunitarias', N'=', N'1'),
    (N'349-TRIMESTRAL',    N'PeriodicidadIva', N'<>', N'Mensual'),
    (N'349-MENSUAL',       N'OperacionesIntracomunitarias', N'=', N'1'),
    (N'349-MENSUAL',       N'PeriodicidadIva', N'=',  N'Mensual'),
    (N'347-ANUAL',         N'SuperaUmbral347', N'=',  N'1'),
    (N'347-ANUAL',         N'FormaJuridica',   N'<>', N'Particular'),
    (N'184-ANUAL',         N'FormaJuridica',   N'=',  N'CB'),
    (N'202-PAGOS',         N'FormaJuridica',   N'IN', N'SL,SA'),
    (N'200-ANUAL',         N'FormaJuridica',   N'IN', N'SL,SA'),
    (N'CCAA-ANUAL',        N'FormaJuridica',   N'IN', N'SL,SA'),
    (N'LIBROS-ANUAL',      N'FormaJuridica',   N'IN', N'SL,SA'),
    (N'100-RENTA',         N'FormaJuridica',   N'IN', N'Autonomo,Particular');

DELETE rc FROM cat.ReglaCondicion rc JOIN cat.ReglaObligacion r ON r.Id = rc.ReglaId WHERE r.Clave IN (SELECT DISTINCT Clave FROM @c);
INSERT INTO cat.ReglaCondicion (ReglaId, Atributo, Operador, Valor)
SELECT r.Id, c.Atributo, c.Operador, c.Valor FROM @c c JOIN cat.ReglaObligacion r ON r.Clave = c.Clave;
GO

-- 4. Plazos de los ejercicios 2026 y 2027 [VERIFICAR TODOS] ------------------------------------
-- Patron: (modelo, periodo, inicio, fin nominal, dia de domiciliacion). Off* = anos a sumar al ejercicio.
-- DiaFin = 0 significa "ultimo dia del mes". DiaDom NULL = modelo sin ingreso (sin domiciliacion).
DECLARE @p TABLE (Modelo nvarchar(10), Periodo nvarchar(2), OffIni int, MesIni int, DiaIni int, OffFin int, MesFin int, DiaFin int, DiaDom int NULL);

-- Trimestrales con plazo hasta el dia 20 del mes siguiente (4T en enero del ano siguiente)
INSERT INTO @p
SELECT m.Codigo, t.Periodo, 0, t.MesIni, 1, t.OffFin, t.MesFin, 20, 15
FROM (VALUES (N'111'), (N'115'), (N'123')) m (Codigo)
CROSS JOIN (VALUES (N'1T', 4, 0, 4), (N'2T', 7, 0, 7), (N'3T', 10, 0, 10), (N'4T', 1, 1, 1)) t (Periodo, MesIni, OffFin, MesFin);

-- 349 trimestral: informativo (sin domiciliacion); 4T hasta el 30 de enero
INSERT INTO @p VALUES (N'349', N'1T', 0, 4, 1, 0, 4, 20, NULL), (N'349', N'2T', 0, 7, 1, 0, 7, 20, NULL), (N'349', N'3T', 0, 10, 1, 0, 10, 20, NULL), (N'349', N'4T', 1, 1, 1, 1, 1, 30, NULL);

-- 303 / 130 / 131 trimestrales: 1T-3T hasta el 20; 4T hasta el 30 de enero (domiciliacion 25)
INSERT INTO @p
SELECT m.Codigo, t.Periodo, 0, t.MesIni, 1, t.OffFin, t.MesFin, t.DiaFin, t.DiaDom
FROM (VALUES (N'303'), (N'130'), (N'131')) m (Codigo)
CROSS JOIN (VALUES (N'1T', 4, 0, 4, 20, 15), (N'2T', 7, 0, 7, 20, 15), (N'3T', 10, 0, 10, 20, 15), (N'4T', 1, 1, 1, 30, 25)) t (Periodo, MesIni, OffFin, MesFin, DiaFin, DiaDom);

-- 303 mensual (REDEME/SII): hasta el 30 del mes siguiente (febrero: ultimo dia); diciembre hasta el 30 de enero
INSERT INTO @p
SELECT N'303', RIGHT('0' + CAST(n.Mes AS varchar(2)), 2), CASE WHEN n.Mes = 12 THEN 1 ELSE 0 END, (n.Mes % 12) + 1, 1,
       CASE WHEN n.Mes = 12 THEN 1 ELSE 0 END, (n.Mes % 12) + 1, CASE WHEN (n.Mes % 12) + 1 = 2 THEN 0 ELSE 30 END, 25
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12)) n (Mes);

-- 349 mensual: hasta el 20 del mes siguiente; diciembre hasta el 30 de enero
INSERT INTO @p
SELECT N'349', RIGHT('0' + CAST(n.Mes AS varchar(2)), 2), CASE WHEN n.Mes = 12 THEN 1 ELSE 0 END, (n.Mes % 12) + 1, 1,
       CASE WHEN n.Mes = 12 THEN 1 ELSE 0 END, (n.Mes % 12) + 1, CASE WHEN n.Mes = 12 THEN 30 ELSE 20 END, NULL
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12)) n (Mes);

-- Anuales (se presentan en el ano siguiente al ejercicio)
INSERT INTO @p VALUES
    (N'390',    N'AN', 1, 1, 1, 1, 1, 30, NULL),   -- resumen anual IVA: enero
    (N'190',    N'AN', 1, 1, 1, 1, 1, 0,  NULL),   -- enero completo
    (N'180',    N'AN', 1, 1, 1, 1, 1, 0,  NULL),
    (N'193',    N'AN', 1, 1, 1, 1, 1, 0,  NULL),
    (N'347',    N'AN', 1, 2, 1, 1, 2, 0,  NULL),   -- febrero
    (N'184',    N'AN', 1, 2, 1, 1, 2, 0,  NULL),   -- febrero
    (N'200',    N'AN', 1, 7, 1, 1, 7, 25, 20),     -- Sociedades (cierre 31-dic): 1-25 julio
    (N'100',    N'AN', 1, 4, 7, 1, 6, 30, 25),     -- Renta: abril-junio [VERIFICAR fecha de inicio de campana]
    (N'714',    N'AN', 1, 4, 7, 1, 6, 30, 25),
    (N'CCAA',   N'AN', 1, 7, 1, 1, 7, 30, NULL),   -- deposito de cuentas: hasta 30 julio (cierre 31-dic)
    (N'LIBROS', N'AN', 1, 4, 1, 1, 4, 30, NULL);   -- legalizacion de libros: hasta 30 abril

-- 202 pagos fraccionados: abril, octubre, diciembre (1-20; domiciliacion 15)
INSERT INTO @p VALUES (N'202', N'1P', 0, 4, 1, 0, 4, 20, 15), (N'202', N'2P', 0, 10, 1, 0, 10, 20, 15), (N'202', N'3P', 0, 12, 1, 0, 12, 20, 15);

-- Materializacion para cada ejercicio, con traslado por dia inhabil (RD-03) y sin tocar filas existentes
DECLARE @ej TABLE (Ejercicio smallint);
INSERT INTO @ej VALUES (2026), (2027);

MERGE cat.PlazoModelo AS pm
USING (
    SELECT p.Modelo, e.Ejercicio, p.Periodo,
           DATEFROMPARTS(e.Ejercicio + p.OffIni, p.MesIni, p.DiaIni) AS Inicio,
           CASE WHEN p.DiaDom IS NULL THEN NULL
                ELSE cat.fn_SiguienteDiaHabil(DATEFROMPARTS(e.Ejercicio + p.OffFin, p.MesFin, p.DiaDom), N'Nacional') END AS Domiciliacion,
           cat.fn_SiguienteDiaHabil(
               CASE WHEN p.DiaFin = 0 THEN EOMONTH(DATEFROMPARTS(e.Ejercicio + p.OffFin, p.MesFin, 1))
                    ELSE DATEFROMPARTS(e.Ejercicio + p.OffFin, p.MesFin, p.DiaFin) END, N'Nacional') AS Presentacion
    FROM @p p CROSS JOIN @ej e
) AS v
ON pm.ModeloCodigo = v.Modelo AND pm.Ejercicio = v.Ejercicio AND pm.Periodo = v.Periodo
WHEN NOT MATCHED THEN INSERT (ModeloCodigo, Ejercicio, Periodo, FechaInicioPresentacion, FechaLimiteDomiciliacion, FechaLimitePresentacion, Fuente, Confirmado)
    VALUES (v.Modelo, v.Ejercicio, v.Periodo, v.Inicio, v.Domiciliacion, v.Presentacion,
            N'Patrón general del calendario del contribuyente AEAT, sin contrastar [VERIFICAR]', 0);
GO
