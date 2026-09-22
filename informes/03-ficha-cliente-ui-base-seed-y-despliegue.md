# 03 · Ficha de cliente, interfaz base, datos de demo y despliegue

> **Fase 1 · tarea 3** · Agente Programador · 2026-09-22

## Qué he construido

La aplicación web Razor Pages con login, listado y ficha de clientes (resumen, obligaciones por ejercicio, versiones del perfil fiscal, historial de auditoría), alta y edición de cliente, nueva versión de perfil, lista y detalle de obligaciones con "Mover a…" validado en servidor, catálogo normativo, usuarios, configuración del tenant, auditoría y guía de estilo. CSS propio con design tokens (claro/oscuro), htmx y SortableJS servidos localmente, CSP estricta. Sembrador de datos ficticios y ficheros de despliegue.

## Decisiones que he tomado yo

1. **No hay `08-ux-ui/design-system.md`**, así que el design system es mío: tokens en `tokens.css` (color, espaciado, tipografía, radios, sombras, semáforo y colores por estado de obligación), modo oscuro por `prefers-color-scheme` + `data-tema`, tipografía **Inter variable autoalojada** (OFL, licencia incluida), densidad alta en gestoría. La página **/Estilo** enseña todos los componentes en ambos temas. Cuando exista el documento, la sustitución es cambiar tokens.
2. **htmx 2.0.8 y SortableJS 1.15.6** en `wwwroot/lib/` con versión en el nombre, `allowEval = false`, cabecera antiforgery global desde `app.js`, sin `hx-on:` ni `style=""` (la CSP no lleva `unsafe-inline` ni `unsafe-eval`; comprobado por test). La confirmación de acciones destructivas usa `data-confirmar` + `htmx:confirm`/`submit`, no `hx-confirm`.
3. **Fase 1 sin htmx en caliente**: todas las páginas funcionan con formularios completos (no-JS). Los fragmentos parciales llegan con el Kanban (fase 2); la infraestructura (partials `_TablaObligaciones`, `_Semaforo`, cabecera `X-Aserta-Anuncio` para `aria-live`) ya está.
4. **Detalle de obligación con "Mover a…" y "Registrar aprobación del cliente"** ya en fase 1: hace falta para enseñar el historial (criterio de éxito 2) y para probar RD-09 en la interfaz. La aprobación real desde el portal es de la fase 3; mientras tanto el gestor registra una conformidad "recibida por otro canal". Un `Administrativo` no puede marcar `Presentado` (regla de actores).
5. **Lista de obligaciones plana** en `/Obligaciones` (filtro por ejercicio, estado, asesor y modelo): no es el Kanban, pero deja la fase enseñable y sirve de vista de lista para móvil (ADR-003 §6).
6. **Alta de cliente = datos + perfil en un solo formulario**, y al guardar se ejecuta el motor y se redirige a la pestaña de obligaciones con el recuento ("el motor ha generado N obligaciones"). Editar datos reejecuta el motor (forma jurídica y fecha de alta afectan). Editar el perfil siempre crea versión nueva; si la vigencia coincide con la de la versión vigente, la sustituye (corrección, no cambio).
7. **Validación de NIF con mensaje explicativo** ("se esperaba la letra X") y **coherencia forma jurídica / identificador**: sociedad o CB exige CIF; autónomo o particular exige NIF/NIE. Territorio foral bloqueado con mensaje claro (DA-14).
8. **RD-07 en el listado**: si la gestoría desactiva "los asesores ven todos los clientes", un `Asesor` solo ve los suyos en `/Clientes` y `/Obligaciones` (`ServicioClientes.ConsultaVisiblesAsync`). Socio y administrativo ven todo.
9. **Cultura `es-ES`** para presentación y un **binder de decimales** que acepta coma y punto (evita el clásico fallo de `input type=number` con cultura española).
10. **Sembrador en C#, no en `Scripts/Seed/`**: las contraseñas de Identity se hashean con `UserManager` y el alta de clientes pasa por los mismos servicios que la interfaz (validación de NIF, motor). Se ejecuta con `Demo:Sembrar = true` (activado en `appsettings.Development.json` y en la unidad systemd). Idempotente: no recrea lo que existe; sí reejecuta el motor. La carpeta `Scripts/Seed/` queda vacía a propósito.
11. **Datos ficticios**: gestoría "Asesoría Montalbán & Ruiz", 4 usuarios de gestoría + 1 de cliente (contraseña común `Aserta-Demo-2026!`, configurable en `Demo:Contrasena`), 9 clientes con perfiles distintos a propósito (SL con empleados y local, autónomo que contrata en julio, CB, módulos, SA en REDEME con intracomunitarias, SL que reparte dividendos, minorista en recargo de equivalencia, particular, SL de alta a mitad de ejercicio). NIF/CIF construidos con dígito de control válido; correos en `demo.aserta.local` / `ejemplo.invalid`.
12. **Reloj con desplazamiento** (`Demo:DesplazamientoDias`) para "viajar" a abril en la demo sin tocar el sistema.
13. **Despliegue**: no tengo `sudo`, así que dejo en `deploy/` la unidad systemd (`MemoryMax=700M`, `TimeoutStartSec=180`), el sitio nginx (subdominio pendiente, DA-12), el script `aserta.sh` (`publicar`, `si-hay-cambios`, `estado`) y `LEEME.md` con los pasos. **"Un push a master despliega"** lo resuelvo con un cron cada 2 minutos que sondea `origin/master` y publica si avanzó: no hay receptor de webhooks y no quiero añadir procesos. Mientras tanto la web corre a mano en `127.0.0.1:5110`.
14. **Data Protection**: claves en `DataProtection:RutaClaves` (fuera de `publicado/`, misma convención que otros proyectos del servidor); en desarrollo usa la carpeta por defecto del usuario.
15. **`/salud`** es el health check de ASP.NET (base de datos aún no incluida en la comprobación; se ampliará con Chromium y la cola en la fase 4).

## Desviaciones

- ADR-003 pide `hx-boost` como mejora y atajos de teclado: no hechos.
- No hay página de gestión de usuarios del lado cliente ni cambio de contraseña/MFA: **MFA pendiente** (queda como tarea propia dentro de la fase 1, ver DUDAS).
- La comprobación de contraste AA la he hecho a ojo sobre los tokens, no con herramienta.

## Huecos encontrados

- Faltan `05-paginas-y-endpoints.md`, `08-ux-ui/pantallas.md` y `08-ux-ui/flujos-usuario.md`: el inventario de pantallas de esta fase es el que aparece en la navegación lateral. Lo describo aquí para que el arquitecto lo contraste.
- `12-infraestructura-despliegue.md` no fija el subdominio: `server_name` queda como `aserta-dev.CAMBIAR-DOMINIO`.
- `CLAUDE.md` tiene la estructura "pendiente de definir": la estructura real es la de ADR-001 §2.1; no lo he editado por ser documento del arquitecto.

## Cómo probarlo

```bash
cd ~/proyectos/Aserta && dotnet run --project src/Aserta.Web     # http://127.0.0.1:5110
```

1. Login `socio@demo.aserta.local` / `Aserta-Demo-2026!`. Cambiar tema con el botón de la cabecera.
2. **Clientes**: buscar, filtrar por asesor; entrar en "Panadería López": KPIs, próximos vencimientos con semáforo, pestañas.
3. **Nuevo cliente** con NIF incorrecto → mensaje con la letra esperada; con territorio foral → bloqueo; correcto → ficha con obligaciones generadas.
4. **Obligaciones**: filtrar por modelo `303`; abrir una; "Mover a…" Documentación completa → En preparación → Revisión interna → Pendiente aprobación cliente; intentar Presentado → error RD-09; "Registrar aprobación" → Presentado → Cerrado; el historial completo queda abajo.
5. **Configuración** (socio): desactivar "exigir aprobación" y comprobar que se puede presentar sin aprobación; desactivar "asesores ven todos" y entrar como `carlos@…`: solo ve sus clientes.
6. **Auditoría**: cada acción anterior con usuario, IP y detalle JSON. **Usuarios**: crear, bloquear (el bloqueado no puede entrar).
7. Entrar como `pedro@…` (administrativo) y comprobar que no ve Usuarios ni Configuración y no puede marcar Presentado.

## Estado de los tests

- Web cubierta por `SeguridadWebTests` (redirección sin sesión, CSP, antiforgery, salud) y por la prueba manual con curl de todas las páginas (todas 200, sin errores en log, < 500 ms).
- No cubierto: flujos de página con sesión (login + navegación) en tests automatizados; accesibilidad con lector de pantalla; el JS (`app.js`) no tiene tests.
