# 08 · Fotografía, vídeo animado del logo y riqueza visual (encargo 02)

> Continuación del encargo inicial. Capturas de antes y después en `capturas/`: `antes-web-inicio-escritorio.png`, `antes-web-inicio-movil.png`, `despues-web-inicio-escritorio.png`, `despues-web-inicio-movil.png`, y las nuevas cabeceras `despues-web-servicios-escritorio.png`, `despues-web-como-escritorio.png`, `despues-web-como-movil.png`, `despues-web-equipo-escritorio.png`.

## Qué hice

**G1 · Las tres fotos de Javier**, tratadas y colocadas donde tienen sentido:
- **Mesa de trabajo** (azul/naranja): fondo a sangre de la portada y cabecera de «Equipo». Recorte 16:9 por la parte alta (la agenda con «SEMANA» y los días en portugués queda fuera o bajo el velo).
- **Calculadora sobre carpeta**: figura con pie en la sección de perfiles («Sobre tus libros, no sobre estimaciones») y cabecera de «Servicios». El papel con texto en ruso y el sello se difuminan con dos zonas de desenfoque gaussiano antes del recorte; no queda ningún texto legible.
- **Firma**: banda de cierre de la portada («¿Hablamos de tu caso?») y cabecera de «Cómo trabajamos».
- Unificación con la paleta: ligera desaturación (82 %) en el fichero y velos de marca en CSS (`velo-morado`, `velo-lateral`, `velo-oscuro` y la veladura morado/verde de las figuras). Texto siempre en blanco sobre el velo, con contraste suficiente.
- Optimización: WebP en 480/960/1600/2400 px (apaisadas) y 480/960 (verticales para móvil), con `srcset`/`sizes` y `<picture>`; marcador de carga de 24 px como fondo (`fotos.css`, generado). Carga diferida salvo en el hero.

**G2 · Vídeo animado del logo**
- `marca/sga-logo-animado.svg` (y `-oscuro`): el logo vectorial con animación CSS incrustada, sin JavaScript. Secuencia de 7 s en bucle: se traza el marco, luego las diagonales, aparecen las solapas, brota el centro de la flor y los **seis pétalos uno a uno**, el amarillo del pistilo, se dibuja el tallo, brota la hoja, se **traza «SGA»** en verde y luego se rellena, y aparecen «Contabilizado», «S.A.» y «S.L.»; fundido y vuelta a empezar.
- El estado base es el logo completo: las animaciones solo se aplican bajo `prefers-reduced-motion: no-preference`. Si el usuario pide menos movimiento, o el CSS no se aplica, se ve el logo entero. En la portada va **en línea** (no como imagen) para que esa preferencia la decida el navegador del visitante.
- Exportado a vídeo con Playwright (`recordVideo`) y recortado y comprimido con el ffmpeg de Playwright: `wwwroot/img/sga-logo-animado.webm`, 1280×720, 7 s, VP8, sin audio. No hay codificador H.264 en ese ffmpeg, así que **no hay `.mp4`**; el `.webm` lo reproducen Chrome, Firefox, Edge y Safari 16+.

**G3 · Riqueza visual**: fotografía a sangre en portada, perfiles y cierre; texto sobre foto con velo; aparición suave al hacer scroll (`IntersectionObserver`, solo si hay JavaScript y el usuario no pide menos movimiento); tipografía y componentes del sistema existente.

**G4 · Composición de la referencia** (clientesmarchante.winsoft.es, traducida a SGA):
1. Hero a sangre con el titular superpuesto arriba a la izquierda (acento en cursiva amarilla: «*Y tú sabiendo cuánto vas a pagar*»), la maqueta del móvil a la derecha y una **barra de acción** abajo a la derecha (teléfono y horario, «Pide tu primera reunión», «Área de clientes»).
2. **Franja de cifras** bajo el hero (48 h, 21/44 modelos presentados, 15 años, 14 clientes) con el pie «cifras ilustrativas de la demostración, no reales ni verificadas».
3. **Tarjetas 01 / 02 / 03** para los perfiles, con icono, ficha técnica (IVA 21 % + retención 19 %; 303 y a algunos 130; 202 y libro de IVA) y flecha al servicio.
4. Acento tipográfico: ya existía (Fraunces cursiva en el titular); se mantiene.
5. **Barra fija inferior en móvil** en la web pública: Llamar, WhatsApp, Área de clientes.
6. **Vídeo incrustado**: `sga-como-trabajamos.webm`, 33 s (7 s por escena, para que dé tiempo a leer cada rótulo; la primera versión duraba 18,6 s y Javier pidió el doble), mudo, con las tres fotos en Ken Burns suave, los cuatro pasos del trimestre como rótulos y el logo animado al final. Con controles y póster, `preload="none"` (no se descarga hasta que se pulsa). Va en la portada («Cuatro momentos») y en «Cómo trabajamos».

## Peso de los assets nuevos
| Asset | Peso |
|---|---|
| 18 WebP de las tres fotos (todas las variantes) | 656 KB en total; la portada carga uno de 23–62 KB según el ancho (174 KB solo en pantallas de 2400 px) |
| `sga-logo-animado.svg` / `-oscuro.svg` | 38 KB cada uno (textos como trazados) |
| `sga-logo-animado.webm` | 240 KB (7 s, fondo morado-900) |
| `sga-como-trabajamos.webm` | 599 KB (33 s); póster 24 KB |
| `fotos.css` (marcadores de carga) | 6 KB |

El vídeo de 599 KB supera «algunos cientos de KB»; solo se descarga si alguien pulsa reproducir. Si molesta en el repositorio, se puede quitar la escena 04 (unos 130 KB menos) o dejarlo fuera de git y generarlo con el script. Desde el 2026-10-01 el vídeo se genera fotograma a fotograma (825 capturas con el tiempo de animación fijado por la API de animaciones) en vez de grabar la pantalla: la grabación en tiempo real perdía fotogramas y desfasaba los rótulos.

## Cómo se generó (fuera del repositorio, en `~/aserta-scratch/`)
- `fotos/`: herramienta .NET con ImageSharp (recorte por proporción, desenfoque por zonas, desaturación, WebP por anchos, marcador). 
- `logo/animar_logo.py`: construye el SVG animado a partir de las mismas medidas y trazados del logo.
- `grabar/`: Playwright que graba una página mínima (el SVG o el `kenburns.html`) y ffmpeg de Playwright para recortar y comprimir.

## Pruebas y comprobaciones
- Las 6 pruebas de `Sga.Tests` y las de Aserta siguen en verde (no se ha tocado dominio ni datos).
- Fotogramas de los dos vídeos revisados uno a uno; el SVG revisado en ocho instantes de la animación (pétalos, textos, fundido) y con la animación bloqueada (logo completo).
- Capturas en 390×844 y 1440×900 de portada, servicios, cómo trabajamos y equipo; sin desbordes horizontales.
- El vídeo del logo y el explicativo no bloquean la página: si no cargan, se ve el logo estático y los cuatro pasos en texto.

## Incidencia de despliegue (importante)
Javier había instalado el servicio `sga-demo` (systemd) y publicado en `SGA/publicado`. Al reiniciar mi instancia de desarrollo, un `pkill` demasiado amplio **paró ese servicio** (20:08). No puedo arrancarlo sin sudo. He publicado la versión nueva en `SGA/publicado` y la he arrancado a mano en el puerto 5120 con la misma configuración del servicio, así que la demo sigue disponible. Para volver al servicio: `pkill -f publicado/Sga.Web.dll && sudo systemctl start sga-demo`. Mi instancia de desarrollo ahora usa el puerto 5121 para no volver a chocar.

## Qué queda o es dudoso
- Las fotos son de stock (Pexels) y así se presentan: no se describen como oficina ni equipo de SGA.
- Sin `.mp4` (no hay H.264 en el ffmpeg disponible); si hiciera falta, se genera en otra máquina desde el `.webm` o desde `kenburns.html`.
- Las cifras de la franja son ilustrativas y lo dicen; SGA debe sustituirlas o quitarlas.
- El vídeo explicativo no tiene voz ni subtítulos: sus rótulos son el texto. Si SGA quiere locución, hace falta grabarla.
- Ajuste posterior si Javier quiere afinar algún patrón concreto de la referencia (ya vista con capturas por el arquitecto).
