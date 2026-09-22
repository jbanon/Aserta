# 06 · Portal del cliente (M2), gestión documental (M3) y mensajería contextual

> **Fase 3** · Agente Programador · 2026-09-22

## Qué he construido

Scripts `0006` (documento, vínculo documento-obligación, requisito por periodo, hilo, mensaje) y `0007` (reglas de requisito documental como datos). Motor de requisitos y cálculo de completitud ("te faltan 2 facturas de julio") en el dominio. Subida de documentos con hash, detección de duplicados, almacén cifrado fuera del despliegue y extracción OCR/IA **simulada**. Bandeja documental de la gestoría con validar/rechazar en línea (htmx), detalle con vista previa y datos sugeridos, vinculación a obligaciones y archivo del justificante. Portal del cliente móvil primero: "qué me falta", subir foto, impuestos con aprobación/rechazo del borrador (RD-09), mensajes. Mensajería anclada a obligación o documento compartida por ambos lados, con avisos cruzados. Eventos de dominio in-process: validar un documento que completa el periodo mueve la obligación a «Documentación completa». Tarea diaria de reclamación documental.

## Decisiones que he tomado yo

1. **Los requisitos documentales también son datos** (`cat.ReglaRequisito` + condiciones, mismo mecanismo que las reglas de obligación): facturas recibidas/emitidas y extracto para toda actividad, nóminas si `TieneEmpleados`, recibo de alquiler si `AlquilaLocal`, tickets opcionales. `CantidadPorMes` = 1 para extracto/nóminas/alquiler; nula ("al menos uno") para facturas. **Solo se reclaman periodos ya terminados** (el mes en curso no).
2. **Generación junto con las obligaciones** (`ServicioGeneracionObligaciones` llama al motor de requisitos) y **ajuste manual por cliente** en la pestaña «Documentación» de la ficha (cantidad esperada, obligatorio, no aplica, añadir). Un requisito ajustado a mano pierde `ReglaOrigenId` y el motor deja de tocarlo.
3. **La frase**: `Te falta(n) N <tipo en plural llano> de <mes>` (`TipoDocumento.Plural`). Los rechazados no cuentan como recibidos; los pendientes de revisión sí (el cliente ya ha hecho su parte).
4. **Documentación completa automática**: al validar un documento se publica `DocumentoValidado`; `ServicioRequisitos` comprueba si **todos los requisitos obligatorios de los meses del periodo de la obligación** están cubiertos y, si es así, la mueve de `PendienteDocumentacion` a `DocumentacionCompleta` con comentario. Solo hacia delante; no la devuelve a pendiente si luego se rechaza algo (D2 del mapa de dominio lo deja abierto).
5. **Vínculo automático documento ↔ obligación** por periodo: un documento de julio se vincula a las obligaciones del 3T y a las mensuales de julio del mismo ejercicio (no a las anuales), con evento `DocumentoVinculado` en el historial. Además vínculo manual desde el detalle.
6. **Duplicados**: `UNIQUE (ClienteId, HashSha256)`; la subida devuelve un error legible con cuándo y como qué se subió la primera vez.
7. **Almacén**: `AlmacenDocumentalFicheros` = fichero por clave opaca en 256 subcarpetas, **cifrado con ASP.NET Core Data Protection** (claves en `DataProtection:RutaClaves`). En desarrollo, `almacen-dev/` en la raíz del repo (ignorado por git); en el servidor `/var/lib/aserta/documentos` (unidad systemd). Sustituible por un blob store sin tocar la aplicación.
8. **Extractor simulado** (DA-08): datos deterministas a partir del hash (proveedor ficticio, base, IVA, total, confianza) con `OrigenExtraccion = "Simulado"` y así etiquetado en pantalla. Solo para facturas y tickets.
9. **Límites de subida**: 20 MB, JPG/PNG/WEBP/HEIC/PDF. El binario se sirve descifrado por `/Documentos/Contenido/{id}` (en línea si imagen/PDF), pasando siempre por el servicio que comprueba tenant y cliente. La CSP permite el `iframe` del PDF por ser mismo origen.
10. **Bandeja**: por defecto solo "pendientes de revisar"; validar/rechazar repintan la fila por htmx (`hx-target="closest tr"`) y funcionan también sin JavaScript (formulario + redirección). El rechazo exige motivo, que el cliente ve en el portal.
11. **Mensajería compartida**: las páginas `/Mensajes/Hilo` y `/Mensajes/Nuevo` sirven a gestoría y cliente (cambian de layout según el rol). Hilo = exactamente una obligación **o** un documento (CHECK en base de datos y en el dominio). Cada mensaje nuevo publica `MensajeNuevo` → aviso en la bandeja del asesor (si escribe el cliente) o de los usuarios del cliente (si escribe la gestoría). Lectura marcada al abrir el hilo, por lado.
12. **Portal**: `_LayoutPortal` con barra inferior (Inicio, Documentos, Impuestos, Mensajes), `input type=file` con `capture="environment"` para abrir la cámara, estados de obligación traducidos a lenguaje llano ("Esperando tus documentos", "Tu gestoría lo está preparando"…), historial simplificado. Solo `ClienteAdmin` aprueba/rechaza; `ClienteUsuario` ve pero no aprueba.
13. **Rechazo del borrador por el cliente** = transición `PendienteAprobacionCliente → EnPreparacion` con el motivo en el historial y aviso al asesor (la máquina de estados ya lo contemplaba).
14. **Justificante**: desde el detalle de una obligación `Presentado`, subir el justificante lo valida, lo enlaza (`JustificanteDocumentoId`) y **cierra** la obligación (paso 7 del ciclo).
15. **Reclamación documental** (`ReclamacionDocumentalWorker`): tarea diaria que, para obligaciones abiertas que vencen en ≤ 15 días con requisitos incompletos, deja al cliente un aviso "Para el 303 de 3T 2026 nos falta documentación: te faltan 2 facturas de julio…" (un aviso por obligación y día).
16. **Seed documental**: la panadería tiene todos los meses cerrados cubiertos salvo el de referencia (hace dos meses), con 6 de 8 facturas (una sin revisar en la bandeja), un 303 del trimestre anterior en «Pendiente aprobación cliente» con borrador 1.234,56 € y un hilo abierto por el asesor. Marcador `SeedDocumentalDemo`. 59 PDF ficticios de una página generados por el propio sembrador.
17. **Auditoría** ampliada a `Documento` y `RequisitoPeriodo`.

## Desviaciones

- El portal no tiene todavía gestión de usuarios de la empresa por el `ClienteAdmin` (§1.2 del mapa de dominio). `ServicioUsuarios.CrearAsync` ya lo contempla; falta la pantalla.
- No hay "modo sin conexión" ni reintento de subida en el móvil (ADR-003 §4 lo pide cuidar); la subida es un formulario normal.
- `Documento.Periodo` admite trimestres y anual además de meses; los requisitos son mensuales. Un documento marcado "3T" no cuenta para requisitos mensuales (queda anotado como hueco).

## Huecos encontrados

- El modelo de datos no dice si el estado `DocumentacionCompleta` debe **revertirse** cuando se rechaza un documento después. He elegido no revertir.
- `RequisitoPeriodo` no tiene columna `NoAplica` en el modelo; la he añadido para "desactivar sin borrar".
- No está definido quién recibe la reclamación documental (todos los usuarios del cliente, en mi implementación) ni con qué antelación (15 días).
- `cat.ReglaRequisito` no aparece en `04-modelo-datos.md`.

## Cómo probarlo

1. **Móvil** (o ventana estrecha): entrar como `panaderia@demo.aserta.local`. Inicio: «Te faltan 2 documentos» y «Te faltan 2 facturas de compra de julio». Pulsar «Subir» → se abre la cámara en el móvil; elegir foto de un ticket → «¡Recibido!». Subir la misma foto otra vez → aviso de duplicado.
2. **Gestoría** (`carlos@…`): campana con aviso; **Bandeja documental** muestra el ticket; «Validar» repinta la fila; abrir el detalle: vista previa, datos sugeridos (simulado), obligaciones vinculadas (303/3T, 111/3T). Rechazar la factura pendiente con motivo → el cliente lo ve en «Documentos» con el motivo.
3. **Aprobación**: como cliente, Impuestos → «Esperan tu aprobación» → 303 · 3T 2026 → «Doy mi conformidad: 1.234,56 €». Como gestoría, en el detalle de esa obligación aparece «Aprobación del cliente» con fecha y el evento en el historial; Carlos recibe el aviso. Ahora «Mover a Presentado» ya no da RD-09.
4. **Mensajes**: responder al hilo desde el portal; en la gestoría, «Mensajes» marca 1 sin leer; abrir un hilo desde una obligación o un documento.
5. **Ficha del cliente → Documentación**: ajustar «facturas de julio» a 6 esperadas → el portal deja de reclamar.
6. **Justificante**: mover una obligación a Presentado y archivar un PDF como justificante → pasa a Cerrado.
7. Como socio, en **Avisos**, «Generar avisos» ejecuta las tareas diarias a mano (vencimientos y reclamaciones).

## Estado de los tests

- Dominio (73): + motor de requisitos (perfil, periodos terminados, idempotencia y ajustes manuales), frases singular/plural, completitud por trimestre (opcionales no bloquean), rechazo con motivo, ancla única del hilo.
- Integración (27): + subida cifrada con hash y descifrado al abrir, duplicado rechazado, validar todos los requisitos del 1T mueve el 303/1T a «Documentación completa» y crea el vínculo, el cliente aprueba y la gestoría ve conformidad + aviso, un usuario de otro cliente no puede abrir el documento.
- Flujo completo por HTTP verificado con script (portal, subida de PNG, duplicado, aprobación, hilo, bandeja con htmx, rechazo, ficha).
- No cubierto: `capture` de cámara real, reclamación documental (probada a mano), justificante (probado a mano), tamaño máximo.
