# 06.4 · Código QR y representación impresa de la factura

> **Estado:** vigente · **Versión:** 1.0 · **Fecha:** 2026-09-22
> **Fuente oficial:** AEAT, *«Detalle de las especificaciones técnicas del código «QR» de la factura y de la «URL» del servicio de cotejo o remisión de información por parte del receptor de la factura»*, **versión 0.5.0**.
> PDF: `https://www.agenciatributaria.es/static_files/AEAT_Desarrolladores/EEDD/IVA/VERI-FACTU/DetalleEspecificacTecnCodigoQRfactura.pdf`
> Página de la sede: `https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/informacion-tecnica/caracteristicas-qr-especificaciones-servicio-cotejo-factura.html`
> Norma: artículos **20.1 y 21** de la orden que desarrolla el RD 1007/2023.
>
> Los datos de este documento están **tomados literalmente del PDF oficial** (consultado el 2026-09-22). `[VERIFICAR]` antes de implementar: que 0.5.0 siga siendo la versión vigente.

---

## 1. Requisitos del código QR

| Requisito | Valor oficial |
|---|---|
| Norma | **ISO/IEC 18004:2015** |
| Nivel de corrección de errores | **M** (medio) |
| Tamaño | Entre **30 × 30 mm** y **40 × 40 mm** |
| Zona de silencio (margen blanco) | **Mínimo 2 mm** en los cuatro lados; **recomendado 6 mm** |
| Contraste | Suficientemente alto respecto al fondo para asegurar la legibilidad |
| Resolución | «Apropiada»: legible tanto en papel como en imagen digital |

## 2. Ubicación y textos que lo acompañan

Esto no es criterio estético: está en la especificación y se comprueba en una inspección.

- El QR se sitúa **al principio de la factura**, antes del contenido generado por el sistema de facturación. Si hay algún obstáculo justificado, debe quedar **bien visible, claramente separado y en lugar preeminente**.
- Debe ser **el primer código QR** de la factura, si hubiera otros.
- Si la factura ocupa varias páginas, aparece **una sola vez, en la primera**.
- **Orientación vertical:** arriba, próximo al margen superior, **preferiblemente centrado** respecto a los márgenes izquierdo y derecho (si no, hacia el margen superior izquierdo).
- **Orientación apaisada:** a la izquierda, preferiblemente cerca del margen superior izquierdo (si no, centrado respecto a los márgenes superior e inferior).

### 2.1 Textos obligatorios

| Posición | Texto | Obligatoriedad |
|---|---|---|
| **Encima** del QR, preferiblemente centrado | **`QR tributario:`** | **Siempre.** Sirve para distinguirlo de otros QR de la factura |
| **Justo debajo** del QR, preferiblemente centrado | **«Factura verificable en la sede electrónica de la AEAT»** o **«VERI\*FACTU»** | Sólo en facturas expedidas por sistemas que emiten facturas verificables — que es **nuestro caso** |

Si la frase no cabe en una línea, puede ocupar varias.

**Tipografía de ambos textos:** legible, con tipo y tamaño **iguales o superiores** a los del resto de datos de la factura. Es decir: no se puede poner en letra pequeña al pie. En nuestra plantilla se fija el mismo tamaño que el cuerpo de la factura (nunca menor), y esto se comprueba en la revisión de diseño.

> **Decisión de producto:** usamos la frase larga, «Factura verificable en la sede electrónica de la AEAT», y no «VERI\*FACTU». Ambas son válidas, pero al receptor de la factura —que es un cliente de un cliente nuestro, no un técnico— la frase larga le dice qué hacer; la corta no le dice nada.

## 3. URL contenida en el QR

### 3.1 Formato (sistema que emite facturas **verificables**)

**Entorno de pruebas (Portal de Pruebas Externas):**

```
https://prewww2.aeat.es/wlpl/TIKE-CONT/ValidarQR?nif=XXXXXXXXY&numserie=YYYY...YYYY&fecha=DD-MM-AAAA&importe=NNNNNNNNN.DD
```

**Entorno de producción:**

```
https://www2.agenciatributaria.gob.es/wlpl/TIKE-CONT/ValidarQR?nif=XXXXXXXXY&numserie=YYYY...YYYY&fecha=DD-MM-AAAA&importe=NNNNNNNNN.DD
```

> Para sistemas que emiten facturas **no** verificables el recurso es `ValidarQRNoVerifactu` en los mismos hosts. **No nos aplica** (DA-11: sólo modo VERI\*FACTU), pero el adaptador acepta el recurso como configuración para no cerrar la puerta.

**Las dos URL van en configuración, nunca en código**, y la elección del entorno es la misma bandera que selecciona el entorno del servicio web de envío: emitir un QR de producción mientras se envía a preproducción produce facturas con un QR que no coteja.

### 3.2 Parámetros obligatorios (exactamente estos cuatro, en este orden)

| Parámetro | Formato | Longitud | Descripción |
|---|---|---|---|
| `nif` | Formato NIF | 9 | NIF del obligado a expedir la factura |
| `numserie` | Cadena de texto; admite caracteres especiales **ASCII 32–126** | **máx. 60** | Nº de serie + nº de factura |
| `fecha` | **`DD-MM-AAAA`** (guiones medios) | 10 | Fecha de expedición |
| `importe` | Numérico con **punto** como separador decimal | máx. **12 dígitos enteros** y **2 decimales** | Importe total de la factura |

**Codificación:** el contenido de los parámetros debe ir codificado con *URL encoding* estándar y **UTF-8**. Esto importa porque el `numserie` admite caracteres como `/`, `&` o espacios, que romperían la URL sin codificar.

> ⚠️ **Trampa.** La cadena de la huella (`huella-y-vectores-prueba.md`) usa el `NumSerieFactura` **sin codificar**; la URL del QR lo usa **codificado**. Son dos representaciones distintas del mismo dato y **no se pueden reutilizar la una por la otra**. Un `12345678/G33` es `12345678/G33` en la huella y `12345678%2FG33` en la URL.

Ejemplo oficial de URL válida (entorno de pruebas):

```
https://prewww2.aeat.es/wlpl/TIKE-CONT/ValidarQR?nif=89890001K&numserie=12345678-G33&fecha=01-09-2024&importe=241.4
```

### 3.3 Parámetros opcionales del servicio de cotejo

| Parámetro | Valores | Nota |
|---|---|---|
| `idioma` | `es` (por defecto), `ca`, `gl`, `eu`, `va`, `en` | Idioma de la respuesta en la sede |
| `formato` | `json` | Devuelve la respuesta en formato máquina. **Nunca puede incorporarse a la URL que va en el código QR de la factura** |

`formato=json` es útil para **nuestro propio** cotejo automatizado (por ejemplo, un trabajo que verifique una muestra de facturas contra la sede), pero no forma parte de la factura. Esta separación debe estar clara en el código: dos métodos distintos, `ConstruirUrlQr()` y `ConstruirUrlCotejoJson()`, y el primero **no acepta** parámetros opcionales.

### 3.4 Restricción que sube al modelo de datos

El límite de **60 caracteres** de `numserie` se refleja en `vf.FacturaEmitida.NumSerieFactura nvarchar(60)` ([04-modelo-datos.md](../04-modelo-datos.md) §6.2). Y el de 12 dígitos enteros en `ImporteTotal decimal(18,2)`, con validación de dominio que rechaza importes de más de 12 dígitos enteros. Una factura que no cabe en su propio QR es una factura defectuosa.

## 4. Generación del QR

- **En servidor**, con QRCoder (u otra librería equivalente que permita fijar el nivel de corrección **M**), como **PNG o SVG embebido en un `data:` URI**.
- **Sin recursos externos**: nada de servicios de generación de QR por URL. La factura se genera en un proceso sin acceso a internet garantizado y, sobre todo, enviar los datos de las facturas de los clientes a un tercero para dibujar un QR sería una cesión de datos injustificable.
- Tamaño: se calcula el número de módulos y se fija el tamaño físico en el CSS de impresión para caer dentro de 30–40 mm en la página A4 renderizada. **Esto se verifica midiendo sobre el PDF generado, no confiando en el cálculo.**

## 5. Plantilla de la factura (PDF)

### 5.1 Contenido

Plantilla Razor → HTML → PDF. Elementos:

- Cabecera con **logo y datos del emisor** (que es el cliente de la gestoría, no la gestoría: quien factura es él).
- Datos del destinatario, número de serie y factura, fecha de expedición.
- Líneas con descripción, cantidad, precio, tipo de IVA.
- **Desglose de IVA por tipo**, con su base y su cuota.
- **Recargo de equivalencia** cuando proceda (ver `especificacion.md`).
- **Retención de IRPF** cuando proceda, con su porcentaje y su importe.
- Totales: base, cuotas, recargo, retención, **importe total**.
- **QR tributario** con sus dos textos, según §1 y §2.
- Pie con los datos registrales y legales que correspondan al emisor.

### 5.2 Generación: Playwright con una sola instancia

Restricción de infraestructura (RAM): **una única instancia de navegador** reutilizada y una **cola con un solo consumidor**.

| Aspecto | Decisión |
|---|---|
| Navegador | **Chromium headless shell**, instalado **aparte del despliegue** (no por `playwright install` en arranque) |
| Comprobación al arrancar | La aplicación verifica que el ejecutable existe y responde; si no, **falla con un mensaje claro** en vez de arrancar y reventar en la primera factura |
| Cola | `Channel<TrabajoPdf>` con **un único consumidor** (`ColaPdfWorker`). Alternativa equivalente: `SemaphoreSlim(1)` |
| Timeout | Por trabajo (propuesta: 20 s). Al vencer, se cancela el trabajo |
| Recuperación | Si un trabajo falla o el navegador deja de responder, **se reinicia el navegador** y se reintenta una vez |
| Saturación | La cola tiene **capacidad máxima**; al llenarse, la petición recibe un mensaje de "generando, inténtelo en unos segundos" en vez de acumular memoria hasta morir. El comportamiento bajo saturación está definido, que es lo que exige el criterio de calidad §8 |
| Reutilización | Se reutiliza el **navegador**, pero cada trabajo abre y cierra su propio **contexto** de página: compartir contexto filtra estado entre facturas |

### 5.3 Regla de oro: **el PDF de una factura emitida se genera una vez**

Una vez generado, se almacena (`vf.FacturaEmitida.PdfClaveAlmacen`) y **no se regenera**. Si mañana cambia la plantilla, la factura de ayer debe seguir viéndose exactamente como se entregó. Regenerar "al vuelo" produciría, con el tiempo, PDFs distintos para la misma factura inmutable — que es precisamente lo que la norma trata de impedir.

### 5.4 Presupuesto de rendimiento

- Objetivo: **PDF en menos de 3 segundos** (criterio de éxito §7.5 de la visión), incluida la espera en cola con la cola vacía.
- La emisión de la factura **no espera al PDF**: se emite, se calcula la huella, se encola el envío y se encola el PDF. El usuario ve la factura emitida inmediatamente y el PDF aparece cuando está.

## 6. Riesgos y mejoras sugeridas

| # | Riesgo | Propuesta |
|---|---|---|
| Q1 | **Chromium es el mayor consumidor de RAM del stack** y el servidor va justo | Una instancia, un consumidor, `MemoryMax` en systemd, y reinicio del navegador cada N trabajos para cortar fugas acumuladas. Medir el pico real y anotarlo en `12-infraestructura-despliegue.md` |
| Q2 | El tamaño físico del QR (30–40 mm) depende del CSS de impresión y es fácil que se salga | Test que genera un PDF y **mide** el QR; no fiarse de la vista previa del navegador |
| Q3 | Confundir el `numserie` codificado (URL) con el sin codificar (huella) | Dos funciones con nombres distintos y test que comprueba que la huella de una factura con `/` en el número **no** contiene `%2F` |
| Q4 | Emitir con URL de producción apuntando a preproducción, o al revés | Una sola bandera de entorno para QR y servicio web; test que falla si las dos configuraciones no son coherentes; y el entorno visible en pantalla cuando no es producción |
| Q5 | La plantilla puede cambiar y afectar a facturas antiguas | PDF generado una vez y almacenado (§5.3), y versión de plantilla guardada junto a la factura |
| Q6 | El ejemplo oficial muestra `importe=241.4` (un decimal) mientras la tabla admite dos | Generamos **siempre dos decimales** con `InvariantCulture`; es válido y evita ambigüedad. Misma regla que en la huella (H3) |
