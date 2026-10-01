# Sistema de diseño · SGA Contabilizado

## La idea
Un despacho, no una startup. Sobrio y cálido: papel crema, morado profundo como color de autoridad, verde como único color de «hecho», amarillo solo para «te falta algo». Una serif con carácter para los titulares (dice «despacho de siempre») y una sans neutra con dígitos tabulares para todo lo demás (dice «cifras claras»).

## Color (sale del logo)
| Token | Valor | Uso |
|---|---|---|
| `--morado-900` | #4A3470 | cabeceras oscuras, pie, fondo del vídeo (el lateral del área es claro desde el 2026-10-01) |
| `--morado-700` | #644A96 | texto de marca, botón «marca», enlaces |
| `--morado-500` | #8A5BB5 | acentos, foco, velos sobre foto |
| `--morado-100` | #F3ECF7 | fondos suaves, celdas de agrupación |
| `--verde-500` | #2F9E5B | **acción principal** y estado «presentado / completo» |
| `--verde-700` | #1F6E40 | texto sobre verde claro |
| `--amarillo-500` | #F6D33C | «en curso», «te falta», subrayados; nunca fondo grande |
| `--amarillo-700` | #8A6100 | texto de aviso sobre claro |
| `--rojo-500` | #C8412B | vencido, error |
| `--papel` | #FBFAF7 | fondo de página |
| `--tinta` / `--tinta-2` / `--tinta-3` | #221B2A / #5B5366 / #8B8494 | texto principal, secundario, terciario |

Contraste: todos los pares texto/fondo usados cumplen AA (los de texto pequeño, ≥ 4,5:1: tinta sobre papel 14,9:1; morado-700 sobre blanco 7,1:1; verde-700 sobre verde-100 6,1:1; amarillo-700 sobre amarillo-100 5,6:1; blanco sobre verde-500 4,6:1 en botones con texto en semibold).

Estados de la matriz (los del Excel, con criterio): presentado = verde suave; en curso = amarillo suave con las iniciales de la persona; pendiente = blanco; NP = gris; trimestre futuro = rayado.

## Tipografía
- **Fraunces** (variable, OFL) para titulares y cifras grandes. Eje `opsz` alto en el hero (más contraste), bajo en tarjetas. Cursiva para el «Contabilizado» del logo y las citas.
- **Inter** (variable, OFL) para interfaz y texto, con `tnum` (dígitos tabulares) activado en todo el sitio para que las columnas de importes se alineen.
- Escala: 12 / 14 / 16 / 18 / 22 / 28 / 36 / 48 / 64 px. Interlineado 1,5 en texto, 1,1 en titulares.

## Espacio, radios, sombras
Base de 4 px (`--e-1` a `--e-9`). Radios 6 / 12 / 20 / 28 px y píldora. Tres sombras, muy suaves. Transiciones de 160 ms.

## Componentes (en `sga.css`)
Botones (primario verde, marca morado, secundario, sutil, peligro; 44 px mínimo tocable) · Tarjetas (normal, marca, suave) · Insignias de estado con punto · Avatares con iniciales · KPI (cifra en Fraunces) · **Anillo de progreso** (SVG) · **Cascada de IVA** (barras) · **Línea de tiempo del trimestre** · Requisito documental (completo / falta) · Listas enlazadas (56 px por fila) · Miniaturas de documento · Matriz (tabla con cabecera y primera columna fijas) · Formularios (44 px, foco visible) · Zona de subida con cámara · Alertas · Estados vacíos · Burbujas de mensajes · Calendario mensual · Pestañas y píldoras de filtro.

## Iconos
Sprite propio `img/iconos.svg`: 46 símbolos de 24 px, trazo 1,75, remates redondos, sin relleno. Una sola familia en todo el sitio.

## Layouts
- **Web pública:** cabecera fija translúcida, contenedor de 1180 px, secciones alternando papel / blanco / morado oscuro, pie oscuro con dirección y horario.
- **Área (cliente y gestor), móvil (< 900 px):** cabecera de 60 px con flor y título, contenido a una columna, **navegación inferior fija de 5 posiciones** con la acción central elevada (cámara para el cliente, clientes para el gestor).
- **Área, escritorio (≥ 900 px):** lateral morado de 264 px, plegable a 76 px (se recuerda), cabecera con plegado, contenido hasta 1280 px a dos columnas.

## Voz
Tú, siempre. Frases cortas con verbo. Cada modelo va con su traducción entre paréntesis la primera vez («el IVA (modelo 303)»). Nunca «sin complicaciones», nunca emojis, nunca exclamaciones.
