# 02 · Catálogo normativo y motor de obligaciones dirigido por datos (M1)

> **Fase 1 · tarea 2** · Agente Programador · 2026-09-22

## Qué he construido

El script `0004_catalogo_datos_2026_2027.sql` (19 modelos, 22 reglas con 33 condiciones, 132 plazos para 2026 y 2027, festivos nacionales 2026–2028) y el motor `MotorObligaciones` en `Aserta.Dominio`: lógica pura, sin base de datos, que evalúa las reglas del catálogo contra el perfil fiscal vigente y devuelve qué crear, qué marcar `NoAplica`, qué reactivar y qué plazo actualizar. `ServicioGeneracionObligaciones` lo orquesta y persiste.

## Decisiones que he tomado yo

1. **Todos los plazos entran con `Confirmado = 0`** y `Fuente = "Patrón general del calendario del contribuyente AEAT, sin contrastar [VERIFICAR]"`. No he buscado ni inventado fechas oficiales: he aplicado el patrón general (día 20 del mes siguiente al trimestre; 4T del 303/130/131 hasta el 30 de enero; mensual hasta el 30 del mes siguiente; 390/190/180/193 en enero; 347/184 en febrero; 200 hasta el 25 de julio; Renta hasta el 30 de junio; 202 en abril/octubre/diciembre; cuentas anuales hasta el 30 de julio; libros hasta el 30 de abril). **Domiciliación = 5 días naturales antes del fin** (15 ó 25) `[VERIFICAR]`. La interfaz marca cada plazo sin confirmar.
2. **El script guarda la fecha ya trasladada** (RD-03) calculándola con `cat.fn_SiguienteDiaHabil` sobre `cat.DiaInhabil` en el propio script. Ejemplo real: 30/01/2027 (sábado) → 01/02/2027. También traslado la fecha de domiciliación `[VERIFICAR]`.
3. **Festivos: solo los nacionales** (2026, 2027 y 2028 porque los plazos del ejercicio 2027 caen en 2028), marcados `[VERIFICAR]`. Sin autonómicos ni locales.
4. **Nuevos códigos de periodo `1P`, `2P`, `3P`** para los pagos fraccionados del 202 (abril, octubre, diciembre); el modelo de datos solo listaba `1T..4T`, `01..12`, `AN`. Rango de fechas asociado: 1P = ene–mar, 2P = abr–sep, 3P = oct–nov `[VERIFICAR]`.
5. **Modelos "no AEAT"** (`CCAA` depósito de cuentas, `LIBROS` legalización) con columna nueva `Organismo` (`AEAT` | `RegistroMercantil`): el alcance dice "se modela la obligación, no el trámite".
6. **Clave natural `Clave` en `ReglaObligacion`** (`303-TRIMESTRAL`, `111-EMPLEADOS`…) para que seeds y tests no dependan de un `IDENTITY`. Las condiciones se reescriben enteras por regla en el script (idempotente por construcción).
7. **OR entre condiciones = dos reglas** (las condiciones de una regla son AND). El 111 y el 190 tienen una regla por "empleados" (prioridad 10) y otra por "profesionales" (prioridad 20); el motor deduplica por modelo+periodo y **conserva como origen la de menor `Prioridad`**. Es para lo que he usado la columna `Prioridad`.
8. **Reglas que he escrito** (el catálogo es dato; corregirlas es un `INSERT`/`UPDATE` en un script nuevo):
   - 303 trimestral/mensual: `RegimenIva IN (General, Simplificado, CriterioCaja)` + periodicidad + `FormaJuridica IN (Autonomo, SL, SA, CB)`. Recargo de equivalencia y exento → sin 303.
   - 390: como 303 pero **solo periodicidad trimestral** (los inscritos en SII/REDEME están exonerados `[VERIFICAR]`).
   - 130: autónomo en directa; 131: autónomo en objetiva. 100: autónomo y particular. 714: sin regla (no hay atributo de patrimonio).
   - 115/180 alquiler; 123/193 capital mobiliario; 349 trimestral si intracomunitarias y IVA no mensual, mensual si IVA mensual `[VERIFICAR umbrales]`; 347 si supera umbral y no es particular; 184 solo CB; 202/200/CCAA/LIBROS solo SL y SA.
   - Territorio foral **no** aparece en las reglas: se bloquea en el dominio (`Cliente.RegistrarPerfil` lanza) y en el motor (perfil foral ⇒ sin obligaciones), DA-14.
9. **Criterio del perfil vigente** (04-modelo-datos §3.2, aislado en `MotorObligaciones.ResolverPerfil`): el vigente **al inicio del periodo**; si el cliente no tenía perfil entonces (alta a mitad de periodo), **el primero que empiece dentro del periodo**. Sin esto, un cliente dado de alta el 1 de julio no tendría 390, 200 ni 2P del 202 en su primer año.
10. **RD-10 como solapamiento**: se genera si el cliente estaba de alta **algún día** del periodo (alta ≤ fin y baja ≥ inicio). Un autónomo de baja el 15 de mayo conserva el 2T.
11. **Reentrancia (riesgo D1)**: el motor **reactiva** una obligación `NoAplica` que vuelve a proceder (→ `PendienteDocumentacion`, evento `Reactivada`) y **actualiza plazos** de las abiertas si el catálogo cambió (evento `PlazoActualizado`). El diagrama de estados pinta `NoAplica` como final; he considerado la reactivación por el motor como la inversa natural de "marcar NoAplica", no como una transición manual (el menú "Mover a…" no la ofrece). **Nunca toca `Presentado` ni `Cerrado`.**
12. **Un plazo que falta en el catálogo no genera la obligación y deja un aviso** (se muestra en la interfaz y va a la auditoría). Prefiero un hueco visible a una fecha inventada.
13. **Ejercicios por defecto**: el actual y el siguiente (hoy 2026 y 2027). Configurable por llamada.
14. **La semilla de datos de demo ejecuta el motor en cada arranque** (es idempotente): sirve para comprobar que reejecutarlo no duplica nada.

## Desviaciones

- `cat.PlazoModelo.FechaLimiteDomiciliacion` admite `NULL` (informativos y trámites mercantiles no tienen ingreso). RD-02 dice "toda obligación tiene dos fechas límite": en esos casos el semáforo cae a la de presentación (`Semaforo.FechaDeReferencia`).
- Periodicidad de reglas incluye `PagoFraccionado` además de las tres del modelo.

## Huecos encontrados

- `02-calendario-fiscal.md` no existe: el seed de plazos es enteramente mío y **debe revisarse antes de enseñarlo como dato fiable**. Está preparado para sustituirse con un script nuevo (`UPDATE ... SET Confirmado = 1`).
- No hay criterio para el 111/115 **mensual** de grandes empresas ni para el 349 por umbral: el perfil no tiene ese atributo.
- `FechaCierreEjercicio` no desplaza todavía el 200 ni las cuentas anuales: los plazos del catálogo asumen cierre 31-dic. Requiere plazos relativos al cierre (extensión del modelo de plazos), no lo he abordado.
- El 349 está en `cat.ModeloTributario` con periodicidad "Trimestral" aunque también sea mensual: la periodicidad real la fija la regla, no el modelo. Sugiero quitar `Periodicidad` del modelo o documentar que es orientativa.

## Cómo probarlo

1. Entrar como `socio@demo.aserta.local` (contraseña `Aserta-Demo-2026!`), **Catálogo normativo** → pestañas reglas / plazos / modelos / días inhábiles. Se ve el aviso de "132 plazos sin confirmar".
2. **Clientes → Nuevo cliente**: NIF `B1234567?` (la pantalla dice qué control espera), SL, alquila local, tiene empleados → al guardar, la ficha muestra las obligaciones de 2026 y 2027 recién generadas (criterio de éxito 1).
3. En la ficha, **Nueva versión del perfil** con vigencia 01/07/2026 y desmarcar "alquila local": el 115 de 3T/4T pasa a `NoAplica`, el de 1T/2T se conserva. Volver a marcarlo: se reactivan.
4. **Recalcular obligaciones** en la ficha: "0 nuevas, ... sin cambios".
5. Cliente de demo "Javier Ortega Campos": el 111 aparece **solo desde el 3T de 2026** (contrata en julio). "Estudio de Arquitectura Norte SL" (alta 01/07/2026): sin 1T/2T ni 1P.

## Estado de los tests

- `Aserta.Dominio.Tests` (67 en verde): NIF/NIE/CIF (válidos, inválidos, normalización, constructores), calendario hábil (sábado, festivo encadenado), rangos de periodo, máquina de estados (todas las transiciones válidas e inválidas, RD-09 con/sin aprobación y con el interruptor del tenant), semáforo (verde/ámbar/rojo/vencido, gris en finales, caída a presentación), y **motor**: autónomo directa, módulos, recargo de equivalencia, SA con IVA mensual, particular, deduplicación por prioridad, RD-10 alta y baja a mitad de ejercicio, cambio de perfil en julio, idempotencia, `NoAplica` sin tocar presentadas, reactivación, actualización de plazos, plazo ausente con aviso, foral, reglas inactivas/fuera de vigencia, atributo desconocido.
- No cubierto: la equivalencia exacta entre `cat.fn_SiguienteDiaHabil` (SQL) y `CalendarioHabil` (C#) no tiene test cruzado; lo he comprobado a mano con el 30/01/2027.
