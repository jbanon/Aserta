# 03 · Web pública (fase 3)

## Qué hice
Seis páginas con textos propios, en español y de tú: inicio, servicios (con un bloque por perfil y anclas), cómo trabajamos, equipo, contacto y acceso al área. Móvil y escritorio (capturas `web-*`).

- **Inicio:** promesa («Tus impuestos al día, sin sustos. Y tú sabiendo cuánto vas a pagar»), maqueta del móvil dibujada en CSS con la pantalla real de «te faltan», franja de confianza, tres perfiles con sus modelos explicados y precio orientativo, el trimestre en cuatro pasos sobre morado, qué ves en el área, tres opiniones ilustrativas y llamada a contacto.
- **Servicios:** para cada perfil, qué hacemos, qué necesitamos del cliente, plazos explicados (con la nota de que están pendientes de confirmar) y «lo que no hacemos».
- **Cómo trabajamos:** cuatro pasos, «tu primer mes», compromisos, herramientas (y la aclaración de que la demo no envía nada a Hacienda).
- **Equipo:** las tres personas ficticias con una cita cada una.
- **Contacto:** formulario simulado, datos, horario y un plano esquemático en SVG.
- **Acceso:** entrada con un clic para cuatro clientes y tres gestores.

## Decisiones
- Sin imágenes de stock: la única «imagen» es la maqueta del móvil, hecha con los mismos componentes del área. Así la portada enseña el producto de verdad.
- Textos con los plazos y modelos reales, siempre explicados; las cifras de confianza y los precios son inventados y están marcados en DUDAS (S-12, S-13).
- Cabecera fija translúcida, contenedor de 1180 px, pie oscuro con la dirección y el horario (lo que más echan en falta las webs de despachos pequeños).

## Cómo probarlo
`/`, `/servicios`, `/como-trabajamos`, `/equipo`, `/contacto`, `/acceso`. En móvil, el menú se abre con el botón de la cabecera.

## Qué queda
Sustituir textos de confianza, precios, testimonios y equipo por los reales; mapa real; fotos del despacho si SGA quiere.
