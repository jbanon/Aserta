# 05 · Área del cliente (fase 5)

## Qué hice
Ocho pantallas, móvil (navegación inferior con la cámara en el centro) y escritorio (lateral morado, dos columnas). Capturas `cliente-*`.

- **Inicio:** saludo, tarjeta del trimestre con **anillo de avance**, la frase «te faltan…» y el botón «Subir ahora»; «cuánto vas a pagar» con la fecha del cargo y la cuenta; línea de tiempo del trimestre (recopilar → calculamos → presentamos → cargo); los modelos del trimestre como píldoras con estado; lo que necesitamos de ti; últimos avisos. El arrendador ve en su lugar sus facturas del trimestre.
- **Documentos:** requisitos del trimestre con barra y contadores, pestañas por trimestre con lo subido, su tipo y su estado (recibido / revisado / incidencia).
- **Subir:** zona grande con la cámara (`capture="environment"`), tipo, trimestre y nota; vista previa de la foto; al enviar, «Recibido» con lo que sigue faltando. Los ficheros se guardan fuera del repositorio.
- **Cuánto voy a pagar:** cifra grande, **cascada** (IVA cobrado en el año − ya liquidado − IVA pagado = resultado), fecha de cargo, botón «Solicitar fraccionamiento» si el importe supera el umbral (abre una incidencia al gestor), y el año trimestre a trimestre con justificantes.
- **Facturas:** si SGA emite, por meses con estado, desglose y PDF; si emite el cliente, sus facturas con el **aviso de numeración** (huecos, duplicados, fechas).
- **Presentaciones:** por trimestre, con CSV y justificante PDF descargable.
- **Facturas de SGA**, **Mensajes** (conversaciones por asunto, respuesta, nueva consulta) y **Más** (menú del móvil).

## Decisiones
- Un objetivo por pantalla en móvil; ninguna tabla ancha en el móvil del cliente.
- El cliente nunca ve códigos de la AEAT ni casillas: ve «presentado», la cifra y la fecha.
- Los avisos se marcan leídos al ver el inicio; los mensajes del gestor, al abrir la conversación.

## Cómo probarlo
`/acceso` → Nuria Campos (móvil: ancho 390 px). Subir un ticket; ver Impuestos; abrir Mensajes. Luego Ernesto Valdés: facturas y PDF; presentaciones y justificante. Tomás: fraccionamiento. Marcos: aviso de numeración.
