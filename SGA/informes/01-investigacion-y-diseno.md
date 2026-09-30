# 01 · Investigación y dirección de diseño (fase 1)

> Para Javier, que no es del sector. Cada término fiscal va explicado la primera vez.

## Qué hice
1. Leí los apuntes de la reunión, la captura del Excel de control, los dos Excel de cálculo de IVA (con sus fórmulas), la factura de alquiler y el justificante del 303, y el proyecto Aserta.
2. Un agente de investigación analizó 27 webs y portales (gestorías online, despachos pequeños de Madrid, software de facturación y portales de cliente de despachos), descargando cada página y extrayendo textos, colores y tipografías. El resultado completo está abajo, resumido.
3. De ahí salieron la arquitectura de la información (`docs/arquitectura-informacion.md`, con wireframes), el sistema de diseño (`docs/sistema-diseno.md`) y las decisiones técnicas (`docs/decisiones.md`).

## Lo que aprendí de las referencias (resumen)

**Qué inspira confianza en una web de gestoría** (observado en las que convencen: AFGSL, Ficobe, FGM, Xolo, Declarando, Abaq):
- Personas con nombre, cargo y una frase propia. Ningún despacho pequeño de Madrid analizado lo hace: hueco libre.
- Cifras pequeñas y exactas («más de 450 clientes», «20 reseñas con nombre») ganan a grandes redondas sin nombre.
- Precio por perfil en la portada, sin tachados «promo» (Disyem: «55 €/mes para autónomos»; Abaq: «117 € por trimestre»), y las exclusiones dichas en voz alta.
- Dirección, horario y fachada real. Sellos verificables (colegiación, colaborador social de la AEAT) en vez de adjetivos.
- Garantía formulada como consecuencia («si el error es nuestro, respondemos nosotros») y plazos numéricos («respuesta en 48 h», «primer trimestre con revisión extra»).
- Titular que nombra el resultado, no el servicio: «Nos ocupamos de tus gestiones. Tú, de lo que importa.» (eMadrid), «Llega al trimestre sabiendo cuánto apartar» (Trimestre).

**Qué evitar** (lo que hace que una web parezca plantilla): banner azul con apretón de manos, rejilla de 6–12 tarjetas «Fiscal / Contable / Laboral» con texto intercambiable, ilustraciones 3D de stock, contadores animados que sin JavaScript muestran «0 clientes», mezclar tú y usted, formularios repetidos, «uno de los más económicos» sin cifra, área de clientes que es un login de otra marca (a3doc, netasesor), emojis en titulares y ataques a «la gestoría de siempre». Poppins + Elementor y Montserrat son la firma de la plantilla; el azul corporativo es el color por defecto del sector pequeño.

**Qué hace que un área de cliente se use** (Holded, Quipu, Trimestre, Adaral, reseñas de a3doc y TaxDown):
- Una sola acción principal en el móvil: la cámara («Haz una foto al ticket y listo»). Los portales de despacho ponen la carpeta y sufren en reseñas («se cuelga colgando documentos»).
- Estados con color y tiempos; «te faltan X» como titular es el mayor hueco del mercado: nadie lo hace explícito para el cliente.
- Una cifra grande de «cuánto pagar» con la palabra «estimado» y la fecha del cargo.
- Tres avisos bastan: «te falta algo», «ya está presentado, descarga el justificante», «esto pagarás y cuándo».
- Lo que enfada: perder lo subido, límites de ficheros, que conteste cada vez una persona distinta, que pidan lo mismo dos veces.

**Tono:** tú, siempre; frases de 6–10 palabras con verbo; explicar el modelo en la propia frase («el 303, tu IVA del trimestre»); nada de «sin complicaciones», «reimagina» ni exclamaciones.

**Color y tipografía observados:** morado ya lo usan Holded (SaaS) y Declarando (fondo oscuro con amarillo de acento) sin parecer infantiles porque el morado es muy oscuro o un solo acento, el amarillo va en superficies pequeñas y la tipografía es neutra o una serif de display. Verde se lee como «validado». Crema en vez de blanco quita frialdad.

Referencias con URL: ayudatpymes.com, declarando.es, txerpa.com, getquipu.com, holded.com/es, anfix.com, contasimple.com, taxdown.es, xolo.io/es-es, abaq.pro, gestoriaonline.es, disyem.es, asesoriapausan.com, ksasesores.com, ficobe.es, torrealday.es, nuñoasesores.com, gestoriafgm.es, gestoriaemadrid.com, afgsl.es, asesoranza.es, demesayvertizconsultores.com, trimestre.app, trimestral-app.com, adaral.com/portal-de-cliente, a3doc cloud (App Store), marketplacepartners.sage.com (Kabiku). Consultadas el 30/09/2026.

## Qué decidí y por qué
- **Dirección:** «un despacho, no una startup». Papel crema, morado profundo del logo como color de autoridad, verde como único color de «hecho», amarillo solo para «te falta». Serif con carácter (Fraunces) para titulares, sans neutra con dígitos tabulares (Inter) para cifras e interfaz. Iconos propios, una sola familia.
- **Web pública:** portada con la promesa y una maqueta del móvil (la pantalla de «te faltan 2») dibujada en CSS; franja de confianza; un bloque por perfil con sus modelos explicados y un precio orientativo; el trimestre en cuatro pasos; opiniones con nombre de negocio (ilustrativas). Precios y cifras de confianza son inventados y están en DUDAS (S-12).
- **Área del cliente:** móvil primero, un objetivo por pantalla, cámara en el centro de la navegación inferior, anillo del trimestre y frase «te faltan…» en la portada, cascada del IVA.
- **Área del gestor:** la matriz del Excel tal cual (mismas columnas y estados, con colores con criterio), «hoy: qué me toca», y desde cada celda la ficha y la mesa de IVA.
- **Pila:** la de Aserta, en su propia carpeta y con su propia base; solo se reutiliza el dominio puro (calendario hábil, NIF, periodos). PDF sin Chromium. Detalle en `docs/decisiones.md`.

## Qué queda
Ver capturas en `informes/capturas/` y el informe 07 para lo pendiente.
