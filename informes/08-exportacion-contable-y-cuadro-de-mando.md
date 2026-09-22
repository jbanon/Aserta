# 08 · Exportación contable y cuadro de mando del socio (M5 + M7)

> **Fase 5** · Agente Programador · 2026-09-22

## Qué he construido

- **Exportación contable (M5)**: puerto `IExportadorContable` en el dominio, `ExportadorCsvGenerico` (formato propio v1: UTF-8 con BOM, `;`, decimales con coma, cabecera fija de 15 columnas, escapado CSV) y `ExportadorA3` **modelado pero no disponible** (sin especificación oficial, `[VERIFICAR]`, DA-07). `ServicioExportacion` reúne por cliente y periodo las **facturas emitidas Veri\*Factu** (no anuladas; datos reales) y las **facturas recibidas y tickets validados** (datos del extractor, marcados «sugeridos»), excluye lo ya exportado (**RD-11**), genera el fichero, **lo conserva en el almacén cifrado con su SHA-256** y registra qué documentos y facturas incluyó. Script `0009` con `dbo.ExportacionContable`, `dbo.ExportacionDocumento` y `dbo.ExportacionFactura` (RLS en las tres).
- **Pantalla `/Exportacion`** (gestoría): cliente, ejercicio, periodo (trimestre o mes), formato, casilla «incluir ya exportados»; **vista previa** de lo que saldría (nuevos / ya exportados omitidos / validados sin datos), generación, histórico con descarga.
- **Cuadro de mando `/CuadroMando`** (`AsesorOSocio`): alertas arriba (aceptados con errores, reintentos agotados, bloqueados sin certificado, certificados que caducan ≤ 60 días, vencidas, plazos sin confirmar), KPIs (abiertas, vencidas, vencen en 7 días, presentadas este mes), obligaciones por estado en barras, vencimientos de la semana con semáforo, clientes con documentación incompleta (con la frase «te faltan…»), productividad por asesor a 30 días, bloque Veri\*Factu (facturas e importe del mes, aceptados, en cola, con errores). Un solo servicio (`ServicioCuadroMando`) lee las mismas tablas que el tablero, la bandeja y la facturación: **coherente por construcción** (criterio 7).
- **`ServicioAlertaCertificados`** (`ITareaDiaria`): el `AlertaCaducidadCertificadosWorker` que faltaba en la fase 4: un aviso por certificado, umbral (60/30/7) y socio, sin repetir.

## Decisiones que he tomado yo

1. **El puerto `IExportadorContable` vive en `Aserta.Dominio.Exportacion`**, no en `Aplicacion`: `Aserta.Exportacion` solo puede depender de `Dominio` (ADR-001) y `Aplicacion` debe poder invocarlo. Los exportadores se registran en `Program.cs` (composición) como singletons.
2. **Tabla extra `dbo.ExportacionFactura`**: el modelo de datos (§7) solo marca documentos; las facturas emitidas de M6 también se exportan y también deben cumplir RD-11.
3. **Formato CSV propio en vez de inventar el de A3**: DA-07 explícito. El A3 aparece en la lista deshabilitado y con su motivo; al pedirlo por POST el servicio lo rechaza con texto `[VERIFICAR]`.
4. **Los datos de las facturas recibidas son sugeridos** (`DatosConfirmados = N` en la columna 14): salen del extractor simulado y nadie los ha validado línea a línea (RD-06). El programa contable recibe el aviso en el propio fichero. Los documentos validados **sin** datos extraídos no se exportan y se cuentan aparte en la vista previa.
5. **Segunda exportación sin nada nuevo = error de validación con mensaje RD-11**, no un fichero vacío. Con «incluir ya exportados» se genera y queda marcado `IncluyoExportados` («repetición») en el histórico.
6. **El fichero se conserva** (clave en el almacén documental cifrado) con hash y usuario: es la prueba de lo que se entregó. No hay borrado.
7. **Tipo de IVA en facturas emitidas con varias líneas**: se exporta el tipo de mayor base (una fila por factura, no por línea). Un desglose por tipo sería una versión 2 del formato.
8. **Ventanas del cuadro**: obligaciones del ejercicio actual y anterior; incompletos de los últimos 6 meses; productividad a 30 días naturales; «vencidas» = abiertas con fecha de referencia pasada. Constantes en el servicio.
9. **Extractor simulado**: generaba CIF de proveedor con letra `X` (no válido sintácticamente). Corregido con `ValidadorNif.ConstruirCif` (regla «NIF/CIF válidos e inventados»).

## Desviaciones

- `IExportadorContable` en Dominio y no en Aplicacion (decisión 1).
- Tabla `ExportacionFactura` no prevista (decisión 2). `ExportacionContable` lleva además `ClaveAlmacen`, `NombreFichero`, `HashSha256`, `IncluyoExportados`.
- Cuadro de mando accesible también al asesor (`AsesorOSocio`), no solo al socio: no había motivo para ocultarle la carga de su propia cartera. Fácil de restringir en `Program.cs`.

## Huecos encontrados

- **Especificación de A3** (y de cualquier otro programa contable): sin ella el único formato útil es el propio. `[VERIFICAR]`.
- La vista previa recalcula todo en cada petición; con carteras grandes convendría paginar el histórico (limitado a 100) y cachear el cuadro unos minutos.
- El cuadro no tiene todavía filtros por asesor ni por periodo, ni gráficos más allá de barras CSS (sin JS de terceros a propósito).
- La alerta «plazos sin confirmar» seguirá roja hasta que alguien confirme los plazos del catálogo contra el calendario de la AEAT (D-05).

## Cómo probarlo

1. Entrar como `socio@demo.aserta.local` → **Cuadro de mando** (primer enlace del menú). Ver las tres alertas (certificado a 45 días, vencidas, plazos sin confirmar), el bloque Veri\*Factu y «Panadería…: te faltan 2 facturas de compra de julio» en incompletos.
2. **Exportación contable** → Panadería Hermanos Vega, 2026, 3T, CSV → «Ver qué se exportaría» (16 registros: 3 facturas emitidas + 13 recibidas/tickets validados) → «Generar y guardar». Descargar el CSV desde el histórico y abrirlo en Excel/LibreOffice.
3. Repetir la vista previa: «0 registros nuevos, 16 ya exportados». Generar sin la casilla → mensaje RD-11. Con «incluir ya exportados» → segunda fila del histórico marcada «(repetición)».
4. Elegir A3: deshabilitado en el desplegable; si se fuerza, mensaje `[VERIFICAR]`.
5. Simulador AEAT en modo «Aceptado con errores», emitir una factura y procesar la cola: el cuadro muestra la alerta roja arriba.

## Estado de los tests

| Proyecto | Tests | Resultado |
|---|---|---|
| Aserta.Dominio.Tests | 75 (+2: CSV genérico, A3 no disponible) | verde |
| Aserta.Verifactu.Tests | 29 | verde |
| Aserta.Integracion.Tests | 35 (+2: RD-11 de extremo a extremo con aislamiento de tenant y A3; cuadro de mando y alerta de certificados por HTTP y servicio) | verde |

Comprobación manual por HTTP (script de flujo): `/CuadroMando` 200 con 3 alertas y 8 KPIs; exportación de 16 registros, CSV con BOM y cabecera; segunda exportación bloqueada por RD-11; A3 rechazado; cliente redirigido (302) en ambas pantallas; asesor con acceso al cuadro. Sin excepciones en el log.
