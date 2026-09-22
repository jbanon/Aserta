# ADR-003 · Interactividad en Razor Pages y estrategia de CSS

- **Estado:** Aceptada
- **Fecha:** 2026-09-22
- **Decide:** Agente Arquitecto
- **Afecta a:** todo el front (M2, M3, M4, M6, M7), `08-ux-ui/`, despliegue
- **Relacionado:** [DA-09](../decisiones-abiertas.md), [08-ux-ui/design-system.md](../08-ux-ui/design-system.md)

---

## 1. Contexto

El stack fija **ASP.NET Core Razor Pages** con renderizado en servidor. Hace falta decidir cómo se consigue la interactividad que la interfaz necesita, y con qué se escriben los estilos. Las exigencias son contradictorias entre sí y conviene ponerlas encima de la mesa:

| Exigencia | De dónde viene |
|---|---|
| Kanban con **arrastrar y soltar** | M4, y es el momento más vistoso de la demo |
| **Actualizaciones parciales** sin recargar página (mover tarjeta, validar documento, subir fichero, refrescar cola de envíos) | M3, M4, M6 |
| **Densidad alta** y manejo con teclado en el lado gestoría | Criterios de calidad §8: el gestor vive en la pantalla ocho horas |
| **Simplicidad extrema y móvil primero** en el lado cliente | M2 |
| **Modo claro y oscuro** y **accesibilidad AA** | Criterios de calidad §8 |
| **Sin cadena de build de Node en el servidor** salvo justificación | Restricción de infraestructura |
| RAM escasa, una sola instancia | Restricción de infraestructura |

La última fila mata por sí sola a media industria del front.

## 2. Decisión

### 2.1 Interactividad: **htmx 2.x + SortableJS**, servidos localmente

- **htmx** para toda actualización parcial: el navegador pide, el servidor devuelve **un fragmento de HTML** (una Razor *Partial*), htmx lo inserta. No hay estado de interfaz duplicado en el cliente, no hay JSON que mantener, no hay modelo de vista en dos sitios.
- **SortableJS** exclusivamente para el arrastrar y soltar del Kanban.
- **Nada más.** Sin jQuery, sin Alpine, sin bundler, sin `node_modules`. Los dos ficheros se descargan una vez, se versionan **dentro del repositorio** en `wwwroot/lib/` y se sirven desde nuestro dominio.

> **Por qué servidos localmente y no desde un CDN:** cero dependencias externas en tiempo de ejecución (un CDN caído tira la demo), política de seguridad de contenidos más estricta, y ninguna petición del navegador de un usuario a un tercero — que en una plataforma con datos fiscales de terceros no es una manía, es RGPD.

### 2.2 Estilos: **CSS propio con design tokens**, sin framework

- Variables CSS en `:root` como única fuente de verdad del design system (color, espaciado, tipografía, radios, sombras, duraciones).
- Organización con `@layer reset, tokens, base, componentes, utilidades` para que la cascada sea predecible sin guerras de especificidad.
- **Modo oscuro** por `prefers-color-scheme` **más** un `data-tema="claro|oscuro"` en `<html>` que permite al usuario forzarlo; los componentes no saben en qué modo están, solo leen tokens.
- Tipografía **autoalojada** (fuente variable, subconjunto latino) — no Google Fonts, por la misma razón que el CDN.
- Sin minificación en build: los ficheros se sirven con compresión de nginx y `asp-append-version="true"` para invalidar caché. Para el tamaño de este proyecto, un paso de build para ahorrar 8 KB no se paga.

### 2.3 Convenciones de implementación *(esto es lo que el Agente Programador necesita)*

**Fragmentos.** Cada trozo de interfaz actualizable vive en una *Partial* reutilizada por la carga completa y por la actualización parcial. Un handler nombrado devuelve `Partial(...)` cuando la petición viene de htmx:

```
OnGetTarjetaAsync(id)        → Partial("_TarjetaObligacion", vm)
OnPostMoverTarjetaAsync(...) → Partial("_ColumnaKanban", vm)   // se repinta la columna, no la página
```

Detección: cabecera `HX-Request`. Una petición normal devuelve la página entera; **las páginas siguen funcionando sin JavaScript en los flujos críticos** (subida de documento, aprobación de borrador, emisión de factura). Es a la vez accesibilidad y red de seguridad.

**Antiforgery.** Obligatorio en todos los POST. Se configura **una vez** de forma global:

```js
document.body.addEventListener('htmx:configRequest', e => {
  e.detail.headers['RequestVerificationToken'] = tokenDelFormularioOculto;
});
```

con `AddAntiforgery(o => o.HeaderName = "RequestVerificationToken")`. Sin esto, cada `hx-post` es una omisión esperando a ocurrir.

**Política de seguridad de contenidos.** Se sirve una CSP **sin `unsafe-inline` ni `unsafe-eval`**. Consecuencia práctica: **prohibido usar `hx-on:` y los filtros de eventos de htmx**, que evalúan cadenas; se configura `htmx.config.allowEval = false` y los comportamientos se enganchan desde `app.js` con `addEventListener`. Los estilos en línea también quedan prohibidos: las barras de progreso y los colores de estado se hacen con clases o con propiedades personalizadas asignadas desde JS, no con `style="..."`.

**Kanban.** SortableJS emite el movimiento, se hace `fetch`/`hx-post` al handler, y **el servidor valida la transición** contra la máquina de estados (`01-mapa-dominio.md` §5.1). Si la rechaza, la tarjeta vuelve a su sitio con un mensaje. La interfaz es optimista; **la verdad está en el servidor**, siempre.

**Arrastrar no puede ser la única forma de mover una tarjeta.** Es un requisito de accesibilidad AA y, además, de productividad: cada tarjeta tiene un menú *"Mover a…"* accesible por teclado, y la vista de lista permite cambiar el estado de varias tarjetas a la vez. En un despacho, en día 18 de abril, el menú se usará más que el arrastre.

**Indicadores.** `hx-indicator` para el estado de carga y una región `aria-live="polite"` donde se anuncian los resultados de las acciones, para que un lector de pantalla se entere de que la tarjeta se movió.

## 3. Opciones consideradas

| Opción | Valoración |
|---|---|
| **Blazor Server** | Descartada. Fuera del stack fijado y, sobre todo, mantiene un circuito con estado por usuario conectado: con 3,8 GB compartidos es un riesgo, y una desconexión de red en medio de una demo deja la pantalla muerta |
| **SPA (React/Vue) + API JSON** | Descartada. Duplica el modelo, exige Node en build, multiplica el trabajo y no aporta nada que Razor Pages + htmx no dé aquí. Además obligaría a diseñar y versionar una API pública que hoy nadie necesita |
| **Alpine.js** *(en vez de o junto a htmx)* | Buena para estado local de interfaz (desplegables, pestañas, modales). Se descarta **por ahora** para no tener dos modelos mentales; esos casos se resuelven con `<details>`, `<dialog>` y ~100 líneas propias. Reabrir si aparece mucha interacción puramente cliente |
| **`fetch` a pelo, sin librería** | Viable, pero acaba siendo htmx peor hecho: reimplementar objetivo, intercambio, indicadores, historial y gestión de errores cuesta más que 14 KB |
| **Tailwind vía CLI standalone** | Es la alternativa seria. **Descartada** porque: (a) añade un binario de ~50 MB y un paso de build que hay que ejecutar en algún sitio; (b) el valor de Tailwind está en equipos grandes con muchos criterios, y aquí hay un diseñador único (el arquitecto) y un consumidor único (el programador); (c) el design system que exige el producto —densidad alta, modo oscuro, accesibilidad AA— se expresa mejor como **tokens semánticos** (`--color-estado-vencido`) que como utilidades. **Cuándo reabrir:** si entra un equipo de front o si el CSS propio pasa de ~2.500 líneas |
| **Bootstrap** | Descartada. Trae una estética reconocible de "plantilla de administración" que resta credibilidad justo en el sector donde se vende confianza, y pesa más de lo que aporta |

## 4. Consecuencias

### Positivas
- Despliegue = copiar ficheros. No hay build de front, no hay `node_modules`, no hay versiones de Node que cuadrar en el servidor.
- El programador escribe C# y HTML; no cambia de lenguaje ni de modelo mental para hacer una pantalla interactiva.
- Control total del aspecto: el producto no parece una plantilla.
- Sin estado de interfaz duplicado: una clase entera de bugs desaparece.
- Consumo del navegador del usuario ridículo; funciona bien en el móvil viejo del cliente de la gestoría, que es exactamente el dispositivo de M2.

### Negativas y cómo se mitigan
| Coste | Mitigación |
|---|---|
| Interacciones muy ricas (arrastre multi-selección, edición en línea compleja) son más trabajosas que en una SPA | Ninguna del alcance lo es. Si aparece, se aísla en una isla de JS propio |
| El CSS propio hay que escribirlo y mantenerlo | Se entrega como parte del diseño (`08-ux-ui/design-system.md`), con los componentes cerrados **antes** de programar. El programador no inventa estilos |
| Cada acción va al servidor: sin conexión no hay interfaz | Aceptable: es una herramienta de despacho conectada. Se cuidan los estados de error y reintento en el portal móvil, donde la cobertura sí falla |
| htmx y SortableJS se actualizan a mano | Fichero con versión en el nombre (`htmx-2.x.x.min.js`), versión anotada en `12-infraestructura-despliegue.md` y revisión en cada release |
| Sin `unsafe-eval` se pierden azúcares de htmx | Es una restricción buena: obliga a tener el JavaScript en un sitio y no repartido por los atributos de las vistas |

## 5. Cómo se verifica

1. `wwwroot/lib/` contiene **solo** htmx y SortableJS, con versión en el nombre; no existe `package.json` en el repositorio.
2. La respuesta HTTP incluye CSP sin `unsafe-inline` ni `unsafe-eval`, y la consola del navegador no muestra violaciones al recorrer el guion de demo completo.
3. Todo `hx-post`/`hx-delete` viaja con la cabecera antiforgery *(test de integración: un POST sin cabecera devuelve 400)*.
4. El Kanban es operable **solo con teclado** de principio a fin.
5. Contraste AA verificado en modo claro y oscuro sobre la paleta de `design-system.md`.
6. Una transición inválida rechazada por el servidor devuelve la tarjeta a su columna con mensaje visible y anunciado.

## 6. Riesgos y mejoras sugeridas

- **Riesgo:** el arrastrar y soltar en móvil con SortableJS requiere cuidado (desplazamiento vs. arrastre). **Mitigación:** el Kanban es una vista **de escritorio**; en móvil, el gestor ve lista con menú *"Mover a…"*. El cliente (M2) nunca ve el Kanban.
- **Riesgo:** repintar la columna entera en cada movimiento es simple pero pesado si una columna tiene 300 tarjetas en abril. **Mitigación:** paginar/virtualizar por columna y repintar solo la tarjeta y los contadores. Medir antes de complicar.
- **Mejora:** atajos de teclado en el lado gestoría (`j`/`k` para recorrer, `m` para mover, `/` para buscar). Barato con este enfoque y es de las cosas que hacen que un usuario intensivo se enamore de una herramienta.
- **Mejora:** una página `/estilo` que renderice **todos** los componentes del design system en ambos modos. Sirve de referencia al programador, de test visual y, en la demo, demuestra madurez de producto.
- **Mejora:** `hx-boost` en la navegación general para que el cambio de página no parpadee, con degradación limpia si falla.
