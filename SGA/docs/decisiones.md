# Decisiones técnicas de la demo SGA

Formato corto: qué se decidió, por qué, y qué implica. Para alguien que no es del sector.

## D1. Pila: la misma que Aserta, en su propia carpeta
.NET 10 con Razor Pages, htmx servido en local, CSS propio con tokens, SQL Server. Sin Node, sin frameworks de interfaz.
**Por qué:** es la pila cerrada del proyecto y permite reutilizar el código de dominio de Aserta sin adaptadores. **Implica:** `SGA/` tiene su propia solución (`Sga.slnx`), su propio servicio (`sga-demo`, puerto 5120) y su propia URL; no toca la demo de Aserta ni su base.

## D2. Base de datos `SGA` creada por EF Core, no por scripts numerados
El esquema lo crea `EnsureCreated()` al arrancar y los datos ficticios los siembra `Sembrador` si la base está vacía. `Demo:Reiniciar = true` la borra y la vuelve a crear.
**Por qué:** es una demo que se recrea entera en segundos; los scripts idempotentes con runner de Aserta protegen un sistema que evoluciona con clientes reales, que no es el caso. **Implica:** si esto se convierte en producto, el esquema pasa a scripts numerados como en Aserta.

## D3. Reutilización de Aserta: solo el dominio puro
Se referencia `Aserta.Dominio` para el calendario hábil (traslado de plazos por festivo), el validador de NIF y los rangos de periodo. No se reutilizan Identity, el almacén documental cifrado, ni el simulador Veri*Factu (opcional de la fase 7).
**Por qué:** lo demás arrastraría la base de Aserta y su modelo multi-gestoría, que aquí sobra.

## D4. Entrada por perfil sin contraseñas
Cookie de autenticación con dos perfiles (`cliente:{id}`, `gestor:{id}`) y botones de entrada directa.
**Por qué:** lo pide el encargo (G7). **Implica:** no vale para producción; ahí iría Identity con MFA como en Aserta.

## D5. Reloj fijo: 6 de octubre de 2026
La demo vive siempre en plena campaña del tercer trimestre (plazo del 1 al 20 de octubre). Configurable en `Demo:Hoy`; `Demo:RelojReal = true` usa la fecha real.
**Por qué:** que se vea lo mismo cada vez que se enseña.

## D6. PDF dibujado a mano, sin Chromium
Factura de alquiler y justificante se generan con un escritor PDF propio (texto, líneas y cajas con las fuentes base del PDF).
**Por qué:** el servidor solo puede permitirse una instancia de Chromium y ya la usa Aserta. **Implica:** el PDF es fiel en composición pero sin tipografías de marca.

## D7. Los cálculos viven en `Sga.Nucleo`, sin base de datos
IVA acumulado, numeración, retención, libro de gastos e identificadores de justificante son funciones puras probadas contra los oráculos anonimizados de los Excel.
**Por qué:** que el número que ve el cliente sea exactamente el que ve el gestor, y que se pueda probar sin arrancar nada.

## D8. Plazos copiados del catálogo de Aserta, todos marcados «pendiente de confirmar»
`CalendarioFiscal` reproduce los patrones del script `0004` de Aserta (día 20 del mes siguiente, 30 de enero para el 4T, etc.) con traslado por día inhábil. Ninguno se ha contrastado con el calendario oficial.
**Por qué:** regla 2 del encargo: no inventar datos normativos.

## D9. Importar Excel: lectura real, mapeo simulado
El botón «Leer el fichero» abre un `.xlsx` de verdad (es un ZIP con XML) y cuenta las filas con números de cada hoja; no sustituye los libros.
**Por qué:** demostrar que sus ficheros se pueden leer sin comprometer una correspondencia de columnas que no está definida.
