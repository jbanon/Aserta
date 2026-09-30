# Encargo 02: fotografía, vídeo animado del logo y más riqueza visual

> Continuación sobre lo ya construido en `SGA/`. No es un proyecto nuevo: es un encargo corto sobre la web pública y, donde encaje, el área de clientes.

## Por qué

Javier ha visto la demo y dice que "está bien" pero le falta riqueza visual: fotografía real y algo de vídeo, no solo iconos y bloques de color. Puso como referencia de estilo (no de contenido) una web que hicieron para un cliente de carpintería, más rica visualmente. **Ese enlace o esas capturas todavía no las tengo.** No esperes a que lleguen: aplica el criterio general de este encargo, y si luego llega la referencia, será un ajuste posterior, no un rediseño.

## G1. Usa las tres fotos que ha subido Javier

En `SGA/Recursos/Fotos/` (no se sube a git, igual que el resto de `SGA/Recursos/`):

- `pexels-cristian-rojas-10041249.jpg` — mano firmando un documento con pluma, traje oscuro, fondo neutro. Vertical, 2648×3965.
- `pexels-mikhail-nilov-8297030.jpg` — mano con calculadora sobre una carpeta de documentos sellados. Vertical, 4000×6000. **Ojo:** los documentos de fondo están en ruso y tienen texto legible de otro país; recórtalas o difumínalas para que ese texto no se lea (usa un `object-position` que lo saque de encuadre, o aplica desenfoque/superposición de color).
- `pexels-japy-9283595.jpg` — mesa de trabajo en tono azul/naranja, teclado, agenda abierta en "SEMANA". Vertical, 3375×6000.

Son fotos de stock (Pexels, licencia libre), no son la oficina real de SGA. Tienen que dar ambiente profesional, no hacerse pasar por fotos del equipo. No muestran caras ni marcas de terceros, así que no hay problema de imagen de nadie, pero no las titules ni las describas como "nuestro equipo" o "nuestra oficina".

**Trátalas para que no parezcan banco de imágenes suelto:**
- Recórtalas o dales una veladura con los colores de la marca (morado/verde/amarillo del logo) para unificarlas con la paleta, no las dejes con sus colores originales sin tratar.
- Genera variantes optimizadas para web: WebP, varios anchos (por ejemplo 480/960/1600/2400 px) con `srcset`, y una versión de baja resolución o dominante de color como marcador de carga. Los originales pesan hasta 2,5 MB; no se sirven así.
- Colócalas donde tengan sentido narrativo, no de relleno: la de la firma en la sección de "cómo trabajamos" o en el cierre de contacto; la de la calculadora en la sección de perfiles fiscales o de presentación de impuestos; la de la mesa en la portada o en "quiénes somos".

## G2. Vídeo animado del logo

Quiere un vídeo corto y animado con el logo, para la portada (y valora si encaja también al abrir alguna sección). No es una grabación real: es una **animación del logo vectorial que ya construiste** (el rectángulo con diagonales, la flor de seis puntas abriéndose pétalo a pétalo, el amarillo del centro apareciendo, y "SGA" trazándose en verde), de unos 3-4 segundos, en bucle suave.

Constructívelo así:
1. **La animación vive primero como SVG/CSS** (o un pequeño fragmento JS sin librerías pesadas), reutilizando el SVG del logo que ya existe en `SGA/marca/`. Es la versión que se ve en el navegador: ligera, nítida a cualquier tamaño, y que respeta `prefers-reduced-motion` (si el usuario lo pide, se ve el logo estático, sin animar).
2. **Además, exporta esa misma animación como fichero de vídeo** (`.webm`, y si quieres `.mp4`) para poder usarla como fondo de vídeo en la portada si el diseño lo pide. Tienes Playwright disponible, con su binario de ffmpeg ya instalado en `~/.cache/ms-playwright/ffmpeg-1011/ffmpeg-linux`: graba la animación con `context.recordVideo` (o captura de fotogramas y compón con ese ffmpeg) sobre una página mínima que solo contenga el SVG animado. No necesitas instalar nada con sudo.
3. El vídeo exportado va como asset estático del proyecto (pésalo: recórtalo a los segundos justos, sin audio, y comprime). Si pesa demasiado para tenerlo cómodo en el repositorio, dilo en tu informe y decide un tamaño razonable (algunos cientos de KB, no varios MB).
4. En la portada, el vídeo o la animación SVG en directo (decide cuál rinde mejor) no debe tapar el contenido ni obligar a esperar: la página tiene que verse bien aunque la animación no cargue o el navegador la bloquee.

## G3. Criterio general de "más riqueza visual" (mientras llega la referencia de Javier)

No copies el aspecto de ninguna plantilla de gestoría. Aplica esto:
- Fotografía grande, a sangre, en al menos la portada y una sección de cierre (contacto o "cómo trabajamos"), no solo iconos pequeños repetidos.
- Composición con capas: texto sobre foto con buen contraste (overlay, no solo foto detrás de una caja blanca), en vez de foto y texto siempre en columnas separadas.
- Algo de movimiento sutil al hacer scroll o al cargar (aparición suave, no rebotes ni parallax agresivo), coherente con `prefers-reduced-motion`.
- Mantén la tipografía y los componentes ya definidos en `SGA/docs/sistema-diseno.md`; esto es vestir la web que ya existe, no rehacer el sistema de diseño.

## Reglas (las de siempre)

- Datos y personas siempre ficticios; estas fotos de stock no cambian esa regla en ningún otro sitio de la demo.
- No rompas las pruebas existentes de SGA ni de Aserta.
- `SGA/Recursos/` no se sube a git; los assets finales (SVG, vídeo, imágenes optimizadas) sí van en el proyecto, en la carpeta de estáticos que ya uses (`wwwroot` o equivalente).
- Informe corto al terminar en `SGA/informes/`, con capturas de antes/después de la portada en móvil y escritorio, y el peso final de los assets nuevos.
- Si algo de esto no es viable en un tiempo razonable (por ejemplo, la grabación de vídeo con Playwright falla en este servidor), dilo en el informe con lo que sí hiciste (la animación SVG/CSS en directo debe quedar siempre, es la base) y sigue.

## G4. Referencia de estilo: clientesmarchante.winsoft.es (añadido tras ver la web)

Javier ha enseñado https://clientesmarchante.winsoft.es/ (un fabricante de ventanas, otro cliente) como ejemplo del nivel de riqueza visual que busca. **No copies su contenido, su rojo ni su temática de ventanas.** Tradúcelo a SGA, con la paleta morado/verde/amarillo y la voz de una gestoría, así:

1. **Hero a sangre con texto superpuesto.** Ahora mismo la portada probablemente separa foto y texto en bloques. Pásalo a una foto grande de las tres de `SGA/Recursos/Fotos/` (o una nueva combinación con el vídeo del logo) ocupando todo el ancho, con el titular superpuesto en la esquina superior y buen contraste (overlay), y una barra de acción abajo a la derecha (en vez de un teléfono, puede ser el acceso al área de clientes o "Pide tu primera reunión").
2. **Franja de cifras justo debajo del hero.** Números grandes con su etiqueta arriba o abajo, separados por líneas finas. Para SGA, usa cifras de ejemplo **marcadas explícitamente como ilustrativas** (nunca reales ni verificadas): por ejemplo plazo medio de respuesta, modelos presentados este trimestre, años de actividad, clientes activos. Dilo en el pie de la franja, como ya haces con `[VERIFICAR]` en otros sitios.
3. **Tarjetas numeradas 01-02-03** para los tres perfiles de cliente (arrendador, profesional, sociedad) en vez de sus cinco sistemas de ventana: un icono o ilustración simple, el nombre del perfil, una ficha corta con el dato técnico que ya tienes (IVA 21 % + retención 19 %; 303 trimestral y 130 a algunos; modelo 202 y libro de IVA), y una flecha al detalle del servicio.
4. **Un acento tipográfico** (una palabra en cursiva o en una serifa dentro del titular) si encaja con el sistema de diseño que ya definiste; si no aporta o choca con lo que ya hay, no lo fuerces.
5. **Barra fija inferior en móvil** con acciones directas: para el área de cliente ya tiene sentido (subir documento, ver impuestos, mensajes); para la web pública, algo como "Llamar / WhatsApp / Acceso clientes".
6. **Vídeo incrustado**, no solo de fondo: además de la animación del logo del punto G2, valora un vídeo corto explicativo en la portada o en la sección "cómo trabajamos" (puede ser la misma animación con voz en off simulada por texto, o un vídeo mudo de las fotos con Ken Burns suave). No hace falta alojarlo fuera; sirve desde el propio proyecto.

Puedes entrar tú mismo en https://clientesmarchante.winsoft.es/ para verlo con tus propias herramientas (capturas con el chrome-headless-shell que ya usas) si quieres más detalle de algún patrón concreto.
