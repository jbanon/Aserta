# 06.1 · Especificación Veri\*Factu

> **Estado:** vigente · **Versión:** 1.0 · **Fecha:** 2026-09-22
> Documento maestro del módulo M6. Los detalles finos están en los otros cuatro ficheros de esta carpeta.

| Documento | Contenido |
|---|---|
| **especificacion.md** *(este)* | Marco normativo, registros, tipos de factura, inalterabilidad, alcance |
| [huella-y-vectores-prueba.md](huella-y-vectores-prueba.md) | Cadena de la huella, formatos y **vectores de prueba verificados** |
| [envio-y-cola-reintentos.md](envio-y-cola-reintentos.md) | Servicio web, outbox, estados, reintentos, simulador |
| [qr-y-pdf.md](qr-y-pdf.md) | QR normalizado, URL de cotejo, plantilla y generación del PDF |
| [declaracion-responsable.md](declaracion-responsable.md) | Contenido y versionado de la declaración responsable |

---

## 1. Marco normativo

| Norma | Qué aporta | Estado de verificación |
|---|---|---|
| **Real Decreto 1007/2023, de 5 de diciembre** | Reglamento de requisitos de los sistemas informáticos de facturación (**RSIF**). Define integridad, conservación, accesibilidad, legibilidad, trazabilidad e inalterabilidad | Citado en la propia documentación técnica de la AEAT ✅ |
| **Orden HAC/1177/2024** | Especificaciones técnicas, funcionales y de contenido; diseños de registro; listas de valores del anexo | `[VERIFICAR]` referencia exacta y posibles modificaciones posteriores, en el BOE |
| **Real Decreto 1619/2012** (art. 6.5) | Reglamento de obligaciones de facturación | Citado por la AEAT ✅ |
| **Real Decreto-ley 15/2025, de 2 de diciembre** (BOE de 3 de diciembre de 2025) | Aplaza la obligatoriedad | Fuentes secundarias coincidentes; `[VERIFICAR]` contra el BOE y **confirmar la convalidación por el Congreso** |
| Documentación técnica de la AEAT | Esquemas XSD, WSDL, especificaciones de huella y de QR | Consultada el 2026-09-22 ✅ |

### 1.1 Calendario de obligatoriedad

| Sujeto | Fecha |
|---|---|
| Contribuyentes del **Impuesto sobre Sociedades** | **1 de enero de 2027** |
| **Resto** (autónomos y demás obligados que usen SIF) | **1 de julio de 2027** |
| **Productores de software** | Ya deben cumplir: los sistemas comercializados tienen que estar adaptados antes de esas fechas |

> **Lectura de producto.** Estamos en septiembre de 2026. Quedan **poco más de tres meses** para la primera oleada y nueve para la segunda. Cada gestoría de España tiene ahora mismo una lista de clientes que necesitan cambiar de sistema de facturación. Ése es el argumento comercial y es el motivo por el que M6 no es un módulo más. Ver [10-argumentario-comercial.md](../10-argumentario-comercial.md).
>
> **Y la advertencia correspondiente:** esta obligatoriedad ya se ha aplazado dos veces. El argumentario no debe construirse *sólo* sobre la fecha, porque puede volver a moverse. Se construye sobre el ahorro de tiempo; la fecha es el detonante, no la propuesta de valor.

## 2. Los dos modos, y por qué elegimos uno

El RSIF admite dos formas de cumplir:

| Modo | Cómo funciona | Nuestra decisión |
|---|---|---|
| **VERI\*FACTU** *(sistemas de emisión de facturas verificables)* | Cada registro de facturación se **remite a la AEAT** de forma inmediata. A cambio, el sistema queda exento de otros requisitos (firma electrónica de registros, registro de eventos) | ✅ **Es el único que implementamos** |
| **NO VERI\*FACTU** | No se remite. A cambio hay que **firmar electrónicamente** cada registro y mantener un **registro de eventos** encadenado y firmado | ❌ Descartado (DA-11) |

**Justificación de descartar NO VERI\*FACTU:** exige firma electrónica de cada registro y un registro de eventos adicional (con su propia cadena de huellas, §3.3 de `huella-y-vectores-prueba.md`), lo que multiplica la superficie normativa y la de pruebas **sin aportar nada a la demo ni al producto**. Nuestros usuarios son gestorías, que trabajan con la AEAT todos los días y no tienen ninguna resistencia a que se le remita información. Además es el modo que la Administración favorece. Se documenta como decisión consciente, no como omisión.

> Consecuencia práctica: el QR de nuestras facturas usa siempre el recurso `ValidarQR`, no `ValidarQRNoVerifactu` (ver `qr-y-pdf.md` §3.1), y todas nuestras facturas llevan la leyenda de verificable.

## 3. Registros de facturación

### 3.1 Los dos tipos

| Registro | Cuándo se genera | Qué contiene |
|---|---|---|
| **Registro de alta** (`RegistroAlta`) | Al expedir cualquier factura (completa, simplificada o rectificativa) | Identificación de la factura, tipo, desglose, importes, encadenamiento, identificación del sistema informático |
| **Registro de anulación** (`RegistroAnulacion`) | Al **anular** una factura previamente registrada | Identificación de la factura anulada y encadenamiento. **No lleva importes ni tipo de factura** |

Ambos se encadenan en la **misma** cadena del emisor: un registro de anulación ocupa su posición en la secuencia igual que uno de alta.

### 3.2 Esquemas oficiales

La AEAT publica los esquemas en su portal de desarrolladores:

| Fichero | Contenido |
|---|---|
| `SuministroLR.xsd` | Operaciones de alta y anulación |
| `SuministroInformacion.xsd` | Definición de tipos comunes |
| `RespuestaSuministro.xsd` | Esquema de la respuesta |
| `SistemaFacturacion.wsdl` | Definición del servicio web |

> **Regla innegociable (regla de trabajo 4 del encargo):** los nombres de elementos, los tipos y las listas de valores se toman **de estos ficheros**, descargados del portal oficial y **versionados dentro del repositorio** en `src/Aserta.Verifactu/Esquemas/`. No se transcriben a mano desde un blog ni desde este documento. El XML generado se **valida contra el XSD antes de enviarse** (ver `envio-y-cola-reintentos.md`).

### 3.3 Campos confirmados contra documentación oficial

Estos nombres y rutas están tomados literalmente de la especificación de huella de la AEAT (v. 0.1.2) y son, por tanto, **fiables**:

**Registro de alta:** `RegistroAlta/IDFactura/IDEmisorFactura`, `.../NumSerieFactura`, `.../FechaExpedicionFactura`, `RegistroAlta/TipoFactura`, `RegistroAlta/CuotaTotal`, `RegistroAlta/ImporteTotal`, `RegistroAlta/Encadenamiento/RegistroAnterior/Huella`, `RegistroAlta/FechaHoraHusoGenRegistro`, `RegistroAlta/Huella`, y el indicador `PrimerRegistro`.

**Registro de anulación:** `RegistroAnulacion/IDFactura/IDEmisorFacturaAnulada`, `.../NumSerieFacturaAnulada`, `.../FechaExpedicionFacturaAnulada`, `RegistroAnulacion/Encadenamiento/RegistroAnterior/Huella`, `RegistroAnulacion/FechaHoraHusoGenRegistro`, `RegistroAnulacion/Huella`.

**Bloque de sistema informático** (aparece en los registros de evento y, con la misma forma, identifica al SIF): `SistemaInformatico/NIF`, `SistemaInformatico/IDOtro/ID`, `SistemaInformatico/IdSistemaInformatico`, `SistemaInformatico/Version`, `SistemaInformatico/NumeroInstalacion`. Estos cinco valores deben ser **coherentes con la declaración responsable** ([declaracion-responsable.md](declaracion-responsable.md)).

**Obligado:** `ObligadoEmision/NIF`.

### 3.4 Campos **no** confirmados  `[VERIFICAR]`

El resto de la estructura (desglose de IVA, destinatarios, datos de facturas rectificadas, bloque de representación, subsanación, rechazo previo, cabecera del envío) **no ha sido verificado campo a campo** en este documento y **no se transcribe aquí para no inducir a error**. Se toma del XSD al implementar. En particular quedan pendientes de confirmar:

- [ ] Estructura exacta del desglose por tipo impositivo y sus claves de régimen y de operación.
- [ ] Nombres de los campos de identificación del destinatario (nacional y extranjero).
- [ ] Bloque de identificación de la factura rectificada y del importe rectificado.
- [ ] Campos de **subsanación** y de **rechazo previo** de un registro.
- [ ] Bloque de representación / remitente distinto del obligado (afecta al [ADR-002](../adr/ADR-002-certificado-y-representacion-verifactu.md)).
- [ ] Lista de valores oficial de los **tipos de factura** (§4) y de los tipos de rectificativa.

## 4. Tipos de factura

Tratamiento funcional previsto. **Los códigos concretos se confirman contra la lista de valores del anexo de la orden** `[VERIFICAR]`; la interpretación funcional es la habitual del sector y coincide con la de otros sistemas de la AEAT.

| Código | Qué es | Soporte en la demo |
|---|---|---|
| **F1** | Factura completa (con identificación del destinatario) | ✅ Completo |
| **F2** | Factura simplificada (el clásico "ticket", sin identificar al destinatario) | ✅ Completo |
| **F3** | Factura emitida en sustitución de facturas simplificadas ya declaradas | ⚠️ Modelada, no implementada en la demo |
| **R1** | Rectificativa por error fundado en derecho y por los supuestos del art. 80 Uno, Dos y Seis LIVA | ✅ Completo |
| **R2** | Rectificativa por concurso de acreedores (art. 80 Tres LIVA) | ⚠️ Modelada |
| **R3** | Rectificativa por créditos incobrables (art. 80 Cuatro LIVA) | ⚠️ Modelada |
| **R4** | Rectificativa: resto de casos | ✅ Completo |
| **R5** | Rectificativa sobre facturas simplificadas | ✅ Completo |

**Regla de producto:** la interfaz **no pregunta al usuario por el código**. Pregunta *"¿por qué rectifica esta factura?"* con opciones en lenguaje llano, y el sistema deriva el código. Un autónomo no sabe qué es un R2, y obligarle a elegir garantiza que elegirá mal.

### 4.1 Rectificativas: por sustitución o por diferencias

Una rectificativa puede expresar **el importe correcto completo** (sustitución) o **sólo la diferencia**. Ambas formas existen en la norma y el registro lo refleja `[VERIFICAR]` nombres de campo. La demo implementa **por diferencias** como modo por defecto por ser el más habitual en gestoría, y deja el otro modelado.

## 5. Inalterabilidad

Es el requisito que más condiciona el diseño y el que más cuesta interiorizar a quien viene del desarrollo de gestión corriente.

> **Una factura emitida nunca se edita ni se borra.** Si está mal, se emite una **rectificativa**. Si no debió emitirse, se genera un **registro de anulación**. La factura original permanece, y su registro también.

Cómo se garantiza (detalle en [04-modelo-datos.md](../04-modelo-datos.md) §6.1):

1. Tablas `vf.FacturaEmitida`, `vf.LineaFactura` y `vf.RegistroFacturacion` con `TRIGGER INSTEAD OF UPDATE, DELETE` que lanza `THROW`.
2. `DENY UPDATE, DELETE` al usuario de la aplicación sobre el esquema `vf`, con `GRANT` selectivo sólo a las tres tablas mutables (`EstadoEnvioRegistro`, `EnvioPendiente`, `CadenaEmisor`).
3. El estado del envío y la respuesta de la AEAT viven en **tablas separadas**, precisamente para que lo inmutable pueda ser realmente inmutable.
4. Ninguna operación de `Update`/`Remove` sobre esas entidades en el código.

**Consecuencia para la demo:** el botón de "reiniciar datos de demostración" **recrea** la base de datos de demostración; no edita facturas. Y conviene decirlo en voz alta durante la demo: es una de las cosas que distingue un sistema que de verdad cumple de uno que dice cumplir.

### 5.1 Anulación

Generar un registro de anulación es una operación **de registro**, no de borrado: crea una fila nueva en `vf.RegistroFacturacion` con `Tipo = 'ANULACION'`, su propia huella encadenada y su propia entrada en la cola de envío. La factura sigue existiendo, marcada como anulada a efectos de presentación.

## 6. Subsanación y rechazos

La AEAT responde a cada registro con uno de tres resultados (§4 de `envio-y-cola-reintentos.md`):

| Resultado | Significado | Qué hacemos |
|---|---|---|
| **Aceptado** | Correcto | Se guarda el CSV/acuse y se cierra |
| **Aceptado con errores** | Se admite pero tiene defectos. **Caso típico: la huella no coincide con la calculada por la AEAT** | ⚠️ **Alerta roja**, no ámbar: indica un fallo estructural nuestro que afectará a todas las facturas siguientes |
| **Rechazado** | No se admite | Requiere **subsanación**: reenviar el registro corregido marcado como subsanación `[VERIFICAR]` nombres de campo |

**La subsanación no vulnera la inalterabilidad**: no se modifica el registro rechazado, se genera uno nuevo que lo subsana. Modelo de datos: el registro de subsanación es una fila nueva que referencia a la anterior.

## 7. Alcance del módulo en la demo

| Dentro | Fuera (modelado, no construido) |
|---|---|
| Series de facturación por emisor | Facturación de honorarios de la gestoría (fase 2) |
| Destinatarios y artículos/servicios | Modo NO VERI\*FACTU y registros de evento |
| Emisión F1, F2, R1, R4, R5 | F3, R2, R3 |
| Huella encadenada por NIF emisor | Facturación B2B obligatoria (Ley 18/2022) |
| Registro de alta y de anulación | Envío real a producción (la demo usa simulador) |
| QR, PDF, leyenda | Territorio foral (TicketBAI) |
| Cola de envío con reintentos y simulador de AEAT | |
| Declaración responsable | |

## 8. Riesgos y mejoras sugeridas

| # | Riesgo | Propuesta |
|---|---|---|
| V1 | **Transcribir campos del XSD a mano.** Es la vía más rápida a un rechazo sistemático | Esquemas descargados y versionados en el repositorio; generación del cliente/serializador **desde** el XSD; validación contra XSD antes de enviar. Nunca copiar de este documento |
| V2 | **La obligatoriedad puede volver a aplazarse** (ya van dos veces) | El argumentario comercial se apoya en el ahorro de tiempo, no en la fecha. La fecha es el detonante |
| V3 | Las versiones de los documentos técnicos cambian (huella 0.1.2 de 08/2024, QR 0.5.0) | Constantes de versión en el código, visibles en la pantalla de declaración responsable, y revisión en cada entrega |
| V4 | **La inalterabilidad chocará con el instinto del equipo** de "corregir un dato mal metido" | Triggers desde el primer script SQL, no como añadido posterior. Que el sistema diga que no desde el día uno |
| V5 | Un cliente en **territorio foral** que intente facturar | Bloqueo explícito en el alta con mensaje claro (DA-14). Nunca fallar en silencio |
| V6 | El desglose de IVA (recargo de equivalencia, criterio de caja, exentas, intracomunitarias) tiene mucha casuística | En la demo, cubrir bien **IVA general, reducido, superreducido, exenta y recargo de equivalencia**, que es el 95 % de una gestoría, y dejar el resto documentado como extensión. Vender menos casuística funcionando es mejor que toda a medias |
| V7 | Quién es el «productor del software» en los datos que viajan en cada registro | Cerrado en DA-02: configuración con datos ficticios en la demo, sustituibles sin desplegar. **Punto de control obligatorio antes de cualquier uso real** |
