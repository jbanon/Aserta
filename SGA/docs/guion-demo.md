# Guion de demostración · 10 minutos

Entrar en `/acceso`. El reloj de la demo está fijado al **martes 6 de octubre de 2026**: en plena campaña del tercer trimestre (el IVA se presenta del 1 al 20 de octubre). Nada se envía a Hacienda: todo lo que diga «presentado» es simulado y lo dice en pantalla.

Antes de empezar: `deploy/sga.sh reiniciar-datos` (o `Demo:Reiniciar`) deja la historia como al principio.

## 0. Web pública (1 min) · `/`
**Frase:** «Esto es lo que vería un cliente nuevo: para quién trabajáis, cómo es un trimestre con vosotros y una maqueta del área. Sin banner azul ni apretón de manos.»
- Bajar hasta los tres perfiles (arrendador, autónomo, sociedad) y los cuatro pasos del trimestre. Señalar que los modelos van explicados.

## 1. El cliente autónomo, desde el móvil (3 min) · entrar como **Nuria Campos** (traductora, le facturáis vosotros)
Poner el navegador en modo móvil (390 px) o usar el teléfono.
- **Inicio:** «Hola, Nuria. El anillo dice cuánto queda para cerrar el trimestre y la frase dice exactamente qué falta: *te faltan 1 ticket, 1 recibo de autónomos y el extracto*. Y cuánto va a pagar de IVA, con la fecha del cargo.»
- Pulsar **Subir** (botón central): «Foto al ticket, tipo, trimestre, enviar. Treinta segundos.» Subir cualquier imagen. La pantalla «Recibido» dice qué sigue faltando.
- **Impuestos:** «El IVA cobrado menos el pagado, dibujado. Es el mismo cálculo acumulado de vuestra hoja LIQ IVA.»
- **Mensajes:** la consulta resuelta sobre la cuota de autónomos. «Una gestora con nombre, en menos de 48 horas.»

## 2. El arrendador (1 min) · entrar como **Ernesto Valdés**
- **Inicio:** «Él no tiene que subir nada: las facturas del local las emitís vosotros.» Tarjeta con sus tres facturas del trimestre y estado (enviada al inquilino / copia recibida).
- **Facturas → PDF:** descargar la 26/8. «Es la factura de siempre: base, IVA 21 %, retención 19 %, 1.887 €.»
- **Presentaciones:** el 303 del 3T ya presentado, con su código de verificación y el justificante en PDF.

## 3. Un cliente con problemas (1 min) · entrar como **Tomás Iglesias** y luego **Marcos Leal**
- Tomás, **Impuestos:** el IVA sale alto por una obra; ha pedido **fraccionar** desde el botón. «La solicitud le llega a Raúl como incidencia.»
- Marcos, **Facturas:** él factura solo; el sistema ha detectado un **hueco (falta la 2026-017) y una duplicada (2026-021)** y Lucía se lo ha escrito.

## 4. La mesa de trabajo del gestor (4 min) · entrar como **Lucía Ferrer**
- **Hoy:** «Lo que me toca a mí hoy: modelos sin presentar, incidencias, documentos por revisar, y cuándo vence cada cosa.»
- **Matriz de control:** «Vuestro Excel, en pantalla: un cliente por fila, un modelo por columna. Vacío es pendiente, un nombre es quien lo lleva, verde es presentado, NP es que no procede. Filtrad por persona: *Todas → Raúl*.»
- Pulsar la celda **303 de Ernesto Valdés (verde)** → ficha del cliente. Pestaña **Trimestre**: modelos del 3T, documentación y IVA según libros.
- **Mesa de IVA** (botón arriba): «La hoja LIQ IVA calculada sola: según libros, menos lo liquidado en 1T y 2T, a liquidar. Y debajo, el libro de emitidas y el de recibidas, con sus sumas.» Botón **Exportar a Monitor (CSV)**.
- Volver a la matriz, cliente **Carmen Ruiz**: su 303 está pendiente porque faltan las facturas del trimestre. Ir a **Facturas de alquiler** → **Generar bloque 3T (6)** → aparecen las seis facturas (dos locales × tres meses) numeradas → **Enviar pendientes**. «Registradas en Monitor y enviadas al inquilino con copia a la clienta.»
- Ficha de Carmen → 303 3T → **Lo cojo yo** → **Presentar** → pantalla con las casillas del 303 → **Presentar (simulado)** → justificante PDF con expediente, CSV y número de justificante, como el real. En la matriz la celda pasa a verde con la fecha.
- **Incidencias:** la de Tomás (fraccionamiento): **Conceder**. «Le llega el aviso al momento.» La de Luz Norte: **conciliación** con cuatro movimientos sin cuadrar → botón Conciliación: sugerencias por importe, conciliar, ignorar, preguntar.
- **Calendario:** plazos del mes con festivos; todos marcados «pendiente de confirmar» porque no se ha inventado ninguna fecha.

## Cierre (30 s)
«Todo lo que habéis visto sale de los mismos datos: lo que ve el cliente en el móvil es lo que veis vosotros en la matriz. Los cálculos reproducen vuestros Excel al céntimo, y lo que no sabíamos seguro está marcado como pendiente de confirmar, no inventado.»

## Si preguntan
- **¿Es real lo de Hacienda?** No: presentación simulada. Lo real sería vía colaborador social o apoderamiento, con certificado.
- **¿Y Veri*Factu?** Está estudiado en Aserta (informe 09 de Aserta); no está en esta demo.
- **¿Dónde están los datos?** En este servidor, en una base propia de la demo. Nada de los ficheros reales de SGA entra en la demo.
