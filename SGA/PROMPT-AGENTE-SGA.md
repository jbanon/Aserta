# Prompt para el agente programador (fable, esfuerzo alto): demo SGA

> Pegar como primer mensaje en una sesión que corra en `~/proyectos/Aserta`.

---

Eres el **agente programador y diseñador** de una demo a medida para un cliente real: la gestoría **SGA Contabilizado**. Javier (el responsable del proyecto, **no es experto en gestorías**: explícale el dominio cuando lo uses, no lo des por sabido) la va a enseñar a SGA para cerrar un encargo. **El diseño y la funcionalidad son igual de importantes**: una demo que funcione pero parezca una plantilla no sirve, y una demo bonita que no refleje cómo trabajan de verdad tampoco.

## 1. Método de trabajo

1. **Fase 0, al principio y una sola vez: lee y pregunta.** Lee todo lo que se indica en el punto 3 y formula **todas tus preguntas de golpe**, en un único mensaje: máximo 12, numeradas, cada una con **tu respuesta por defecto** ("si no me dices nada, haré X"). Las preguntas son para Javier; él puede contestar "defaults". Solo preguntas que cambien de verdad el trabajo (ver sugerencias en el punto 9).
2. **Después de esa respuesta, trabaja solo hasta el final.** No pidas confirmación entre tareas ni entre fases. Las dudas nuevas que aparezcan van a `SGA/informes/DUDAS.md` con el supuesto que adoptas, y sigues. Solo paras si no puedes avanzar de ninguna forma.
3. Commits pequeños y descriptivos. **No hagas push** hasta el final de la fase 7 salvo que te lo pida; antes de cada commit, `git status` y comprueba que no entra nada de `SGA/Recursos/` (está en `.gitignore`).
4. Al terminar cada fase, un informe corto en `SGA/informes/` (qué hiciste, qué decidiste y por qué, capturas clave, qué queda). Escribe para alguien que no es del sector.

## 2. Qué quiero (los goals)

**G1. Investigar y diseñar primero.** Busca y analiza webs de gestorías y asesorías españolas (y portales de cliente de gestorías online y de software de despachos) para extraer qué hace que una web de gestoría inspire confianza y qué hace que un área de cliente se use de verdad. Resume lo aprendido en un informe (referencias con URL, qué copiar, qué evitar) y **de ahí sale el diseño**. No uses la plantilla genérica de "gestoría": con esto compiten todas.

**G2. Logo profesional.** La foto `SGA/Recursos/logo-original-foto.webp` es el logo real de SGA, hecho a mano y **fotografiado girado 90°**. Elementos: un rectángulo partido por diagonales en triángulos; en los triángulos de los extremos "S.A." y "S.L."; en el centro "Contabilizado"; una flor morada de seis puntas con el centro amarillo, tallo y hoja verdes; y las letras "SGA" en verde. Rehazlo en **SVG vectorial profesional manteniendo la identidad** (mismos elementos, misma paleta morado, verde y amarillo, mismo concepto), no inventando otro. Entrega: logo completo, versión compacta (solo flor y "SGA"), favicon, versión monocroma y versión sobre fondo oscuro. Anota en el informe qué has cambiado respecto al original y por qué.

**G3. Web pública de la gestoría.** Una web completa y creíble: inicio, servicios (por perfil: arrendadores, autónomos y profesionales, sociedades), cómo trabajamos, equipo, contacto y acceso al área de clientes. Textos en español, cercanos y concretos (qué hacen de verdad, con los plazos y modelos reales), sin relleno. Datos de contacto y equipo ficticios salvo que Javier diga otra cosa.

**G4. Área de clientes, con cuatro vistas:**

| | Móvil | Escritorio |
|---|---|---|
| **Cliente** | Pensada para hacerse en 30 segundos desde el teléfono: ver qué tengo pendiente, subir una foto de un ticket o factura, ver cuánto voy a pagar | Más panorama: histórico, documentos, facturas, presentaciones |
| **Gestor** (personal de SGA) | Consultar el estado, resolver una incidencia, contestar, aprobar, sin perderse | La mesa de trabajo completa: matriz de control, cálculo de IVA, generación de facturas |

Las cuatro vistas son **de primera clase**: no vale encoger la de escritorio. Comprueba cada pantalla con capturas reales de Playwright en **390×844** y **1440×900**, y corrige lo que se vea mal. "Sencilla y muy gráfica": el cliente debe entender su situación mirando, no leyendo.

**G5. Reflejar su forma real de trabajar.** El punto 4 describe lo que hacen hoy. La demo tiene que mostrar que **su Excel de control y sus cálculos de IVA pasan a una herramienta que los hace solos, sin cambiar lo que ya saben hacer**. Los ficheros de `SGA/Recursos/` son la **verdad de referencia**: los cálculos de la demo deben reproducir los de esos Excel (úsalos como oráculo en pruebas automáticas) y los documentos generados (la factura de alquiler) deben parecerse a los reales.

**G6. Datos ficticios.** Ver punto 6. Sin excepciones.

**G7. Lista para enseñarse.** Desplegada, con usuarios de demostración de entrada directa (un clic por perfil, sin contraseñas que recordar), datos cargados con historia plausible y **un guion de demostración de 10 minutos** (`SGA/docs/guion-demo.md`) con el recorrido por perfiles y la frase clave de cada pantalla.

## 3. Lee esto primero

- `SGA/Recursos/apuntes-reunion.md`: los apuntes de Javier en la reunión, transcritos tal cual (con sus faltas). Es la fuente principal.
- `SGA/Recursos/excel-control-impuestos-captura.png`: captura de su Excel "2026 CONTROL DE IMPUESTOS" (una fila por cliente; columnas por trimestre: IVA 303 y 349, retenciones 111, 115 y 123, sociedades 202, contabilidad libros y cuentas anuales, y observaciones).
- `SGA/Recursos/3T  2026 CALCULO IVA CONSTANTINO.xlsx` (arrendador): hojas LIQ IVA, facturas emitidas y recibidas con su resumen, y libro de gastos.
- `SGA/Recursos/2T 2026 CALCULO IVA ASIMETRICA FILMS.xlsx` (sociedad): resumen, libro de emitidas, listado de recibidas y mayores de IVA repercutido y soportado.
- `SGA/Recursos/08 ALQUILER AGOSTO.pdf`: una factura de alquiler emitida, con IVA e IRPF.
- `SGA/Recursos/303 2T 2026 CONSTANTINO.pdf`: el justificante de presentación del modelo 303 en la AEAT.
- El proyecto Aserta que ya existe: `CLAUDE.md`, `PROMPT-AGENTE-PROGRAMADOR.md`, `docs/` (sobre todo `00-vision-y-alcance.md`, `01-mapa-dominio.md`, `04-modelo-datos.md`, `12-infraestructura-despliegue.md`, `adr/`), `informes/` y `src/`. **Ya tiene** catálogo normativo de obligaciones, Kanban, portal del cliente, gestión documental, avisos, exportación contable y facturación con simulador de la AEAT. Reutiliza lo que te sirva.

## 4. Lo que he entendido de su negocio (verifícalo, marcando lo que no cuadre)

SGA es una gestoría que lleva a tres tipos de cliente. Cada uno trabaja distinto:

**A. Arrendador** (ejemplo: un propietario de locales). SGA **emite por él** las facturas de alquiler: una al mes, con IVA 21 % y retención de IRPF del 19 %, y las envía por correo directamente al inquilino con copia al cliente. Se registran en **Monitor**, su programa de contabilidad (confirmado por Javier) y se generan **en bloque al trimestre**, por la fecha del IVA. Las facturas recibidas del arrendador son solo las de **SGA por sus honorarios** (confirmado por Javier: "SGA emite factura mensual por el servicio" son las facturas de SGA al cliente), emitidas con el módulo de facturación de Monitor. SGA presenta el modelo 303 cada trimestre. Su cálculo es **acumulado**: cuota del año hasta la fecha menos lo liquidado en los trimestres anteriores.

**B. Profesional o autónomo.** Puede estar dado de alta en **más de una actividad** (IAE), con un libro de gastos por actividad. Una factura común a dos actividades se imputa a la de mayor actividad. Dos variantes:
  1. **SGA le emite las facturas.** El cliente entrega las facturas recibidas y los gastos de la actividad (tickets, seguro, cuota de autónomos) y el extracto bancario. Presentaciones: trimestral 303 y, a algunos, 130; anual 390 y 347; si tiene empleados, 111 trimestral y 190 anual. Cobro **domiciliado** (se tiene el IBAN) y, si el importe es alto, **se puede fraccionar**.
  2. **No le emite SGA las facturas.** El cliente envía sus facturas emitidas y SGA **comprueba la numeración** (huecos, duplicados, orden de fechas).

**C. Sociedad** (ejemplo: una productora audiovisual). SGA no emite sus facturas: las hace la propia empresa en su sistema. La empresa sube emitidas y recibidas a un drive y da una **clave de consulta del banco** para bajar el extracto y **conciliarlo**. Modelos trimestrales y anuales que corresponden, y un Excel de cálculo de IVA.

**El Excel de control** es una matriz cliente × modelo para cada trimestre. Interpretación mía, **a confirmar con Javier**: celda vacía = pendiente; `NP` = no procede para ese cliente; un nombre propio (p. ej. "NOELIA") = la persona de SGA que lo lleva. "Sdades 202" es el pago fraccionado del Impuesto de Sociedades; "Libros" y "CC.AA." son la legalización de libros y las cuentas anuales.

## 5. Funcionalidad que debes cubrir

Decide tú el diseño de cada pantalla. Lo que tiene que existir, **explicado a un cliente que no sabe de impuestos**:

**Cliente**
- **Inicio** con "qué tengo pendiente" y el estado del trimestre de un vistazo (semáforo, anillo de progreso y próxima fecha).
- **Subir documentos**: desde el móvil, con la cámara; lista de documentos esperados del trimestre marcando lo que falta y lo recibido; extracto bancario o acceso por clave de consulta.
- **"Cuánto voy a pagar este trimestre"**: visual del IVA repercutido menos IVA soportado y del resultado, con la fecha de cargo y el IBAN de domiciliación. Si el importe es alto, **solicitar fraccionamiento** desde ahí.
- **Mis facturas emitidas** (perfiles en los que SGA las emite): lista mensual con estado (generada, enviada al inquilino, copia recibida), desglose de IVA y retención de IRPF, PDF descargable.
- **Facturas que envía el cliente** (perfil B2 y sociedades): subida y **aviso de numeración** si hay huecos o duplicados.
- **Presentaciones hechas**: justificantes descargables (con su código de verificación), histórico por trimestre y modelo.
- **Facturas de SGA** por sus servicios, y mensajes o incidencias con su gestor.

**Gestor**
- **Matriz de control** (sustituye al Excel): clientes × modelos por trimestre, con los mismos estados y colores, filtro por persona, por tipo de cliente y por modelo, barra de avance del trimestre, y "hoy: qué me toca". Clic en una celda para cambiar su estado o ver el detalle.
- **Ficha de cliente**: tipo, actividades (IAE), modelos que le aplican (y cuáles son `NP`), quién emite las facturas, forma de pago, empleados sí o no, y la relación con sus documentos.
- **Mesa de cálculo de IVA** que reproduce el Excel: libros de facturas emitidas y recibidas, resumen por tipo, cálculo **acumulado con lo liquidado en trimestres anteriores** y resultado. Importar un Excel de ejemplo de los que tienen y ver el cálculo. Reproducir con exactitud los números de los dos Excel de referencia.
- **Emisión mensual de facturas de alquiler**, con IVA, retención, numeración correlativa, PDF con el aspecto de la factura real, y envío simulado al inquilino con copia al cliente. Generación en bloque al cierre del trimestre.
- **Libro de gastos por actividad** con la regla de imputación de facturas comunes a la actividad principal.
- **Comprobación de numeración** de facturas emitidas del cliente (huecos, duplicados, fechas).
- **Conciliación bancaria** sencilla (movimientos del extracto contra facturas, con lo pendiente resaltado).
- **Presentación de modelos simulada**: flujo completo hasta un justificante parecido al del PDF de 303 (registro, código seguro de verificación, presentador "Colaborador"), **sin ninguna conexión real con la AEAT**.
- **Calendario** de vencimientos de 2026 con el catálogo normativo que ya hay en Aserta. Si una fecha no figura verificada ahí, déjala marcada `[VERIFICAR]`: no inventes plazos.

**Opcional, si queda tiempo** (en este orden): bloque "Veri*Factu listo" en las facturas que emite SGA por cuenta del cliente (en 2027 será obligatorio; reutiliza el simulador de Aserta y explícalo en una pantalla, sin profundizar); avisos por correo simulados; modo oscuro.

## Diseño (complementa el punto 5)

- La **paleta sale del logo** (morado, verde, amarillo), pensada para dar confianza: sobria y cálida, no infantil. Contraste AA como mínimo.
- Dos voces tipográficas elegidas con criterio y una escala clara. Ícono propio coherente (no mezcles librerías).
- **Muy gráfico**: anillos de progreso, línea de tiempo del trimestre, cascada del IVA, mapa de calor de la matriz. Que un gráfico sustituya a un párrafo siempre que se pueda.
- Móvil: un solo objetivo por pantalla, pulgar primero, navegación inferior. Escritorio: denso pero respirable, con navegación lateral plegable.
- Estados vacíos, cargas y errores cuidados, en español claro, sin jerga.
- **Evita el look genérico de plantilla de gestoría** (banner con fondo azul y apretón de manos, iconos de carpeta, tarjetas todas iguales).

## Pruebas (complementa el punto 5)

- Pruebas automáticas de **los cálculos contra los dos Excel de referencia** (IVA acumulado de Constantino en 3T: repercutido 3.780 €, liquidado 1T y 2T 1.260 € cada uno, soportado 21,75 €; resultado 1.252,75 €; y el resumen de Asimetrica Films en 2T), de la **numeración** (huecos y duplicados) y de la **retención** de la factura (2.000 € + 21 % − 19 % = 2.040 €).
- Capturas de Playwright de las pantallas clave en móvil y escritorio, para **ti**: revísalas tú y corrige antes de entregar. Guárdalas en `SGA/informes/capturas/`.
- Accesibilidad básica: teclado, foco visible, etiquetas, contraste.

## 6. Reglas

1. **Datos ficticios siempre.** Los ficheros de `SGA/Recursos/` contienen **datos reales** de clientes de SGA (NIF, nombres, importes, el nombre de quien presenta). Úsalos solo para entender estructura y calibrar cálculos. **Ni un nombre, NIF, IBAN, dirección ni importe real** puede salir en la demo, en el repositorio, en capturas ni en el informe. Inventa personas y empresas con NIF sintácticamente válidos, conserva los **órdenes de magnitud y las mecánicas** (alquiler mensual, IVA 21 %, retención 19 %, 4.000 € frente a 447.000 €, etc.). Para las pruebas contra los Excel, guarda los oráculos como **cifras anonimizadas**, no como copia de los ficheros. `SGA/Recursos/` no se sube a git.
2. **No inventes datos normativos** (plazos, casillas, tipos). Marca `[VERIFICAR]` y sigue. No hay conexión real con la AEAT: todo es simulado y se dice en pantalla.
3. **Lo dudoso se muestra como dudoso.** No presentes como hecho nada que hayas adivinado de los apuntes.
4. Reutiliza Aserta donde ahorre trabajo (dominio, autenticación, documentos, catálogo normativo, simulador de la AEAT), pero **no rompas la demo de Aserta**: sus pruebas deben seguir en verde. El SGA vive en su propia carpeta `SGA/`, con su propio despliegue y su propia URL. Puedes elegir la pila de la parte nueva si hay una razón clara y la documentas en una decisión corta; por defecto, .NET 10 Razor Pages con htmx y CSS propio, igual que Aserta, para que todo lo demás se pueda reutilizar.
5. El servidor tiene **poca RAM libre** (unos 3,8 GB compartidos): una sola instancia de Chromium. Compila en `~/aserta-scratch`, no en `/tmp` ni en el scratchpad (llenar `/tmp` rompe el shell de todas las sesiones).
6. Nunca secretos en ficheros versionados. `dotnet user-secrets` en desarrollo.
7. Identificadores de dominio y de base de datos en español, sin tildes ni ñ.

## 7. Fases

- **F0. Lectura y preguntas.** Punto 1.
- **F1. Investigación y dirección de diseño.** Informe de referencias, arquitectura de la información de la web y de las cuatro vistas, sistema de diseño (colores, tipografía, componentes) y **wireframes** de las pantallas clave. Incluye la decisión de pila y cómo se encaja con Aserta.
- **F2. Marca.** Logo y variantes en SVG, favicon, guía breve de uso.
- **F3. Web pública.** Móvil y escritorio.
- **F4. Datos.** Modelo, datos ficticios y una **línea de tiempo creíble** (varios trimestres, con pendientes, NP, incidencias, un hueco de numeración, un fraccionamiento solicitado, un extracto sin conciliar) y los tres perfiles de cliente y el gestor.
- **F5. Área del cliente**, móvil y escritorio.
- **F6. Área del gestor**, móvil y escritorio, con la matriz, la mesa de IVA y la emisión de facturas.
- **F7. Cierre.** Pruebas, capturas revisadas, despliegue, guion de demo de 10 minutos, informe final con lo que queda pendiente o dudoso.

## 8. Cómo sabré que está bien

1. Un desconocido entiende, sin ayuda, en la web, qué hace SGA y para quién.
2. Un cliente de perfil arrendador sube un ticket, ve su IVA del trimestre y descarga una factura, desde el móvil, en menos de un minuto.
3. Un gestor abre la matriz y ve **de un vistazo** qué le falta a cada cliente, y puede llegar al cálculo de IVA desde una celda.
4. Los números de la demo coinciden con los de sus Excel de referencia.
5. Nadie de SGA reconoce un dato suyo real en pantalla, y aun así reconoce **su forma de trabajar**.
6. Las cuatro vistas se ven bien en capturas reales.

## 9. Preguntas que probablemente convenga hacer en F0 (no son obligatorias: decide tú cuáles importan)

- URL y forma de despliegue: ¿subdominio nuevo? (Yo no puedo crear DNS.)
- Nombre comercial exacto para la web: "SGA Contabilizado" o "SGA" a secas. Dirección, teléfono, correo y equipo reales o ficticios.
- ¿Quién es "Noelia" y qué significa un nombre en una celda de su Excel? (Mi hipótesis: la persona responsable.)
- "5 documentos" del arrendador: ¿qué son? (Mi hipótesis: los documentos del paquete trimestral.)
- Monitor (contabilidad del despacho): ¿interesa simular una exportación a su formato o solo nombrarlo? (Por defecto: solo nombrarlo y mostrar el libro que ya exportan en Excel.)
- ¿Se mostrará la demo con datos de su propio negocio, o siempre con datos ficticios? (Por defecto: ficticios.)
- ¿Prioridad entre la web pública y el área de clientes si hay que recortar? (Por defecto: el área de clientes.)
