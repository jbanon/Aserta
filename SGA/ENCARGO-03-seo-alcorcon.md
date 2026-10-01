# Encargo 03: preparar el SEO local de Alcorcón (sin desplegar aún en el dominio definitivo)

> Continuación sobre `SGA/`. El dominio elegido es **sga.es** (`sga.com` estaba ocupado), pero **todavía no se despliega ahí** — sigue registrándose/gestionándose aparte, y la demo sigue viviendo en `sga.winsoft.es`. Este encargo es solo lo que se puede adelantar sin ese paso.

## Por qué

Javier va a hablar con SGA la semana del 6 de octubre sobre precio y sobre el SEO local. El objetivo de posicionamiento real es **"gestoría Alcorcón"**, no Madrid (la demo dice "Gestoría en Madrid" por error: es un dato de relleno, no el real). El detalle completo de por qué y cómo explicárselo al cliente está en `SGA/OFERTA-COMERCIAL.md`, secciones 2 y 3. Este encargo es la parte técnica de esa sección 2 que no depende de tener el dominio ya apuntando aquí.

## Qué sí hacer ahora

1. **Cambiar "Madrid" por "Alcorcón"** en el título, la descripción (`<meta name="description">`), el `<h1>` del hero y cualquier otro sitio de la web pública donde aparezca "Madrid" como localización. Revisa también el guion de demo y los informes si mencionan "Madrid" como si fuera el dato real (no hace falta tocar los datos ficticios internos de la demo, solo lo que se presenta como la ubicación de SGA).
2. **Bloque de dirección, teléfono y horario (NAP)** en el pie de todas las páginas de la web pública, con los tres datos marcados `[VERIFICAR]` (igual que haces con otros datos pendientes de confirmar): dirección en Alcorcón, teléfono, horario. Constrúyelo ya con la estructura final para que solo haga falta rellenar los valores reales cuando los tengamos.
3. **Información estructurada de negocio local** (`schema.org/LocalBusiness` o `AccountingService` si existe un tipo más específico, en JSON-LD): nombre, tipo de negocio, dirección y horario con los mismos valores `[VERIFICAR]` del punto 2, para que estén listos para rellenar.
4. **`robots.txt` y `sitemap.xml`**, pero con una condición importante: **mientras el sitio responda en el host de la demo (`sga.winsoft.es`), tiene que decir "no indexar"** (meta `noindex` y/o disallow en `robots.txt`); en cuanto responda en el dominio definitivo (`sga.es`), tiene que indexarse con normalidad, sin que haga falta tocar código el día del cambio. La forma más simple: una variable de configuración con el dominio canónico (`Seo:DominioCanonico`, por ejemplo `https://sga.es`) y una comprobación en tiempo de ejecución de si el `Host` de la petición coincide con ese dominio canónico; si no coincide (como ahora, que es `sga.winsoft.es`), se sirve `noindex` y un `robots.txt` que deniega todo; si coincide, se sirve normal con el `sitemap.xml` y las etiquetas canónicas apuntando siempre a `https://sga.es/...`. Así el cambio de dominio no exige ningún despliegue de código aparte, solo cambiar esa variable.
5. **Página de contacto** con un mapa centrado en Alcorcón (sin necesitar aún la dirección exacta: un mapa general del municipio vale hasta tener la dirección real) y el mismo bloque NAP del punto 2.
6. **Dos o tres páginas de contenido local**, de las que menciona la sección 3 de `SGA/OFERTA-COMERCIAL.md`: por ejemplo "Alquiler de un local en Alcorcón: qué impuestos pagas" y "Autónomo en Alcorcón: plazos del trimestre". Contenido real y útil, no relleno con la palabra repetida; puedes basarte en lo que ya sabes del dominio fiscal de la demo (modelos, plazos) y dejar `[VERIFICAR]` donde haga falta un dato normativo que no tengas confirmado.
7. Anota en `SGA/informes/DUDAS.md` que dirección, teléfono y horario reales deben venir de SGA, y que el dominio canónico ya está decidido (`sga.es`) pero pendiente de registrar/apuntar.

## Qué NO hacer todavía

- **No registres ni contrates el dominio.** Eso lo hace Javier directamente con el registrador; no es una tarea de código.
- **No toques DNS, nginx ni certificados** para `sga.es`. Sigue desplegando y sirviendo todo desde `sga.winsoft.es`, como hasta ahora.
- No hace falta ninguna ficha de Google Business Profile ni nada fuera del código: eso lo gestiona el cliente.

## Reglas de siempre

- Datos ficticios donde no haya dato real; marca `[VERIFICAR]` lo pendiente de SGA, no inventes dirección ni teléfono reales.
- No rompas las pruebas de SGA ni de Aserta.
- Informe corto en `SGA/informes/` al terminar, con capturas de las páginas tocadas y una nota clara de qué pasa automáticamente el día que `sga.es` apunte aquí (según el mecanismo del punto 4) y qué habría que seguir haciendo manualmente ese día (si acaso, nada más que cambiar la variable de configuración).
