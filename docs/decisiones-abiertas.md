# Decisiones abiertas

> **Estado:** vivo · **Última actualización:** 2026-09-22 (cierre de DA-01, DA-02 y DA-13 por el usuario)
> Registro de lo que **todavía no está decidido** y de lo que el arquitecto ha decidido *por defecto* para no bloquear el trabajo. Cuando una decisión se cierra, se marca aquí y se refleja en el ADR correspondiente.
>
> **Convención:** mientras una decisión esté abierta, el Agente Programador implementa **la recomendación por defecto**. Ninguna decisión abierta puede detener la construcción.

## Resumen

| Id | Decisión | Recomendación por defecto | Quién decide | Bloquea a | Estado |
|---|---|---|---|---|---|
| DA-01 | Nombre del producto y del repositorio | **`Aserta`** | Usuario | Namespaces, systemd, subdominio, BD, declaración responsable | ✅ **CERRADA 2026-09-22** |
| DA-02 | Identidad del *productor del software* para la declaración responsable | **Datos ficticios en configuración**, sustituibles sin desplegar | Usuario | M6 · declaración responsable | ✅ **CERRADA 2026-09-22** (revisar antes de producción) |
| DA-03 | Servidor de producción: propio o compartido | **Propio.** 4 GB compartidos con Chromium + SQL Server es un riesgo real | Usuario | Despliegue a producción (no la demo) | ABIERTA |
| DA-04 | Ubicación de SQL Server: misma máquina o separada | Misma máquina en desarrollo con `max server memory` limitado; separada en producción si hay presupuesto | Arquitecto + usuario | `12-infraestructura-despliegue.md` | ABIERTA |
| DA-05 | Integración AEAT en la demo | **Simulador**, con opción de apuntar a preproducción real si aparece un certificado | Usuario | M6 | ABIERTA (no bloquea) |
| DA-06 | Certificado con el que se envía a la AEAT | Certificado de la **gestoría** como representante/colaborador social; soporte también para certificado del propio cliente | Usuario (implicación legal) | M6 · producción | ABIERTA → propuesta en [ADR-002](adr/ADR-002-certificado-y-representacion-verifactu.md) |
| DA-07 | Exportador contable de la demo | **A3 + CSV genérico**; formato A3 pendiente de documentación del fabricante `[VERIFICAR]` | Usuario | M5 | ABIERTA (no bloquea) |
| DA-08 | IA/OCR de facturas | Adaptador **simulado** con datos precargados | Arquitecto | M3 | CERRADA por defecto |
| DA-09 | Interactividad JS y estrategia de CSS | **htmx + SortableJS servidos localmente + CSS propio con design tokens** | Arquitecto | Todo el front | CERRADA → [ADR-003](adr/ADR-003-interactividad-razor-pages-y-css.md) |
| DA-10 | Arquitectura de solución y multi-tenant | Monolito modular en 6 proyectos, doble barrera de aislamiento | Arquitecto | Todo | CERRADA → [ADR-001](adr/ADR-001-arquitectura-general-y-estructura-de-solucion.md) |
| DA-11 | Modo NO VERI\*FACTU | **No se implementa.** Solo modo VERI\*FACTU | Arquitecto | M6 | CERRADA por defecto, pendiente de ADR |
| DA-12 | Servicio, puerto, subdominio y base de datos en el servidor de desarrollo | Servicio `aserta-dev`, puerto **5110**, BD `Aserta_Dev`, subdominio por confirmar | Usuario (DNS) | Despliegue en desarrollo | ABIERTA |
| DA-13 | Prioridad de la demo si hay que recortar | **Equilibrada**: núcleo → Kanban → portal/documental → Veri\*Factu → exportación y cuadro de mando | Usuario | Orden del backlog | ✅ **CERRADA 2026-09-22** |
| DA-14 | Territorio foral (País Vasco, Navarra) | **Fuera de alcance**, con bloqueo explícito y mensaje claro en el alta de cliente | Arquitecto | M1 | CERRADA por defecto |
| DA-15 | Repositorio GitHub | Repositorio nuevo en `github.com/jbanon`, nombre = DA-01 | Usuario | CI/CD | ABIERTA |

---

## Detalle de las decisiones abiertas

### DA-01 · Nombre del producto y del repositorio  ✅ CERRADA

**Contexto.** El prompt propone `GestorFlow` como nombre provisional. Sin embargo, el directorio de trabajo, el repositorio git ya inicializado y el script `~/scripts/rc-aserta.sh` del servidor usan **`Aserta`**. Son dos nombres distintos para lo mismo y hay que elegir **antes de escribir la primera línea de C#**, porque el nombre entra en:

- namespaces y nombres de proyecto (`Aserta.Web` vs `GestorFlow.Web`) — renombrar después es caro y sucio;
- nombre del servicio systemd, del subdominio, de la base de datos y del repositorio de GitHub;
- **la declaración responsable de Veri\*Factu**, que identifica el *nombre del sistema informático de facturación* de forma legalmente vinculante.

**Opciones.**

| Opción | A favor | En contra |
|---|---|---|
| **A · `Aserta`** *(recomendada)* | Ya está en el directorio, en git y en los scripts del servidor: cero fricción. Nombre corto, español, serio, encaja con el sector ("asertar" ≈ afirmar con certeza) | Hay que comprobar que no colisiona con marcas registradas del sector `[VERIFICAR]` |
| B · `GestorFlow` | Es el nombre del encargo | Anglicismo mixto; obliga a renombrar directorio, repo y scripts; menos diferenciado (hay muchos "*Flow*") |
| C · Otro nombre | — | Retrasa el arranque |

**DECISIÓN (usuario, 2026-09-22): opción A · `Aserta`.** Los proyectos son `Aserta.Web`, `Aserta.Dominio`, `Aserta.Aplicacion`, `Aserta.Infraestructura`, `Aserta.Verifactu`, `Aserta.Exportacion`. `GestorFlow` queda solo como nombre de trabajo del encargo original. Pendiente menor: comprobar colisión con marcas registradas del sector antes de usar el nombre comercialmente `[VERIFICAR]`.

---

### DA-02 · Productor del software (declaración responsable)  ✅ CERRADA

**Contexto.** El RSIF exige que el sistema informático de facturación lleve una **declaración responsable** del *productor* con su identificación (razón social y NIF), el nombre, identificador y versión del sistema, sus componentes, y la fecha y lugar de suscripción `[VERIFICAR]` contra el texto del RD 1007/2023 y la Orden HAC/1177/2024. Ese mismo bloque de identificación viaja además **dentro de cada registro de facturación** que se remite a la AEAT, y ambos deben ser coherentes.

Esto no es una pantalla decorativa: es una declaración jurídica. No puedo inventar el NIF ni la razón social.

**Qué necesito:** razón social, NIF y domicilio del productor, y si el sistema se comercializa como producto propio o como desarrollo a medida (el contenido exigido difiere `[VERIFICAR]`).

**DECISIÓN (usuario, 2026-09-22):** datos **ficticios** por ahora, leídos de una sección `DeclaracionResponsable` de la configuración y marcados visiblemente como datos de demostración. No es deuda técnica: la declaración cambia con cada versión del software, así que leerla de configuración es el diseño correcto.

**Condición de salida obligatoria:** antes de cualquier uso real —no solo antes de producción, también antes de enseñar la demo a un cliente que pudiera tomarla por un producto homologado— hay que sustituir razón social, NIF y domicilio reales del productor, y hacerlos coherentes con el bloque de identificación del sistema informático que viaja en cada registro. Queda como punto de control en el checklist de `12-infraestructura-despliegue.md`.

---

### DA-03 · Servidor de producción

**Contexto.** El servidor de desarrollo (Hetzner) tiene **3,8 GB de RAM** compartidos con SQL Server, nginx y otros diez proyectos; en la primera inspección quedaban ~840 MB libres y el **swap estaba al 84 %**. Añadir Chromium (el mayor consumidor del stack) a esa máquina para *producción* es pedir un OOM en medio de una demo.

**Recomendación:** servidor propio para producción, con un mínimo de **8 GB**. Reparto orientativo a validar en `12-infraestructura-despliegue.md`: SQL Server 3 GB (`max server memory`), aplicación 700 MB, Chromium 600 MB de pico, sistema y nginx el resto.

**Coste de no decidir:** ninguno para la demo. La demo puede vivir en desarrollo.

---

### DA-04 · Ubicación de SQL Server

**Contexto.** Hay un SQL Server escuchando en el 1433 de la máquina de desarrollo, compartido con otros proyectos. La demo puede usarlo con **base de datos propia** (`Aserta_Dev`), que es lo que recomiendo — no una instancia nueva, que duplicaría el consumo de memoria.

**Pendiente:** confirmar que `max server memory` está limitado; si no lo está, SQL Server se comerá toda la RAM disponible y el resto del servidor sufrirá.

---

### DA-05 · Integración AEAT en la demo

**Recomendación:** simulador (`SimuladorAeat`) como implementación por defecto, seleccionable por configuración, **con capacidad de provocar fallos, latencia y rechazos** — porque enseñar la cola de reintentos vaciándose sola es uno de los mejores momentos de la demo.

Si aparece un certificado válido, apuntar al entorno de **preproducción** de la AEAT es cambiar una clave de configuración. URLs de los servicios: `[VERIFICAR]` en la sede electrónica de la AEAT, **nunca inventar**.

---

### DA-06 · Certificado y representación

Analizada en profundidad en [ADR-002](adr/ADR-002-certificado-y-representacion-verifactu.md). **Requiere validación de un asesor legal antes de producción**; para la demo no bloquea, porque el simulador no valida certificados.

---

### DA-07 · Exportador contable

**Recomendación:** `CsvGenerico` (formato propio, documentado y estable) como implementación de referencia, más un `ExportadorA3` que se documentará cuando se disponga de la especificación oficial del fabricante `[VERIFICAR]`.

**Advertencia:** los formatos de importación de A3, Sage y ContaPlus **dependen de la versión** del producto destino y no son públicos de forma fiable. Inventarlos sería peor que no tenerlos: en la demo, enseñar el CSV genérico bien hecho y decir *"conectamos con su software contable, díganos cuál"* es más creíble que un A3 aproximado.

---

### DA-12 · Servicio, puerto, subdominio y base de datos en desarrollo

**Contexto.** En el servidor de desarrollo están ocupados los puertos **5102, 5104, 5187** (y 1433 de SQL Server, 80/443 de nginx). La convención observada es: servicio systemd en kebab-case (`portal-persycom.service`), sitio nginx `<proyecto>-dev`, proyecto en `~/proyectos/<Proyecto>`, script en `~/scripts/<proyecto>.sh`.

**Propuesta (a confirmar, ninguna IP ni dominio inventado):**

| Elemento | Propuesta |
|---|---|
| Servicio systemd | `aserta-dev.service` |
| Puerto Kestrel (loopback) | **5110** |
| Sitio nginx | `/etc/nginx/sites-available/aserta-dev` |
| Subdominio | *pendiente de confirmar con el usuario* (siguiendo el patrón de los sitios existentes) |
| Base de datos | `Aserta_Dev` en la instancia SQL Server existente |
| Usuario de BD | `aserta_app`, con `DENY UPDATE, DELETE` sobre las tablas de facturación |
| Ruta de documentos | `/var/lib/aserta/documentos` (fuera del directorio de despliegue) |
| Script de despliegue | `~/scripts/aserta.sh` |

---

### DA-13 · Prioridad de la demo  ✅ CERRADA

**DECISIÓN (usuario, 2026-09-22): orden equilibrado.**

1. Núcleo (M0) + datos semilla + ficha de cliente y motor de obligaciones (M1)
2. Kanban y calendario fiscal (M4) — lo más vistoso
3. Portal del cliente (M2) y gestión documental (M3)
4. Facturación Veri\*Factu con simulador de AEAT (M6)
5. Exportación contable (M5) y cuadro de mando del socio (M7)

**Criterio de recorte asociado:** cada fase debe dejar la demo *enseñable por sí sola*. Si hay que recortar, se recorta **profundidad dentro de la última fase alcanzada**, nunca se dejan dos fases a medias. La fecha objetivo sigue sin fijarse; el backlog se ordena por fases, no por fechas.

---

## Decisiones cerradas por defecto (revisables)

| Id | Decisión | Justificación breve |
|---|---|---|
| DA-08 | OCR/IA simulado | Coste y latencia no aportan a la demo; el contrato del adaptador sí |
| DA-09 | htmx + SortableJS + CSS propio | Ver ADR-003: sin Node en el servidor, control total del design system |
| DA-10 | Monolito modular, 6 proyectos | Ver ADR-001 |
| DA-11 | Solo modo VERI\*FACTU | El modo NO VERI\*FACTU exige firma electrónica de registros y registro de eventos adicional: más superficie normativa, cero valor de demo, y es el modo que la AEAT favorece. Pendiente de ADR propio |
| DA-14 | Territorio foral fuera de alcance | Otro sistema (TicketBAI), otro calendario, otra hacienda. Incluirlo duplicaría el motor de reglas |
