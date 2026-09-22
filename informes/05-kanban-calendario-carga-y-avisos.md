# 05 · Kanban, calendario fiscal, carga por asesor y avisos (M4)

> **Fase 2 · tarea 1** · Agente Programador · 2026-09-22

## Qué he construido

El tablero Kanban (una columna por estado, una tarjeta por obligación, arrastrar y soltar con SortableJS, menú «Mover a…» accesible por teclado, diálogo de detalle con historial), la vista de calendario mensual con los días inhábiles y la agenda de 30 días, la tabla de carga de trabajo por asesor, la vista de lista con cambio de estado en lote y menú por fila, la bandeja de avisos en pantalla (`INotificador`), el planificador de tareas diarias y la tarea de avisos de vencimiento. Además, el sembrador simula ahora el histórico de las obligaciones pasadas para que la demo no nazca "toda vencida".

## Decisiones que he tomado yo

1. **Un solo handler para arrastre y menú** (`Tablero?handler=Mover`): recibe `id`, `destino` y `orden`, valida contra la máquina de estados en el servidor y devuelve **la tarjeta repintada** (parcial `_Tarjeta`) o **400 con el motivo** (texto plano). El arrastre usa `fetch` (control total de la reversión); el menú usa `hx-post` con `hx-target="closest .tarjeta"`. En ambos casos `kanban.js` recoloca la tarjeta en la columna que indica su `data-estado` y recalcula los contadores en cliente. No se repinta la columna entera (ADR-003 §6: medir antes de complicar; con 138 tarjetas la página responde en < 350 ms).
2. **Rechazo visible y anunciado**: si el servidor devuelve 400, la tarjeta vuelve a su posición original, se sacude (animación, desactivada con `prefers-reduced-motion`), aparece un aviso flotante y se anuncia en la región `aria-live`. Verificado por test de integración y por script.
3. **Teclado**: cada tarjeta es enfocable (`tabindex=0`); `Enter` abre el detalle, `M` abre el menú «Mover a…»; el menú navega con flechas y cierra con `Escape`. El menú solo lista los destinos válidos desde el estado actual; los estados finales muestran "Estado final" deshabilitado.
4. **Orden dentro de la columna** (`OrdenEnColumna`): al soltar, la tarjeta toma el índice de destino y las que estaban en esa posición o después se desplazan una. Ordenación de columna: `OrdenEnColumna`, luego fecha de referencia, luego modelo.
5. **`NoAplica` no tiene columna**: no es trabajo. Se ve en la lista (filtro "Todas") y en la ficha del cliente.
6. **Diálogo de detalle** (`<dialog>` nativo + `hx-get` al parcial `_DetalleObligacion`): historial de los últimos 12 eventos y enlace a la ficha completa. Cierra con `Escape` o clic fuera.
7. **Calendario**: rejilla lunes–domingo que cubre el mes, cada obligación en su **fecha de referencia** (domiciliación si existe, presentación si no, RD-02), color de semáforo, días inhábiles atenuados con su nombre en el título, máximo 4 por celda y "+N más" que lleva a la lista filtrada por día. Debajo, "Próximos 30 días" en tabla. En móvil, solo los días del mes con sus obligaciones, en una columna.
8. **Carga por asesor**: obligaciones abiertas por asesor y estado, vencen en 7 días, vencidas y clientes asignados; la barra es un `<progress>` nativo (sin `style=""`, respeta la CSP), en rojo si hay vencidas. Los asesores sin carga también aparecen.
9. **Lista**: casillas + "Mover seleccionadas a…" (cambio en lote, errores agrupados por motivo) y menú «Mover a…» por fila **sin JavaScript** (botones `submit` con `formaction`). Es la vista para móvil.
10. **Avisos (M4 "avisos automáticos")**: tabla `dbo.Aviso` (script `0005`) como adaptador de `INotificador`; idempotente por (destinatario, clave). `ServicioAvisosVencimiento` avisa al asesor (o al primer usuario de gestoría activo si no hay asesor) con **un aviso por obligación y umbral** (`7d`, `3d`, `hoy`, `vencida`), nunca uno por ejecución (misma idea que ADR-002 §6.4 para certificados). Campana en la cabecera con contador de no leídos; página `/Avisos` para leer, marcar todos y, como socio, **ejecutar la tarea ahora** (para la demo).
11. **`PlanificadorDiario`**: un `BackgroundService` con `PeriodicTimer` cada hora que consulta `dbo.EjecucionProgramada` por tarea; si hoy no se ejecutó, la ejecuta **una vez por gestoría activa** abriendo un ámbito de tenant explícito (RLS incluido). Espera 15 s tras el arranque para no competir con migraciones y seed. Registra `UltimaEjecucionUtc` y `ProximaEjecucionUtc` (06:00 del día siguiente, orientativo).
12. **Histórico simulado en el seed**: las obligaciones de demo cuyo plazo pasó hace más de 10 días recorren la máquina de estados con fechas anteriores al plazo (documentación → preparación → revisión → aprobación del cliente por el usuario `panaderia@…` → presentado → cerrado, con importe de borrador), dejando 1 de cada 23 vencida y 1 de cada 3 en `Presentado` sin cerrar. Se ejecuta una sola vez (marcador `SeedHistoricoDemo` en `EjecucionProgramada`). Sin esto, la tarea diaria genera 50 avisos de "vencida" nada más arrancar.
13. **Inicio (`/`) va al tablero** para la gestoría.

## Desviaciones

- ADR-003 sugiere `hx-boost` y atajos `j`/`k`/`/`: no hechos (solo `Enter`/`M` en tarjetas).
- El calendario no es una vista semanal ni diaria; con la agenda de 30 días y la lista filtrada por día es suficiente para la demo.
- Las columnas del Kanban no son configurables por tenant (el mapa de dominio §3.1 dice "configurable"): los estados son fijos; el nombre visible podría hacerse configurable más adelante.

## Huecos encontrados

- No hay especificación de qué usuario recibe cada aviso (asesor, socio, ambos). He elegido asesor responsable de la obligación; el socio lo ve en la carga por asesor.
- `04-modelo-datos` no tiene tabla de avisos; la he añadido (`dbo.Aviso`) y merece revisión.
- El campo `Obligacion.OrdenEnColumna` existía en el modelo pero sin semántica escrita; la de arriba es mía.

## Cómo probarlo

1. Entrar como `socio@demo.aserta.local`. **Tablero**: arrastrar una tarjeta de «Pendiente documentación» directamente a «Presentado» → vuelve, se sacude y aparece el motivo. Arrastrarla a «Documentación completa» → se queda y cambia el contador.
2. Con teclado: `Tab` hasta una tarjeta, `M`, flechas, `Enter` → se mueve; `Enter` sobre la tarjeta → diálogo con historial.
3. Recorrer una tarjeta hasta «Pendiente aprobación cliente», intentar «Presentado» → RD-09; abrir "ficha completa", "Registrar aprobación del cliente", y ya se puede presentar y cerrar. El historial muestra todo (criterio de éxito 2).
4. **Calendario**: ir a abril de 2026 (pico trimestral); pasar el ratón por el 3 de abril (Viernes Santo). **Carga por asesor**: Carlos tiene más carga que Lucía (Transportes Vega es mensual).
5. **Lista**: filtrar modelo `111`, seleccionar varias, "Mover seleccionadas a" → «Documentación completa».
6. **Avisos**: como `carlos@…` la campana muestra los avisos generados por la tarea diaria; como socio, "Generar avisos de vencimiento ahora" es idempotente (0 nuevos la segunda vez).
7. Móvil (< 900 px): el tablero sigue siendo desplazable en horizontal; la lista con menú por fila es la vista recomendada.

## Estado de los tests

- Nuevos: `FlujoKanbanTests` (2, integración por HTTP con sesión real): transición inválida → 400 con motivo y válida → tarjeta repintada con cabecera de anuncio; flujo completo hasta `Cerrado` con RD-09 y comprobación del historial en base de datos.
- Suite completa: 67 dominio + 23 integración en verde.
- No cubierto: `kanban.js` (arrastre real en navegador), el planificador (probado manualmente: fila en `EjecucionProgramada` y avisos generados tras el arranque), el calendario y la carga (solo prueba de humo 200).
