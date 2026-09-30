# 02 · Marca (fase 2)

## Qué hice
Rehíce el logo de SGA en vector a partir de la foto (que está girada 90°). Medí las proporciones sobre la foto y las trasladé a un rectángulo de 2,6:1. Los textos van convertidos a trazados (no dependen de fuentes instaladas). Ficheros en `marca/` y copiados a `src/Sga.Web/wwwroot/img/`:

| Fichero | Uso |
|---|---|
| `sga-logo.svg` | completo, en color, sobre claro |
| `sga-logo-oscuro.svg` | completo sobre fondo oscuro (pie de la web) |
| `sga-logo-mono.svg` | una tinta (sellos, fax, impresión en negro) |
| `sga-compacto.svg`, `-oscuro`, `-mono` | flor + «SGA» (cabeceras, lateral) |
| `sga-flor.svg` | solo la flor (cabecera móvil) |
| `favicon.svg` | flor sobre morado oscuro redondeado, legible a 16 px |

## Qué cambié respecto al original y por qué
- **Mismo concepto y mismos elementos**: rectángulo con dos solapas triangulares («S.A.» a la izquierda, «S.L.» a la derecha), «Contabilizado» en cursiva entre ellas, la flor de seis puntas morada con el centro amarillo, tallo y hoja verdes, y «SGA» en verde abajo a la izquierda. Se lee como un sobre o un documento con solapas: encaja con «contabilizado».
- **Geometría limpia:** las diagonales van de vértice a vértice y las dos solapas son simétricas; en el original están a mano alzada. La flor es dos triángulos equiláteros con uniones redondeadas; el tallo, una curva suave; la hoja, una elipse.
- **Color con criterio:** morado #7B3F8E (flor), morado oscuro #4A2660 (líneas y textos), verde #2F9E5B (tallo y «SGA»), verde-hoja #A9C23A, amarillo #F6D33C. Son los del original, ajustados para cumplir contraste sobre blanco. Las solapas llevan un tinte morado muy suave para dar profundidad (se quita en la versión mono y oscura).
- **Tipografía:** «Contabilizado» y «SGA» en Fraunces cursiva (la misma serif de la web), que conserva el aire manuscrito del original sin ser una fuente «de firma»; «S.A.» y «S.L.» en Inter semibold, pequeñas y limpias.
- **Versiones:** el original solo existe en color sobre blanco; añado mono, oscuro, compacto y favicon, que es lo que una web y una app necesitan.

## Guía breve de uso
- Zona de protección: la mitad de la altura de la flor alrededor del logo.
- Tamaño mínimo: 120 px de ancho para el completo, 24 px para la flor.
- Sobre fondos oscuros, usar siempre la versión «oscuro»; sobre fotografías, la mono en blanco.
- No cambiar los colores ni separar «Contabilizado» de las solapas; para espacios pequeños está el compacto.

## Cómo se generó
`~/aserta-scratch/logo/construir_logo.py` (fuera del repositorio) con una utilidad .NET que convierte texto a trazados (SixLabors.Fonts). Se puede regenerar cambiando medidas o colores.
