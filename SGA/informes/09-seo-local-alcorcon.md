# 09 · SEO local de Alcorcón (encargo 03)

Preparado el 2026-10-01 sobre la demo que sigue sirviéndose en `sga.winsoft.es`. El objetivo de posicionamiento es «gestoría Alcorcón»; el dominio definitivo decidido es **sga.es**, todavía sin registrar ni apuntar. Nada de este encargo toca DNS, nginx ni certificados.

## Qué se ha hecho

1. **«Madrid» → «Alcorcón»** en el título, la descripción, el `<h1>` del hero (ahora lleva «Gestoría en Alcorcón» como antetítulo dentro del propio `h1`), la entradilla, los datos de contacto y los títulos y descripciones de Servicios, Equipo y Contacto. «Madrid» solo queda como provincia («Alcorcón (Madrid)») y en los datos ficticios internos de la demo, que no se tocan.
2. **Bloque NAP** (dirección, teléfono, horario) en el pie de todas las páginas públicas, en Contacto y en el lateral de las guías, como `<address>`, con el mismo texto en todos los sitios. Los tres datos son provisionales y llevan la marca `[VERIFICAR]`; la marca desaparece sola al poner `Sga:DatosVerificados = true` cuando SGA confirme los valores. El teléfono es enlace `tel:` y el correo `mailto:`.
3. **Datos estructurados** `schema.org/AccountingService` (el tipo específico de gestoría, subtipo de `LocalBusiness`) en JSON-LD en todas las páginas públicas: nombre, URL canónica, teléfono, correo, dirección postal (Alcorcón, Madrid, ES), zona atendida y horario por tramos (`Sga:Horarios`). Se genera con el serializador de .NET, que escapa `<` y `&`, así que ningún dato puede romper la etiqueta. La CSP no afecta: es un bloque de datos, no un script.
4. **`robots.txt` y `sitemap.xml` condicionados al dominio canónico** (ver mecanismo abajo).
5. **Contacto** con un plano esquemático de Alcorcón (SVG propio, sin servicios externos: municipios vecinos, A-5, M-40, M-50, Cercanías C-5 y Metro L10/L12 con sus estaciones), el marcador de SGA en el centro y enlace a OpenStreetMap. La ubicación exacta se marcará cuando llegue la dirección.
6. **Tres guías de contenido local**, enlazadas desde la portada («Guías para Alcorcón») y el pie:
   - `/alcorcon/alquiler-de-local-impuestos`: IVA, retención, 303, 390 y renta para un propietario particular, con ejemplo numérico (1.200 €/mes) y casos especiales.
   - `/alcorcon/autonomo-plazos-del-trimestre`: los cuatro modelos del trimestre, tabla del calendario del año generada desde `CalendarioFiscal` (con traslado a día hábil), domiciliación, anuales y qué guardar.
   - `/alcorcon/sociedad-obligaciones-fiscales`: trimestrales, pagos fraccionados 202, tabla anual (390/190/180, 347, libros, 200, cuentas) y lo que se olvida (Registro Mercantil de Madrid, IAE, tasas municipales).
   Cada dato normativo que no está confirmado lleva `[VERIFICAR]` visible (umbrales, porcentajes, plazos mercantiles). Los precios citados son los de la página de Servicios.
7. **Extras baratos:** etiqueta `canonical` en todas las páginas, Open Graph y tarjeta de 1200 × 630 (`/img/sga-tarjeta.png`) para que el enlace se vea bien al compartirlo por WhatsApp, hueco para la verificación de Search Console (`Seo:VerificacionGoogle`), y las áreas privadas siguen con `noindex` propio.
8. **Pruebas:** 16 nuevas en `Sga.Tests` (host canónico, robots, sitemap, URL canónica, JSON-LD). Total SGA: 22 en verde.

## El mecanismo del dominio canónico

Una sola variable: `Seo:DominioCanonico = https://sga.es` (en `appsettings.json`; se puede sobreescribir con la variable de entorno `Seo__DominioCanonico`). En cada petición se compara el `Host` con el del dominio canónico (nginx reenvía `Host $host`, comprobado en `deploy/nginx-sga-demo.conf`):

| Petición llega por | `robots.txt` | `sitemap.xml` | Cabecera `X-Robots-Tag` | `<meta name="robots">` | `<link rel="canonical">` |
|---|---|---|---|---|---|
| `sga.winsoft.es`, `127.0.0.1`, cualquier otro | `Disallow: /` | 404 | `noindex, nofollow` (también PDF y estáticos) | `noindex, nofollow` | `https://sga.es/...` |
| `sga.es` | permite la web pública, deniega `/cliente/`, `/gestor/`, `/acceso`, `/salir`; anuncia el sitemap | las 8 URL públicas sobre `https://sga.es` | (no se emite) | (no se emite) | `https://sga.es/...` |

**El día que `sga.es` apunte aquí pasa automáticamente:** el sitio se indexa, el sitemap aparece y las canónicas ya eran correctas, sin desplegar nada. **Lo que habría que hacer a mano ese día,** fuera del código: DNS, bloque de nginx con `server_name sga.es` y certificado (los mismos pasos de `deploy/LEEME.md`), redirigir `www.sga.es` al dominio sin `www` en nginx (`www` no cuenta como canónico a propósito), dar de alta el sitio en Search Console (pegar el código en `Seo:VerificacionGoogle` y reiniciar) y enviar el sitemap. Mientras la demo siga en `sga.winsoft.es`, ese host no se indexa aunque alguien comparta el enlace.

Comprobado en local: con `Host: sga.es` el robots permite y el sitemap devuelve las ocho URL; con el host real todo es `noindex` y el sitemap da 404.

## Capturas
`informes/capturas/seo-portada-escritorio-arriba.png`, `seo-portada-movil.png`, `seo-contacto-escritorio.png`, `seo-contacto-movil.png`, `seo-guia-alquiler-escritorio.png`, `seo-guia-autonomo-escritorio.png`, `seo-guia-autonomo-movil.png`, `seo-guia-sociedad-escritorio.png`.

## Pendiente de SGA o de Javier (no de código)
- Dirección, teléfono y horario reales, letra por letra iguales a los de la ficha de Google Business Profile (DUDAS S-29).
- Registro de `sga.es` y, cuando toque, DNS, nginx y certificado (S-30).
- Ficha de Google Business Profile creada y verificada por el dueño del negocio; reseñas (S-31).
- Revisión normativa de los `[VERIFICAR]` de las tres guías antes de publicar la web definitiva.
