# Arquitectura de la información y wireframes

## Web pública (6 páginas)
```
/                    Inicio: promesa + maqueta del móvil · franja de confianza · 3 perfiles · el trimestre en 4 pasos · qué ves en el área · opiniones · llamada a contacto
/servicios           Un bloque por perfil (arrendadores, autónomos, sociedades): qué hacemos, qué necesitamos, plazos, precio orientativo · «lo que no hacemos»
/como-trabajamos     4 pasos · tu primer mes · compromisos · herramientas
/equipo              3 personas con cita · cómo nos organizamos
/contacto            formulario (simulado) · dirección, teléfono, horario · plano esquemático
/acceso              entrada por perfil con un clic (4 clientes, 3 gestores)
```

## Área del cliente
```
/cliente                 Inicio: anillo del trimestre + «te faltan…» + cuánto vas a pagar + línea de tiempo + modelos + avisos
/cliente/documentos      requisitos del trimestre (barra) + pestañas 1T–4T con lo subido y su estado
/cliente/subir           cámara / fichero + tipo + trimestre → «Recibido» con lo que sigue faltando
/cliente/impuestos       cifra grande + cascada (cobrado − liquidado − pagado) + fecha de cargo + fraccionar + trimestre a trimestre
/cliente/facturas        SGA emite: por meses, estado, PDF · cliente emite: aviso de numeración
/cliente/presentaciones  por trimestre, CSV y justificante PDF
/cliente/honorarios      facturas de SGA
/cliente/mensajes(/id)   conversaciones por asunto, respuesta; /nuevo
/cliente/mas             menú del móvil
Navegación móvil: Inicio · Documentos · [Subir] · Impuestos · Más
```

## Área del gestor
```
/gestor                          Hoy: anillo del trimestre, mis modelos, incidencias, docs por revisar, «qué me toca», próximos vencimientos, reparto
/gestor/matriz                   la matriz (clientes × modelos) con filtros, barra de avance y leyenda; celda → ficha
/gestor/clientes                 lista con avance por cliente
/gestor/clientes/{id}            ficha: pestañas Trimestre (modelos, IVA, documentación, incidencias) · Ficha · Documentos · Facturas · Presentaciones
/gestor/clientes/{id}/iva        mesa de cálculo (LIQ IVA) + libros + exportar CSV + importar Excel + presentar
/gestor/clientes/{id}/gastos     libro de gastos por actividad (IAE), con imputación de comunes
/gestor/clientes/{id}/numeracion huecos, duplicados, fechas + avisar
/gestor/clientes/{id}/conciliacion extracto contra facturas, sugerencias por importe, ignorar, preguntar
/gestor/clientes/{id}/presentar/{o} confirmación y presentación simulada (casillas del 303)
/gestor/alquileres               bloques por arrendador: generar trimestre, enviar, PDF
/gestor/incidencias(/id)         lista y detalle, responder, resolver, conceder/denegar fraccionamiento
/gestor/calendario               mes con plazos y festivos + próximos 90 días
Navegación móvil: Hoy · Matriz · [Clientes] · Incidencias · Plazos
```

## Wireframes de las pantallas clave (móvil 390 / escritorio 1440)

Inicio del cliente, móvil:
```
┌──────────────────────────┐
│ ✿   Inicio          [NC] │  cabecera 60
├──────────────────────────┤
│ Hola, Nuria              │
│ lunes 6 de octubre…      │
│ ┌──────────────────────┐ │
│ │ (anillo 62%)  3T 2026│ │  tarjeta morada
│ │  Te faltan 1 ticket… │ │
│ │  [📷 Subir ahora]    │ │
│ └──────────────────────┘ │
│ ┌──────────────────────┐ │
│ │ Cuánto vas a pagar   │ │
│ │ 486,20 €             │ │  cifra Fraunces
│ │ cargo 20 oct · ES14… │ │
│ └──────────────────────┘ │
│ ●───●───○───○  línea     │
│ [303 · En curso] [130…]  │
│ Lo que necesitamos de ti │
│ ✓ Facturas compras  3/3  │
│ ! Tickets           2/3  │
│ ! Extracto          0/1  │
├──────────────────────────┤
│ ⌂   ▤   (📷)   €   ≡     │  nav inferior
└──────────────────────────┘
```

Matriz del gestor, escritorio:
```
┌────────┬──────────────────────────────────────────────────────────────────────┐
│ lateral│ Control de impuestos · 3T 2026               [las presentaciones…]   │
│ Hoy    │ [3T▾] [Persona▾] [Tipo▾] [Modelo▾] (Filtrar)                         │
│ Matriz │ 19 de 41 presentados ▓▓▓▓▓░░░░░   leyenda ■ ■ □ ■                    │
│ Clientes│┌───────────┬───IVA───┬IRPF┬──Retenciones──┬Soc┬Contab.┬Docs┬Avance┐│
│ Alquil.││ Cliente   │303 │349 │130 │111 │115 │123 │202│Lib│CCAA│    │     ││
│ Incid. ││ Ernesto V.│ ✓5/10│NP │ NP │ NP │ NP │ NP │NP │NP │NP │nada│ 1/1 ││
│ Calend.││ Carmen R. │  —  │NP │ NP │ NP │ NP │ NP │NP │NP │NP │nada│ 0/1 ││
│        ││ Nuria C.  │  —  │NP │ —  │ NP │ NP │ NP │NP │NP │NP │4/6 │ 0/2 ││
│        ││ Luz Norte │ LF  │NP │ NP │✓5/10│NP │ NP │LF │✓  │✓  │3/3 │ 2/5 ││
│        │└───────────┴─────┴────┴────┴────┴────┴────┴───┴───┴────┴────┴─────┘│
└────────┴──────────────────────────────────────────────────────────────────────┘
```

Mesa de IVA, escritorio: tres tarjetas arriba (repercutido: según libros − liquidado 1T − liquidado 2T = a liquidar; soportado igual; resultado en morado con «Presentar 303» e «Importar Excel»), debajo pestañas con el libro de emitidas y el de recibidas con sus sumas.

Subir, móvil: una zona grande con la cámara arriba, tipo y trimestre debajo, un botón «Enviar» a todo el ancho; tras enviar, pantalla «Recibido» con la frase de lo que sigue faltando.
