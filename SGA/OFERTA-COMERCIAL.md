# Oferta comercial y plan de posicionamiento (SGA) — para la reunión del 2026-10-08 (semana del 6 de octubre)

> Notas de trabajo para Javier. No es un documento para entregar tal cual al cliente sin revisar: faltan datos reales (dirección, teléfono, horario) y el punto de la oferta se cortó al dictarlo.

## 1. Oferta económica

**Primer trimestre: 2.500 €**
- Desarrollo de la página web.
- Contratación del dominio.
- Desarrollo de la aplicación durante 3 meses.

**A partir del segundo trimestre: 1.000 €/trimestre**
- Hosting de la web y de la aplicación.
- Mantenimiento evolutivo de la aplicación.

**Pendiente:** el dictado se cortó justo después de "mantenimiento evolutivo de aplicación". Si había algo más en la lista de la cuota trimestral (soporte, horas incluidas, límites), añádelo antes de la reunión.

## 2. Para cuando tengamos el dominio definitivo de la gestoría

Dos condiciones previas, que no dependen de nosotros:
- El **dominio real** de la gestoría (lo de ahora, `sga.winsoft.es`, es una demo de pruebas).
- La **ficha de Google Business Profile**: la tiene que crear y verificar el dueño del negocio (carta o videollamada de Google), no se puede hacer desde el código.

En cuanto haya dominio, el encargo a fable incluye:
1. Migrar el despliegue del subdominio de pruebas al dominio real (DNS, nginx, certificado — mismos pasos que ya documentamos en `SGA/deploy/LEEME.md`, repetidos para el dominio nuevo).
2. **Cambiar "Madrid" por "Alcorcón"** en el título, la descripción y el titular de la web — ahora mismo la demo dice "Gestoría en Madrid", y el objetivo de posicionamiento real es **"gestoría Alcorcón"**.
3. Dirección, teléfono y horario reales en el pie de **todas** las páginas, exactamente iguales que en la ficha de Google (si no coinciden letra a letra, Google lo penaliza).
4. Información estructurada de negocio local (marcado de datos: tipo de negocio, dirección, horario) para que Google entienda qué es, dónde está y cuándo abre.
5. `robots.txt` y mapa del sitio.
6. Página de contacto con el mapa real de Alcorcón.
7. Que la demo actual (el subdominio de pruebas) lleve un "no indexar" explícito, para que no compita con la web real cuando exista.
8. Dos o tres páginas de contenido que respondan a búsquedas reales de un vecino de Alcorcón (por ejemplo "alquiler de local en Alcorcón, qué impuestos pago" o "autónomo en Alcorcón, plazos del trimestre"). Mejor dos o tres bien escritas que muchas de relleno.
9. Dejar anotado en `SGA/informes/DUDAS.md` que dirección, teléfono y horario reales deben venir de la gestoría, marcados `[VERIFICAR]` hasta entonces.

## 3. Qué decirle al cliente sobre el SEO de "gestoría Alcorcón"

Sí se puede trabajar, y "gestoría Alcorcón" es de las búsquedas locales más alcanzables. Pero conviene que entienda qué decide ese resultado, porque la mayor parte no está en la web.

**Qué aparece cuando alguien busca "gestoría Alcorcón":**

1. **El mapa con tres fichas (Google Maps).** Es lo primero que ve la gente y lo que más llamadas trae. No depende de la web, sino de la ficha de Google Business Profile: que exista, que tenga la dirección de Alcorcón verificada, la categoría "Gestoría" o "Asesoría fiscal", fotos, horario y, sobre todo, reseñas recientes y respondidas.
2. **Los resultados normales.** Ahí compiten las gestorías de la zona y los portales tipo directorio. Para una gestoría de barrio, con una web bien hecha y unos meses de trabajo, estar en la primera página es realista. Garantizar "la primera" no lo puede prometer nadie honestamente.

**Qué tiene ya la web y qué le falta:**

- **Lo bueno:** es HTML servido desde el servidor, rápida, con títulos y descripción por página. Eso es la base y ya está.
- **Lo que falta, y es fácil:** hoy la web dice "Gestoría en Madrid" en el título, la descripción y el hero. Para posicionar en Alcorcón tiene que decir Alcorcón, con la dirección y el teléfono reales en el pie de todas las páginas, exactamente iguales que en la ficha de Google. Faltan también la información estructurada de negocio local, el fichero de robots, el mapa del sitio y una página de contacto con el mapa real de Alcorcón.
- **Lo que requiere constancia:** contenido que responda a lo que busca un vecino de Alcorcón. Dos o tres páginas así, bien escritas, valen más que cien repeticiones de la palabra.

**Dos condiciones previas que no dependen de nosotros:**

- Hace falta el dominio real de la gestoría. Lo que hay ahora es una demo en un subdominio de pruebas y no debe indexarse; de hecho conviene que la demo lleve un "no indexar" explícito para que no compita con la web real cuando exista.
- La ficha de Google la tiene que crear y verificar el dueño del negocio, con una carta o una videollamada de Google. Eso lo hace la gestoría, no se puede hacer desde el código.

**Si quiere que lo preparemos**, el trabajo en la demo sería: cambiar "Madrid" por "Alcorcón" donde toque, añadir la información estructurada de negocio local, robots y mapa del sitio, el bloque de dirección y teléfono en el pie, y una página de ejemplo de contenido local. Quedaría anotado en DUDAS que la dirección, el teléfono y el horario reales deben venir de la gestoría, con `[VERIFICAR]` hasta entonces.

**Plazos: qué decirle al cliente.** Mejor no prometer una fecha. Conviene separar dos cosas:

- **Que la web exista para Google (indexarse):** días. En cuanto esté publicada con el dominio real y se le avise a Google (mapa del sitio, Search Console), la indexa normalmente en menos de una semana.
- **Que aparezca bien colocada al buscar "gestoría Alcorcón" (posicionarse):** meses, no días. No lo deciden solo los cambios técnicos: un dominio nuevo empieza sin ninguna autoridad ante Google aunque esté todo perfecto por dentro; la ficha de Google Maps suele moverse algo más rápido, pero compite con gestorías que ya llevan años acumulando reseñas; y los competidores también se mueven, no es una meta fija.
- **Después tampoco se termina:** hay que seguir pidiendo y respondiendo reseñas, añadir contenido local de vez en cuando, vigilar que los datos de la ficha de Google y de la web sigan coincidiendo, y ajustar si cambian los algoritmos o entra un competidor fuerte. Es el argumento para justificar el **mantenimiento evolutivo** de los 1.000 €/trimestre (sección 1): no es solo mantener la web encendida, el SEO sigue necesitando trabajo después de la entrega.

Frase para decirle tal cual: *"Que la web exista para Google es cuestión de días. Que aparezca donde queremos cuando alguien busca 'gestoría Alcorcón' es cuestión de meses, y no se lo puedo prometer con fecha exacta porque no lo decidimos solo nosotros: también sus competidores y sus propias reseñas. Lo que sí le prometo es que desde el primer día hacemos todo lo que depende de nosotros, y lo seguimos revisando cada trimestre."*
